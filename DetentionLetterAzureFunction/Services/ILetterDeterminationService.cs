using System.Collections.Generic;
using DetentionLetterAzureFunction.Models;

namespace DetentionLetterAzureFunction.Services
{
    public interface ILetterDeterminationService
    {
        List<LetterType> DetermineLetterTypes(List<OrderProduct> orderProducts);
    }
}
