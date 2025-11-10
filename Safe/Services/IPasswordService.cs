using System.Threading.Tasks;

namespace Safe.Services
{
    public interface IPasswordService
    {
        Task<bool> VerifyPasswordAsync(string password);
        Task SetPasswordAsync(string password);
        Task<bool> HasPasswordSetupAsync();
    }
}