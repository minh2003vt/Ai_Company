using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Service.Interfaces;

namespace Ai_Company.ActionLogging
{
    public class ActionLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ActionLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IActionLogService actionLogService)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Bỏ qua login endpoint
            if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            Guid? userId = null;
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst(JwtRegisteredClaimNames.Sub);
                if (claim != null && Guid.TryParse(claim.Value, out var parsed))
                {
                    userId = parsed;
                }
            }

            // Tiếp tục request trước, sau đó log (hoặc log trước tùy nhu cầu)
            await _next(context);

            if (userId.HasValue)
            {
                var method = context.Request.Method;
                var statusCode = context.Response?.StatusCode;
                var ip = context.Connection.RemoteIpAddress?.ToString();
                var actionType = $"{method} {statusCode}";
                var actionDetail = $"{method} {path}";

                await actionLogService.LogAsync(userId.Value, actionType, actionDetail, path, ip);
            }
        }
    }
}


