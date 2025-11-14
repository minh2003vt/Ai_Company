using System;
using System.Threading.Tasks;
using Application.Service.Interfaces;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Application.Service
{
    public class ActionLogService : IActionLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActionLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogAsync(Guid userId, string actionType, string actionDetail, string endpoint, string ipAddress)
        {
            // Lấy login log gần nhất của user để gắn ActionLog
            var loginLogs = await _unitOfWork.LoginLogs.FindAsync(l => l.UserId == userId);
            var latestLogin = loginLogs.OrderByDescending(l => l.LoginTime).FirstOrDefault();

            var action = new ActionLog
            {
                LoginLogId = latestLogin?.Id ?? Guid.Empty,
                ActionType = actionType,
                ActionDetail = actionDetail,
                Endpoint = endpoint,
                IpAddress = ipAddress
            };

            await _unitOfWork.ActionLogs.AddAsync(action);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}


