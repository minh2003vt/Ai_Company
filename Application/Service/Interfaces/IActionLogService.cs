using System;
using System.Threading.Tasks;

namespace Application.Service.Interfaces
{
    public interface IActionLogService
    {
        Task LogAsync(Guid userId, string actionType, string actionDetail, string endpoint, string ipAddress);
    }
}


