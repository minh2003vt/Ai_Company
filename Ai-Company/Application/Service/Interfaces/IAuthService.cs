using System.Threading.Tasks;
using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IAuthService
    {
        Task<(AuthResult result, LoginResponseDto userInfo)> LoginAsync(string email, string password);
        Task<(AuthResult result, LoginResponseDto userInfo)> LoginWithGoogleAsync(string idToken);
        Task<AuthResult> ForgotPasswordAsync(string email);
    }
}


