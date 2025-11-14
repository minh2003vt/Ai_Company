using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entitites;
using Infrastructure;
using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAuthService _authService;

        public AuthController(AppDbContext db, IAuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null,firstError));
                }

                var (result, info) = await _authService.LoginAsync(request.Email, request.Password);
                if (!result.Success)
                {
                    return Unauthorized(ApiResponse<object>.Fail(result.Data, result.Error));
                }

                return Ok(ApiResponse<LoginResponseDto>.Ok(info, "Đăng nhập thành công"));
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null,"Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpPost("login/google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var (result, info) = await _authService.LoginWithGoogleAsync(request.IdToken);
                if (!result.Success)
                {
                    return Unauthorized(ApiResponse<object>.Fail(result.Data, result.Error));
                }

                return Ok(ApiResponse<LoginResponseDto>.Ok(info, "Đăng nhập thành công"));
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var result = await _authService.ForgotPasswordAsync(request.Email);
                if (!result.Success)
                {
                    return BadRequest(ApiResponse<object>.Fail(null, result.Error));
                }

                return Ok(ApiResponse<object>.Ok(null, "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu."));
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request,[FromQuery]string token)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }
                if(String.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(ApiResponse<object>.Fail(null, "Token không hợp lệ"));
                }
                var result = await _authService.ResetPasswordAsync(token, request.NewPassword);
                if (!result.Success)
                {
                    return BadRequest(ApiResponse<object>.Fail(null, result.Error));
                }

                return Ok(ApiResponse<object>.Ok(null, "Đặt lại mật khẩu thành công"));
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

    }
}


