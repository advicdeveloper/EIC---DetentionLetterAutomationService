using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using DA = CONTECH.Service.DataAccess;
using BE = CONTECH.Service.BusinessEntities;
using BL = CONTECH.Service.BusinessLogic;

namespace CONTECH.Service.Workflow
{
    /// <summary>
    /// Custom Workflow Action to get Detention Order Data for Power Automate
    /// This action retrieves sales order products with unique product families
    /// and gets SESell1 user details for the order
    /// </summary>
    public class GetDetentionOrderDataAction : CodeActivity
    {
        #region Input Parameters

        [RequiredArgument]
        [Input("Sales Order Id")]
        public InArgument<string> SalesOrderId { get; set; }

        [Input("Order Number")]
        public InArgument<string> OrderNumber { get; set; }

        #endregion

        #region Output Parameters

        [Output("Word Templates")]
        public OutArgument<string> WordTemplates { get; set; }

        [Output("Word Templates Count")]
        public OutArgument<int> WordTemplatesCount { get; set; }

        [Output("User Email")]
        public OutArgument<string> UserEmail { get; set; }

        [Output("User Name")]
        public OutArgument<string> UserName { get; set; }

        [Output("User Full Name")]
        public OutArgument<string> UserFullName { get; set; }

        [Output("User Title")]
        public OutArgument<string> UserTitle { get; set; }

        [Output("Success")]
        public OutArgument<bool> Success { get; set; }

        [Output("Error Message")]
        public OutArgument<string> ErrorMessage { get; set; }

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                tracingService.Trace("GetDetentionOrderDataAction: Starting execution");

                // Get input parameters
                string salesOrderId = SalesOrderId.Get(executionContext);
                string orderNumber = OrderNumber.Get(executionContext);

                tracingService.Trace($"Sales Order Id: {salesOrderId}");
                tracingService.Trace($"Order Number: {orderNumber}");

                // Validate inputs
                if (string.IsNullOrEmpty(salesOrderId))
                {
                    throw new InvalidPluginExecutionException("Sales Order Id is required");
                }

                // Get word template list based on product analysis
                List<string> wordTemplateList = GetWordTemplateList(salesOrderId, service, tracingService);

                tracingService.Trace($"Found {wordTemplateList.Count} word templates needed");

                // Get SESell1 user details if order number is provided
                Entity userDetails = null;
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    userDetails = GetSESell1UserDetails(orderNumber, service, tracingService);
                    tracingService.Trace($"Retrieved user details for order: {orderNumber}");
                }

                // Set output parameters
                WordTemplates.Set(executionContext, string.Join(", ", wordTemplateList));
                WordTemplatesCount.Set(executionContext, wordTemplateList.Count);

                if (userDetails != null)
                {
                    UserEmail.Set(executionContext, userDetails.Contains("internalemailaddress") ?
                        userDetails.GetAttributeValue<string>("internalemailaddress") : string.Empty);
                    UserName.Set(executionContext, userDetails.Contains("domainname") ?
                        userDetails.GetAttributeValue<string>("domainname") : string.Empty);
                    UserFullName.Set(executionContext, userDetails.Contains("fullname") ?
                        userDetails.GetAttributeValue<string>("fullname") : string.Empty);
                    UserTitle.Set(executionContext, userDetails.Contains("title") ?
                        userDetails.GetAttributeValue<string>("title") : string.Empty);
                }
                else
                {
                    UserEmail.Set(executionContext, string.Empty);
                    UserName.Set(executionContext, string.Empty);
                    UserFullName.Set(executionContext, string.Empty);
                    UserTitle.Set(executionContext, string.Empty);
                }

                Success.Set(executionContext, true);
                ErrorMessage.Set(executionContext, string.Empty);

                tracingService.Trace("GetDetentionOrderDataAction: Completed successfully");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error in GetDetentionOrderDataAction: {ex.Message}");
                tracingService.Trace($"Stack Trace: {ex.StackTrace}");

                Success.Set(executionContext, false);
                ErrorMessage.Set(executionContext, ex.Message);

