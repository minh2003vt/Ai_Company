using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/user-ai-configs")]
    [Authorize]
    public class UserAiConfigController : ControllerBase
    {
        private readonly IUserAiConfigService _userAiConfigService;

        public UserAiConfigController(IUserAiConfigService userAiConfigService)
        {
            _userAiConfigService = userAiConfigService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserAiConfigCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var result = await _userAiConfigService.CreateAsync(dto);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet("{userId}/{aiConfigureId}")]
        public async Task<IActionResult> GetById(Guid userId, Guid aiConfigureId)
        {
            try
            {
                var result = await _userAiConfigService.GetByIdAsync(userId, aiConfigureId);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _userAiConfigService.GetAllAsync();
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            try
            {
                var result = await _userAiConfigService.GetByUserIdAsync(userId);
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet("ai-configure/{aiConfigureId}")]
        public async Task<IActionResult> GetByAIConfigureId(Guid aiConfigureId)
        {
            try
            {
                var result = await _userAiConfigService.GetByAIConfigureIdAsync(aiConfigureId);
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }


        [HttpDelete("{userId}/{aiConfigureId}")]
        public async Task<IActionResult> Delete(Guid userId, Guid aiConfigureId)
        {
            try
            {
                var result = await _userAiConfigService.DeleteAsync(userId, aiConfigureId);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet("{userId}/{aiConfigureId}/has-access")]
        public async Task<IActionResult> HasAccess(Guid userId, Guid aiConfigureId)
        {
            try
            {
                var result = await _userAiConfigService.HasAccessAsync(userId, aiConfigureId);
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }
    }
}
