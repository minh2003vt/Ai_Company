using System.Threading.Tasks;

namespace Application.Service.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}


