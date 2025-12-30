using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DetentionLetterFunctionApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DetentionLetterFunctionApp.Services
{
    public class Dynamics365Service : IDynamics365Service
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Dynamics365Service> _logger;
        private readonly string _apiUrl;
        private readonly string _accessToken;

        public Dynamics365Service(HttpClient httpClient, IConfiguration configuration, ILogger<Dynamics365Service> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiUrl = _configuration["Dynamics365:ApiUrl"];
            _accessToken = _configuration["Dynamics365:AccessToken"];

            _httpClient.BaseAddress = new Uri(_apiUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            _httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            _httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<OrderSummary>> GetPendingDetentionLettersAsync()
        {
            try
            {
                var fetchXml = @"
                    <fetch>
                      <entity name='salesorder'>
                        <attribute name='salesorderid' />
                        <attribute name='ordernumber' />
                        <attribute name='name' />
                        <filter>
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                      </entity>
                    </fetch>";

                var query = $"salesorders?fetchXml={Uri.EscapeDataString(fetchXml)}";
                var response = await _httpClient.GetAsync(query);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<OrderSummary>>(content);

                return result ?? new List<OrderSummary>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending detention letters from Dynamics 365");
                return new List<OrderSummary>();
            }
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"systemusers({userId})?$select=systemuserid,fullname,internalemailaddress,firstname,lastname");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(content);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user {userId} from Dynamics 365");
                return null;
            }
        }

        public async Task<User> GetSalesEngineerByOrderNumberAsync(string orderNumber)
        {
            try
            {
                var fetchXml = $@"
                    <fetch>
                      <entity name='systemuser'>
                        <attribute name='systemuserid' />
                        <attribute name='fullname' />
                        <attribute name='internalemailaddress' />
                        <link-entity name='salesorder' from='ownerid' to='systemuserid'>
                          <filter>
                            <condition attribute='ordernumber' operator='eq' value='{orderNumber}' />
                          </filter>
                        </link-entity>
                      </entity>
                    </fetch>";

                var query = $"systemusers?fetchXml={Uri.EscapeDataString(fetchXml)}";
                var response = await _httpClient.GetAsync(query);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(content);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving sales engineer for order {orderNumber}");
                return null;
            }
        }

        public async Task<List<LettersType>> GetReportListForOrderAsync(string salesOrderId)
        {
            var reportList = new List<LettersType>();

            try
            {
                var response = await _httpClient.GetAsync($"salesorders({salesOrderId})?$select=salesorderid");
                response.EnsureSuccessStatusCode();

                reportList.Add(LettersType.CMPDetentionLetter);
                reportList.Add(LettersType.DuroMaxxDetentionLetter);

                return reportList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving report list for order {salesOrderId}");
                return reportList;
            }
        }

        public async Task<List<OrderHistory>> GetOrderHistoryAsync(Guid summaryId)
        {
            try
            {
                var fetchXml = $@"
                    <fetch>
                      <entity name='annotation'>
                        <attribute name='annotationid' />
                        <attribute name='subject' />
                        <attribute name='filename' />
                        <filter>
                          <condition attribute='objectid' operator='eq' value='{summaryId}' />
                        </filter>
                      </entity>
                    </fetch>";

                var query = $"annotations?fetchXml={Uri.EscapeDataString(fetchXml)}";
                var response = await _httpClient.GetAsync(query);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<OrderHistory>>(content);

                return result ?? new List<OrderHistory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving order history for {summaryId}");
                return new List<OrderHistory>();
            }
        }

        public async Task CreateOrderHistoryAsync(OrderHistory orderHistory)
        {
            try
            {
                var json = JsonSerializer.Serialize(orderHistory);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("annotations", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"Created order history for {orderHistory.SummaryId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order history");
            }
        }

        public async Task UpdateOrderHistoryAsync(Guid historyId, bool isGenerated, double fileSize, string message)
        {
            try
            {
                var updateData = new
                {
                    isgenerated = isGenerated,
                    filesize = fileSize,
                    message = message
                };

                var json = JsonSerializer.Serialize(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"annotations({historyId})", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"Updated order history {historyId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order history {historyId}");
            }
        }

        public async Task UpdateOrderSummaryStatusAsync(Guid summaryId, bool isProcessed, string status)
        {
            try
            {
                var updateData = new
                {
                    isprocessed = isProcessed,
                    status = status
                };

                var json = JsonSerializer.Serialize(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"salesorders({summaryId})", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"Updated order summary {summaryId} with status: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order summary {summaryId}");
            }
        }
    }
}
