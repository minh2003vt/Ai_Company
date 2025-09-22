using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/ai-configures")]
    public class AIConfigureController : ControllerBase
    {
        private readonly IAIConfigureService _aiConfigureService;

        public AIConfigureController(IAIConfigureService aiConfigureService)
        {
            _aiConfigureService = aiConfigureService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AIConfigureCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail(null, "Không thể xác định người dùng"));
                }

                var result = await _aiConfigureService.CreateAsync(dto, userId);
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _aiConfigureService.GetByIdAsync(id);
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
                var result = await _aiConfigureService.GetAllAsync();
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AIConfigureUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail(null, "Không thể xác định người dùng"));
                }

                var result = await _aiConfigureService.UpdateAsync(id, dto, userId);
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _aiConfigureService.DeleteAsync(id);
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
    }
}
