using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Domain.Entitites.Enums;
using UAParser;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using Google.Apis.Auth;

namespace Application.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(AuthResult result, LoginResponseDto userInfo)> LoginAsync(string email, string password)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
            {
                await LogFailedLoginByEmailOnlyAsync(email, LoginMethod.Email);
                return (new AuthResult { Success = false, Error = "Invalid credentials" }, null);
            }

            // Nếu đang bị khoá tạm thời
            if (user.IsBlocked && user.BlockedUntil.HasValue && user.BlockedUntil.Value > DateTime.UtcNow)
            {
                var until = user.BlockedUntil.Value.ToUniversalTime().ToString("u");
                return (new AuthResult { Success = false, Error = $"Tài khoản đang bị khóa đến {until}" }, null);
            }

            var providedHash = PasswordHasher.HashPassword(password);
            if (user.PasswordHash != providedHash)
            {
                const int maxAttempts = 5;
                const int blockMinutes = 15;

                user.FailedLoginAttempts += 1;

                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    user.IsBlocked = true;
                    user.BlockedUntil = DateTime.UtcNow.AddMinutes(blockMinutes);
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                if (user.IsBlocked)
                {
                    await LogFailedLoginByEmailOnlyAsync(user.Email, LoginMethod.Email);
                    return (new AuthResult
                    {
                        Success = false,
                        Error = $"Tài khoản bị khóa {blockMinutes} phút do đăng nhập sai quá {maxAttempts} lần"
                    }, null);
                }

                var remaining = Math.Max(0, maxAttempts - user.FailedLoginAttempts);
                // include failure data when email exists
                await LogFailedLoginByEmailOnlyAsync(user.Email, LoginMethod.Email);
                return (new AuthResult
                {
                    Success = false,
                    Error = "Invalid credentials",
                    FailedAttempts = user.FailedLoginAttempts,
                    RemainingAttempts = remaining,
                    UserId = user.Id,
                    Data = new
                    {
                        Email = user.Email,
                        FullName = user.FullName,
                        FailedLoginAttempts = user.FailedLoginAttempts
                    }
                }, null);
            }

            var token = GenerateJwtToken(user);

            // Log successful login
            var httpContext = _httpContextAccessor.HttpContext;
            var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();
            var xff = httpContext?.Request?.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(xff)) ip = xff.Split(',')[0].Trim();

            // Parse User-Agent
            var parser = Parser.GetDefault();
            var clientInfo = parser.Parse(userAgent ?? string.Empty);
            var device = $"{clientInfo.UA.Family} {clientInfo.UA.Major} on {clientInfo.OS.Family} {clientInfo.OS.Major}".Trim();

            // GeoIP (best-effort)
            string location = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(ip) && ip != "::1" && ip != "127.0.0.1" && ip != "unknown")
                {
                    var client = _httpClientFactory.CreateClient();
                    var geo = await client.GetFromJsonAsync<dynamic>($"http://ip-api.com/json/{ip}");
                    if (geo != null)
                    {
                        string country = geo?.country;
                        string city = geo?.city;
                        if (!string.IsNullOrWhiteSpace(country) || !string.IsNullOrWhiteSpace(city))
                        {
                            location = string.Join(", ", new[] { city, country }.Where(s => !string.IsNullOrWhiteSpace(s)));
                        }
                    }
                }
            }
            catch { }
            var loginLog = new LoginLogs
            {
                UserId = user.Id,
                LoginTime = DateTime.UtcNow,
                IpAddress = ip,
                Device = device,
                Location = location,
                LoginMethod = LoginMethod.Email.ToString()
            };
            await _unitOfWork.LoginLogs.AddAsync(loginLog);
            await _unitOfWork.SaveChangesAsync();

            // Reset counters and update last login
            user.FailedLoginAttempts = 0;
            user.IsBlocked = false;
            user.BlockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            var info = new LoginResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Id = user.Id,
                Email = user.Email,
                Role = user.Role?.Name ?? "User"
            };
            return (new AuthResult { Success = true, Token = token }, info);
        }

        public async Task<(AuthResult result, LoginResponseDto userInfo)> LoginWithGoogleAsync(string idToken)
        {
            try
            {
                var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken);
                var email = payload.Email;
                var fullName = payload.Name;

                var user = await _unitOfWork.Users.GetByEmailAsync(email);
                if (user == null)
                {
                    // assign default role "User"
                    var roles = await _unitOfWork.Roles.FindAsync(r => r.Name == "User");
                    var role = roles.FirstOrDefault();
                    if (role == null)
                    {
                        role = new Role { Name = "User", CreatedAt = DateTime.UtcNow };
                        await _unitOfWork.Roles.AddAsync(role);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    user = new User
                    {
                        Email = email,
                        FullName = string.IsNullOrWhiteSpace(fullName) ? email : fullName,
                        PasswordHash = PasswordHasher.HashPassword(Guid.NewGuid().ToString("N")),
                        RoleId = role.Id,
                        CreatedAt = DateTime.UtcNow,
                        DateOfBirth = new DateTime(1970, 1, 1),
                        PhoneNumber = string.Empty
                    };
                    await _unitOfWork.Users.AddAsync(user);
                    await _unitOfWork.SaveChangesAsync();
                }

                var token = GenerateJwtToken(user);
                await LogSuccessfulLoginAsync(user, LoginMethod.Google);
                user.FailedLoginAttempts = 0;
                user.IsBlocked = false;
                user.BlockedUntil = null;
                user.LastLoginAt = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                var info = new LoginResponseDto
                {
                    Token = token,
                    FullName = user.FullName,
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role?.Name ?? "User"
                };
                return (new AuthResult { Success = true, Token = token }, info);
            }
            catch
            {
                return (new AuthResult { Success = false, Error = "Google token không hợp lệ" }, null);
            }
        }

        public async Task<AuthResult> ForgotPasswordAsync(string email)
        {
            var exists = await _unitOfWork.Users.ExistsByEmailAsync(email);
            if (!exists)
            {
                return new AuthResult { Success = true, Error = null };
            }

            // TODO: issue reset token and send email; placeholder success
            return new AuthResult { Success = true };
        }

        private async Task LogSuccessfulLoginAsync(User user, LoginMethod method)
        {
            var (ip, device, location) = GetClientContext();
            var loginLog = new LoginLogs
            {
                UserId = user.Id,
                LoginTime = DateTime.UtcNow,
                IpAddress = ip,
                Device = device,
                Location = location,
                LoginMethod = method.ToString()
            };
            await _unitOfWork.LoginLogs.AddAsync(loginLog);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task LogFailedLoginByEmailOnlyAsync(string email, LoginMethod method)
        {
            try
            {
                var anon = await _unitOfWork.Users.GetByEmailAsync("anonymous@system.local");
                if (anon == null)
                {
                    var roles = await _unitOfWork.Roles.FindAsync(r => r.Name == "Anonymous");
                    var role = roles.FirstOrDefault();
                    if (role == null)
                    {
                        role = new Role { Name = "Anonymous", CreatedAt = DateTime.UtcNow };
                        await _unitOfWork.Roles.AddAsync(role);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    anon = new User
                    {
                        FullName = "Anonymous",
                        Email = "anonymous@system.local",
                        PasswordHash = PasswordHasher.HashPassword(Guid.NewGuid().ToString("N")),
                        RoleId = role.Id,
                        CreatedAt = DateTime.UtcNow,
                        DateOfBirth = new DateTime(1970, 1, 1),
                        PhoneNumber = string.Empty
                    };
                    await _unitOfWork.Users.AddAsync(anon);
                    await _unitOfWork.SaveChangesAsync();
                }

                var (ip, device, location) = GetClientContext();
                var loginLog = new LoginLogs
                {
                    UserId = anon.Id,
                    LoginTime = DateTime.UtcNow,
                    IpAddress = ip,
                    Device = device,
                    Location = location,
                    LoginMethod = method.ToString()
                };
                await _unitOfWork.LoginLogs.AddAsync(loginLog);
                await _unitOfWork.SaveChangesAsync();
            }
            catch { }
        }

        private (string ip, string device, string location) GetClientContext()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();
            var xff = httpContext?.Request?.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(xff)) ip = xff.Split(',')[0].Trim();

            var parser = Parser.GetDefault();
            var clientInfo = parser.Parse(userAgent ?? string.Empty);
            var device = $"{clientInfo.UA.Family} {clientInfo.UA.Major} on {clientInfo.OS.Family} {clientInfo.OS.Major}".Trim();

            string location = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(ip) && ip != "::1" && ip != "127.0.0.1" && ip != "unknown")
                {
                    var client = _httpClientFactory.CreateClient();
                    var geo = client.GetFromJsonAsync<dynamic>($"http://ip-api.com/json/{ip}").GetAwaiter().GetResult();
                    if (geo != null)
                    {
                        string country = geo?.country;
                        string city = geo?.city;
                        if (!string.IsNullOrWhiteSpace(country) || !string.IsNullOrWhiteSpace(city))
                        {
                            location = string.Join(", ", new[] { city, country }.Where(s => !string.IsNullOrWhiteSpace(s)));
                        }
                    }
                }
            }
            catch { }

            return (ip, device, location);
        }

        private async Task LogAnonymousFailedLoginAsync(User a,string loginMethod)
        {
            try
            {
                
                var httpContext = _httpContextAccessor.HttpContext;
                var ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
                var xff = httpContext?.Request?.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrEmpty(xff)) ip = xff.Split(',')[0].Trim();
                var device = httpContext?.Request?.Headers["User-Agent"].ToString();

                var loginLog = new LoginLogs
                {
                    UserId = a.Id,
                    LoginTime = DateTime.UtcNow,
                    IpAddress = ip,
                    Device = device,
                    Location = string.Empty,
                    LoginMethod = loginMethod
                };
                await _unitOfWork.LoginLogs.AddAsync(loginLog);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // best-effort; ignore failures
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ;
            var issuer = _configuration["Jwt:Issuer"] ;
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured. Set Jwt:Key");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