                throw new InvalidPluginExecutionException($"Error in GetDetentionOrderDataAction: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get word template list based on product analysis
        /// Queries D365 Online entities directly using IOrganizationService
        /// </summary>
        private List<string> GetWordTemplateList(string salesOrderId, IOrganizationService service, ITracingService tracingService)
        {
            List<BL.LettersType> lstlettersTypes = new List<BL.LettersType>();

            try
            {
                Guid orderGuid = new Guid(salesOrderId);
                tracingService.Trace($"Querying products for sales order: {orderGuid}");

                // Get all sales order products using FetchXML
                // This replaces the GetDetentionOrderProducts stored procedure
                string fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='salesorderdetail'>
                    <attribute name='salesorderdetailid' />
                    <attribute name='new_productsubtype' />
                    <attribute name='new_productfamily' />
                    <attribute name='new_productnumber' />
                    <attribute name='new_pa_gage' />
                    <attribute name='new_pa_grade' />
                    <attribute name='new_pa_corrugation' />
                    <attribute name='new_pa_shape' />
                    <attribute name='new_pa_diameter' />
                    <link-entity name='salesorder' from='salesorderid' to='salesorderid' alias='so'>
                      <attribute name='ordernumber' />
                      <attribute name='modifiedby' />
                      <link-entity name='systemuser' from='systemuserid' to='modifiedby' alias='su'>
                        <attribute name='fullname' />
                        <attribute name='internalemailaddress' />
                      </link-entity>
                    </link-entity>
                    <filter type='or'>
                      <condition attribute='salesorderid' operator='eq' value='{" + orderGuid.ToString() + @"}' />
                    </filter>
                  </entity>
                </fetch>";

                EntityCollection results = service.RetrieveMultiple(new FetchExpression(fetchXml));
                tracingService.Trace($"Retrieved {results.Entities.Count} product records");

                // Check for split orders in custom table if it exists
                // Add split order products if applicable
                try
                {
                    string splitOrderFetch = @"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                      <entity name='new_snowflakeordersplit'>
                        <attribute name='new_orderid' />
                        <attribute name='new_snowflakeorderid' />
                        <filter type='or'>
                          <condition attribute='new_orderid' operator='eq' value='{" + orderGuid.ToString() + @"}' />
                          <condition attribute='new_snowflakeorderid' operator='eq' value='{" + orderGuid.ToString() + @"}' />
                        </filter>
                      </entity>
                    </fetch>";

                    EntityCollection splitResults = service.RetrieveMultiple(new FetchExpression(splitOrderFetch));

                    if (splitResults.Entities.Count > 0)
                    {
                        tracingService.Trace($"Found {splitResults.Entities.Count} split order records");

                        // Get products from related split orders
                        foreach (Entity splitOrder in splitResults.Entities)
                        {
                            Guid relatedOrderId = splitOrder.Contains("new_orderid") ?
                                splitOrder.GetAttributeValue<Guid>("new_orderid") :
                                splitOrder.GetAttributeValue<Guid>("new_snowflakeorderid");

                            if (relatedOrderId != orderGuid)
                            {
                                string relatedFetch = fetchXml.Replace(orderGuid.ToString(), relatedOrderId.ToString());
                                EntityCollection relatedResults = service.RetrieveMultiple(new FetchExpression(relatedFetch));

                                foreach (Entity entity in relatedResults.Entities)
                                {
                                    results.Entities.Add(entity);
                                }
                            }
                        }
                    }
                }
                catch (Exception exSplit)
                {
                    tracingService.Trace($"Note: Split order table not found or error querying: {exSplit.Message}");
                    // Continue without split order data
                }

                // Process each product and apply business logic
                foreach (Entity product in results.Entities)
                {
                    ProcessProductForTemplates(product, lstlettersTypes, tracingService);
                }

                // Remove duplicates
                List<BL.LettersType> uniqueLetterTypes = lstlettersTypes.Distinct().ToList();
                tracingService.Trace($"Found {uniqueLetterTypes.Count} unique letter types");

                // Convert to template names
                List<string> templateNames = new List<string>();
                foreach (BL.LettersType letterType in uniqueLetterTypes)
                {
                    string templateName = letterType.ToString();
                    templateNames.Add(templateName);
                    tracingService.Trace($"Template: {templateName}");
                }

                return templateNames;
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error getting word template list: {ex.Message}");
                tracingService.Trace($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Process individual product and determine which templates are needed
        /// This implements the same logic as GetDetentionLetterReport.GetReportListForDownload
        /// </summary>
        private void ProcessProductForTemplates(Entity product, List<BL.LettersType> lstlettersTypes, ITracingService tracingService)
        {
            try
            {
                string productFamily = product.Contains("new_productfamily") ?
                    product.GetAttributeValue<string>("new_productfamily")?.Trim() : string.Empty;
                string partNo = product.Contains("new_productnumber") ?
                    product.GetAttributeValue<string>("new_productnumber")?.Trim().ToLower() : string.Empty;
                string shape = product.Contains("new_pa_shape") ?
                    product.GetAttributeValue<string>("new_pa_shape")?.Trim().ToLower() : string.Empty;

                tracingService.Trace($"Processing product: Family={productFamily}, PartNo={partNo}");

                // Check product family conditions
                if (productFamily == "CMP Detention" ||
                    productFamily == "CMP Detention - Voidsaver" ||
                    productFamily == "CMP Detention - xFiltration")
                {
                    lstlettersTypes.Add(BL.LettersType.CMPDetentionLetter);
                    return;
                }
                else if (productFamily == "DuroMaxx Containment Tank")
                {
                    lstlettersTypes.Add(BL.LettersType.DuroMaxxContainmentTankNotificationLetter);
                    return;
                }
                else if (productFamily == "DuroMaxx Detention" ||
                         productFamily == "DuroMaxx Detention - VoidSaver")
                {
                    lstlettersTypes.Add(BL.LettersType.DuroMaxxDetentionLetter);
                    return;
                }
                else if (productFamily == "UrbanGreen SRPE Cistern")
                {
                    lstlettersTypes.Add(BL.LettersType.DuroMaxxCisternRWHLetter);
                    return;
                }
                else if (productFamily == "DuroMaxx Sewer")
                {
                    lstlettersTypes.Add(BL.LettersType.DuroMaxxSewerLetter);
                    return;
                }

                // Check part number based conditions
                if (!string.IsNullOrEmpty(partNo) && partNo.Length > 3)
                {
                    string strPartNo1 = partNo.Substring(0, 2);
                    string strPartNo3 = partNo.Substring(0, 3);
                    bool isLarger = false;

                    #region HEL-COR AND RIVETED PIPE - Large Diameter
                    if ((strPartNo1 == "hp" || strPartNo1 == "hc" || strPartNo1 == "he" ||
                         strPartNo1 == "rp" || strPartNo1 == "re" || strPartNo1 == "rh") &&
                        partNo.Length >= 10)
                    {
                        string strCorr = partNo.Substring(2, 1);
                        string strGrade = partNo.Substring(3, 2);
                        string strGage;
                        string strDiam;

                        if (strPartNo1 == "hp" || strPartNo1 == "hc" || strPartNo1 == "he")
                        {
                            strGage = partNo.Substring(6, 2);
                            strDiam = partNo.Substring(8, 3);
                        }
                        else
                        {
                            strGage = partNo.Substring(5, 2);
                            strDiam = partNo.Substring(7, 3);
                        }

                        if (int.TryParse(strDiam, out int intDiam))
                        {
                            switch (strCorr)
                            {
                                case "2":
                                    isLarger = CheckAluNonAluCorrugation(strGrade, intDiam, strGage);
                                    break;
                                case "3":
                                case "5":
                                    isLarger = CheckAluCorrugation(strGrade, intDiam);
                                    break;
                                case "s":
                                    isLarger = CheckNonAluUltraFlo(strGrade, strGage, strPartNo1, intDiam);
                                    break;
                            }

                            if (isLarger)
                            {
                                lstlettersTypes.Add(BL.LettersType.CMPLargeDiameterLetter);
                                return;
                            }
                        }
                    }
                    #endregion

                    #region DOUBLE WALL HEL-COR
                    if ((strPartNo1 == "dw" || strPartNo1 == "da") && partNo.Length >= 14)
                    {
                        string strCorr = partNo.Substring(2, 1);
                        string strDiam = partNo.Substring(11, 3);

                        if (int.TryParse(strDiam, out int intDiam))
                        {
                            switch (strCorr)
                            {
                                case "2":
                                    if (intDiam > 77) isLarger = true;
                                    break;
                                case "3":
                                    if (intDiam > 101) isLarger = true;
                                    break;
                            }

                            if (isLarger)
                            {
                                lstlettersTypes.Add(BL.LettersType.CMPLargeDiameterLetter);
                                return;
                            }
                        }
                    }
                    #endregion

                    #region DUROMAXX PIPES
                    if (strPartNo3 == "xpg" && partNo.Length >= 8)
                    {
                        string strDiam = partNo.Substring(5, 3);

                        if (int.TryParse(strDiam, out int intDiam))
                        {
                            if (intDiam > 72)
                            {
                                lstlettersTypes.Add(BL.LettersType.DuroMaxxLgDiameterLetter);
                                return;
                            }
                        }
                    }
                    #endregion

                    #region URBAN GREEN SRPE AND UGM
                    if ((strPartNo3 == "ugu" || strPartNo3 == "ugs") && partNo.Length >= 7)
                    {
                        switch (strPartNo3)
                        {
                            case "ugu":
                                string strDiam = partNo.Substring(5, 2);
                                if (partNo.Length > 7)
                                {
                                    strDiam = partNo.Substring(5, 3);
                                }
                                if (int.TryParse(strDiam, out int intDiam))
                                {
                                    if (intDiam >= 72)
                                    {
                                        isLarger = true;
                                    }
                                }
                                break;
                            case "ugs":
                                isLarger = true;
                                break;
                        }

                        if (isLarger)
                        {
                            lstlettersTypes.Add(BL.LettersType.DuroMaxxCisternRWHLetter);
                            return;
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error processing product: {ex.Message}");
                // Continue processing other products
            }
        }

        private bool CheckAluCorrugation(string strGrade, int intDiam)
        {
            if ((strGrade == "al" && intDiam > 71) || (strGrade != "al" && intDiam > 101))
            {
                return true;
            }
            return false;
        }

        private bool CheckAluNonAluCorrugation(string strGrade, int intDiam, string strGage)
        {
            if ((strGrade == "al" && intDiam > 59) || (strGrade != "al" && intDiam > 77))
            {
                return true;
            }
            return false;
        }

        private bool CheckNonAluUltraFlo(string strGrade, string strGage, string strPartGroup, int intDiam)
        {
            if ((strGrade != "al" && intDiam > 77) || (strGrade == "al" && intDiam > 59))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get SESell1 user details for the order
        /// Queries D365 Online entities directly - replaces GetUserDetailForDetentionLetter SP
        /// Joins: SystemUser -> new_zipcode -> SalesCommission (via custom table)
        /// </summary>
        private Entity GetSESell1UserDetails(string orderNumber, IOrganizationService service, ITracingService tracingService)
        {
            Entity userDetails = null;

            try
            {
                tracingService.Trace($"Querying SESell1 user for order: {orderNumber}");

                // This FetchXML replicates the GetUserDetailForDetentionLetter stored procedure
                // Joins: SystemUser -> new_zipcode (custom entity) -> new_salescommission (custom table)
                string fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' top='1'>
                  <entity name='systemuser'>
                    <attribute name='systemuserid' />
                    <attribute name='fullname' />
                    <attribute name='domainname' />
                    <attribute name='internalemailaddress' />
                    <attribute name='title' />
                    <link-entity name='new_zipcode' from='new_senameid' to='systemuserid' alias='zipcode'>
                      <attribute name='new_zipcode' />
                      <link-entity name='new_salescommission' from='new_sesell1zipcode' to='new_zipcode' alias='commission'>
                        <filter>
                          <condition attribute='new_merlinorderno' operator='eq' value='" + orderNumber + @"' />
                        </filter>
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

                EntityCollection results = service.RetrieveMultiple(new FetchExpression(fetchXml));

                if (results.Entities.Count > 0)
                {
                    userDetails = results.Entities[0];

                    tracingService.Trace($"User Email: {(userDetails.Contains("internalemailaddress") ? userDetails.GetAttributeValue<string>("internalemailaddress") : "N/A")}");
                    tracingService.Trace($"User Name: {(userDetails.Contains("domainname") ? userDetails.GetAttributeValue<string>("domainname") : "N/A")}");
                    tracingService.Trace($"User Full Name: {(userDetails.Contains("fullname") ? userDetails.GetAttributeValue<string>("fullname") : "N/A")}");
                    tracingService.Trace($"User Title: {(userDetails.Contains("title") ? userDetails.GetAttributeValue<string>("title") : "N/A")}");
                }
                else
                {
                    tracingService.Trace("No user details found for the order");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error getting user details: {ex.Message}");
                tracingService.Trace($"Stack trace: {ex.StackTrace}");

                // Don't throw - continue with null user details
                // This allows the workflow to complete even if user details are not found
                userDetails = null;
            }

            return userDetails;
        }
    }
}
