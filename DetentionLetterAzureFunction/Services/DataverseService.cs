using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DetentionLetterAzureFunction.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DetentionLetterAzureFunction.Services
{
    public class DataverseService : IDataverseService
    {
        private readonly ServiceClient _serviceClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataverseService> _logger;
        private readonly Guid _businessUnitId;

        public DataverseService(IConfiguration configuration, ILogger<DataverseService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var connectionString = _configuration["Dynamics365:ConnectionString"];
            _serviceClient = new ServiceClient(connectionString);

            if (!_serviceClient.IsReady)
            {
                _logger.LogError("Failed to connect to Dataverse");
                throw new Exception("Failed to connect to Dataverse");
            }

            _businessUnitId = Guid.Parse(_configuration["Dynamics365:BusinessUnitId"]);
            _logger.LogInformation("Successfully connected to Dataverse");
        }

        public async Task<List<DetentionOrderSummary>> GetPendingDetentionOrdersAsync()
        {
            try
            {
                var query = new QueryExpression("crmgp_detentionordersummary")
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("crmgp_issend", ConditionOperator.Equal, 0)
                        }
                    }
                };

                var result = await Task.Run(() => _serviceClient.RetrieveMultiple(query));

                var summaries = result.Entities.Select(e => new DetentionOrderSummary
                {
                    Id = e.Id,
                    SalesOrderId = e.GetAttributeValue<EntityReference>("crmgp_salesorderid")?.Id ?? Guid.Empty,
                    SoldToContactId = e.GetAttributeValue<EntityReference>("crmgp_soldtocontactid")?.Id ?? Guid.Empty,
                    OwningBusinessUnitId = e.GetAttributeValue<EntityReference>("owningbusinessunit")?.Id ?? Guid.Empty,
                    IsSend = e.GetAttributeValue<int>("crmgp_issend"),
                    CreatedOn = e.GetAttributeValue<DateTime>("createdon")
                }).ToList();

                _logger.LogInformation($"Retrieved {summaries.Count} pending detention orders");
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending detention orders");
                return new List<DetentionOrderSummary>();
            }
        }

        public async Task<SalesOrder> GetSalesOrderAsync(Guid salesOrderId)
        {
            try
            {
                var entity = await Task.Run(() => _serviceClient.Retrieve("salesorder", salesOrderId, new ColumnSet(true)));

                return new SalesOrder
                {
                    Id = entity.Id,
                    OrderNumber = entity.GetAttributeValue<string>("ordernumber"),
                    Name = entity.GetAttributeValue<string>("name"),
                    StateCode = entity.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0,
                    StatusCode = entity.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0,
                    OpportunityId = entity.GetAttributeValue<EntityReference>("opportunityid")?.Id ?? Guid.Empty,
                    QuoteId = entity.GetAttributeValue<EntityReference>("quoteid")?.Id ?? Guid.Empty,
                    CustomerId = entity.GetAttributeValue<EntityReference>("customerid")?.Id ?? Guid.Empty,
                    OwningBusinessUnit = entity.GetAttributeValue<EntityReference>("owningbusinessunit")?.Id ?? Guid.Empty,
                    ZipCode = entity.GetAttributeValue<string>("billto_postalcode")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sales order {salesOrderId}");
                return null;
            }
        }

        public async Task<List<OrderProduct>> GetOrderProductsAsync(Guid salesOrderId)
        {
            try
            {
                var query = new QueryExpression("salesorderdetail")
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("salesorderid", ConditionOperator.Equal, salesOrderId)
                        }
                    }
                };

                var result = await Task.Run(() => _serviceClient.RetrieveMultiple(query));

                return result.Entities.Select(e => new OrderProduct
                {
                    Id = e.Id,
                    SalesOrderId = salesOrderId,
                    ProductId = e.GetAttributeValue<EntityReference>("productid")?.Id ?? Guid.Empty,
                    ProductName = e.GetAttributeValue<string>("productdescription"),
                    Quantity = e.GetAttributeValue<decimal>("quantity")
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving order products for {salesOrderId}");
                return new List<OrderProduct>();
            }
        }

        public async Task<Contact> GetContactAsync(Guid contactId)
        {
            try
            {
                var entity = await Task.Run(() => _serviceClient.Retrieve("contact", contactId, new ColumnSet(true)));

                return new Contact
                {
                    Id = entity.Id,
                    FullName = entity.GetAttributeValue<string>("fullname"),
                    Email = entity.GetAttributeValue<string>("emailaddress1"),
                    FirstName = entity.GetAttributeValue<string>("firstname"),
                    LastName = entity.GetAttributeValue<string>("lastname")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving contact {contactId}");
                return null;
            }
        }

        public async Task<List<Contact>> GetSalesEngineersAsync(string zipCode)
        {
            try
            {
                var query = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet("systemuserid", "fullname", "internalemailaddress"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("isdisabled", ConditionOperator.Equal, false)
                        }
                    }
                };

                var result = await Task.Run(() => _serviceClient.RetrieveMultiple(query));

                return result.Entities.Select(e => new Contact
                {
                    Id = e.Id,
                    FullName = e.GetAttributeValue<string>("fullname"),
                    Email = e.GetAttributeValue<string>("internalemailaddress")
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sales engineers for zip code {zipCode}");
                return new List<Contact>();
            }
        }

        public async Task UpdateDetentionOrderStatusAsync(Guid detentionOrderId, int isSend, string errorMessage = null)
        {
            try
            {
                var entity = new Entity("crmgp_detentionordersummary", detentionOrderId);
                entity["crmgp_issend"] = isSend;

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    entity["crmgp_errormessage"] = errorMessage;
                }

                if (isSend == 1)
                {
                    entity["crmgp_processedon"] = DateTime.UtcNow;
                }

                await Task.Run(() => _serviceClient.Update(entity));
                _logger.LogInformation($"Updated detention order {detentionOrderId} status to {isSend}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating detention order {detentionOrderId}");
            }
        }
    }
}
