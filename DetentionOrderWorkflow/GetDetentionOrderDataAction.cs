using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Xrm.Sdk;
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
                List<string> wordTemplateList = GetWordTemplateList(salesOrderId, tracingService);

                tracingService.Trace($"Found {wordTemplateList.Count} word templates needed");

                // Get SESell1 user details if order number is provided
                BE.Users userDetails = null;
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    userDetails = GetSESell1UserDetails(orderNumber, tracingService);
                    tracingService.Trace($"Retrieved user details for order: {orderNumber}");
                }

                // Set output parameters
                WordTemplates.Set(executionContext, string.Join(", ", wordTemplateList));
                WordTemplatesCount.Set(executionContext, wordTemplateList.Count);

                if (userDetails != null)
                {
                    UserEmail.Set(executionContext, userDetails.Email ?? string.Empty);
                    UserName.Set(executionContext, userDetails.UserName ?? string.Empty);
                    UserFullName.Set(executionContext, userDetails.FullName ?? string.Empty);
                    UserTitle.Set(executionContext, userDetails.Title ?? string.Empty);
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
        /// Uses the same logic as GetDetentionLetterReport.GetReportListForDownload
        /// </summary>
        private List<string> GetWordTemplateList(string salesOrderId, ITracingService tracingService)
        {
            List<string> templateNames = new List<string>();

            try
            {
                // Use existing business logic to determine which templates are needed
                BL.GetDetentionLetterReport reportLogic = new BL.GetDetentionLetterReport();
                List<BL.LettersType> letterTypes = reportLogic.GetReportListForDownload(salesOrderId);

                tracingService.Trace($"Retrieved {letterTypes.Count} letter types from business logic");

                // Convert LettersType enum values to template names
                foreach (BL.LettersType letterType in letterTypes)
                {
                    string templateName = letterType.ToString();
                    templateNames.Add(templateName);
                    tracingService.Trace($"Template added: {templateName}");
                }

                tracingService.Trace($"Word templates: {string.Join(", ", templateNames)}");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error getting word template list: {ex.Message}");
                throw;
            }

            return templateNames;
        }

        /// <summary>
        /// Get SESell1 user details for the order
        /// This method ensures unique user data is returned
        /// </summary>
        private BE.Users GetSESell1UserDetails(string orderNumber, ITracingService tracingService)
        {
            BE.Users userDetails = null;

            try
            {
                BL.DetentionLetterSummaryAction summaryAction = new BL.DetentionLetterSummaryAction();
                userDetails = summaryAction.GetSESell1Detail(orderNumber);

                if (userDetails != null)
                {
                    tracingService.Trace($"User Email: {userDetails.Email}");
                    tracingService.Trace($"User Name: {userDetails.UserName}");
                    tracingService.Trace($"User Full Name: {userDetails.FullName}");
                    tracingService.Trace($"User Title: {userDetails.Title}");
                }
                else
                {
                    tracingService.Trace("No user details found for the order");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error getting user details: {ex.Message}");
                throw;
            }

            return userDetails;
        }
    }
}
