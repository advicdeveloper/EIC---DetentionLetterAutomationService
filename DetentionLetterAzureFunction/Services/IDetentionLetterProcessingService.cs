using System.Threading.Tasks;

namespace DetentionLetterAzureFunction.Services
{
    public interface IDetentionLetterProcessingService
    {
        Task ProcessPendingDetentionLettersAsync();
    }
}
