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

        [Output("Product Families")]
        public OutArgument<string> ProductFamilies { get; set; }

        [Output("Product Families Count")]
        public OutArgument<int> ProductFamiliesCount { get; set; }

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

                // Get all sales order products and extract unique product families
                List<string> uniqueProductFamilies = GetUniqueProductFamilies(salesOrderId, tracingService);

                tracingService.Trace($"Found {uniqueProductFamilies.Count} unique product families");

                // Get SESell1 user details if order number is provided
                BE.Users userDetails = null;
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    userDetails = GetSESell1UserDetails(orderNumber, tracingService);
                    tracingService.Trace($"Retrieved user details for order: {orderNumber}");
                }

                // Set output parameters
                ProductFamilies.Set(executionContext, string.Join(", ", uniqueProductFamilies));
                ProductFamiliesCount.Set(executionContext, uniqueProductFamilies.Count);

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
        /// Get unique product families from sales order products
        /// </summary>
        private List<string> GetUniqueProductFamilies(string salesOrderId, ITracingService tracingService)
        {
            List<string> uniqueFamilies = new List<string>();

            try
            {
                DA.OrderProductRepositry productRepo = new DA.OrderProductRepositry();
                DataTable productData = productRepo.GetOrderProductDetail(salesOrderId);

                tracingService.Trace($"Retrieved {productData.Rows.Count} products from database");

                if (productData != null && productData.Rows.Count > 0)
                {
                    // Extract product families and make them unique
                    HashSet<string> familySet = new HashSet<string>();

                    foreach (DataRow row in productData.Rows)
                    {
                        if (row["ProductFamily"] != DBNull.Value)
                        {
                            string productFamily = row["ProductFamily"].ToString().Trim();

                            if (!string.IsNullOrEmpty(productFamily))
                            {
                                familySet.Add(productFamily);
                            }
                        }
                    }

                    uniqueFamilies = familySet.OrderBy(f => f).ToList();
                    tracingService.Trace($"Unique product families: {string.Join(", ", uniqueFamilies)}");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error getting product families: {ex.Message}");
                throw;
            }

            return uniqueFamilies;
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
