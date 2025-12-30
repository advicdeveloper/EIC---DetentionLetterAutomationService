using System.Threading.Tasks;

namespace DetentionLetterFunctionApp.Services
{
    public interface IDetentionLetterService
    {
        Task ProcessPendingDetentionLettersAsync();
    }
}
