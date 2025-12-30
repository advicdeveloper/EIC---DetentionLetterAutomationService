using System.Collections.Generic;
using System.Linq;
using DetentionLetterAzureFunction.Models;
using Microsoft.Extensions.Logging;

namespace DetentionLetterAzureFunction.Services
{
    public class LetterDeterminationService : ILetterDeterminationService
    {
        private readonly ILogger<LetterDeterminationService> _logger;

        public LetterDeterminationService(ILogger<LetterDeterminationService> logger)
        {
            _logger = logger;
        }

        public List<LetterType> DetermineLetterTypes(List<OrderProduct> orderProducts)
        {
            var letterTypes = new HashSet<LetterType>();

            foreach (var product in orderProducts)
            {
                var productFamily = product.ProductFamily?.ToUpper() ?? string.Empty;
                var partNumber = product.PartNumber?.ToUpper() ?? string.Empty;

                if (productFamily.Contains("CMP") || partNumber.Contains("CMP"))
                {
                    letterTypes.Add(LetterType.CMPLetter);
                }

                if (productFamily.Contains("DUROMAXX") || partNumber.Contains("DMX"))
                {
                    letterTypes.Add(LetterType.DuroMaxxLetter);
                }

                if (productFamily.Contains("URBANGREEN") || partNumber.Contains("UG"))
                {
                    letterTypes.Add(LetterType.UrbanGreenLetter);
                }

                if (productFamily.Contains("LARGEDIAMETER") || partNumber.Contains("LD"))
                {
                    letterTypes.Add(LetterType.LargeDiameterLetter);
                }
            }

            _logger.LogInformation($"Determined {letterTypes.Count} letter types for order with {orderProducts.Count} products");
            return letterTypes.ToList();
        }
    }
}
