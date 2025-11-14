using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserDepartmentController : ControllerBase
    {
        private readonly IUserDepartmentService _userDepartmentService;

        public UserDepartmentController(IUserDepartmentService userDepartmentService)
        {
            _userDepartmentService = userDepartmentService;
        }

        /// <summary>
        /// Tạo liên kết người dùng - phòng ban
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserDepartmentCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                return BadRequest(ApiResponse<object>.Fail(null, firstError));
            }

            try
            {
                var result = await _userDepartmentService.CreateAsync(dto);
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

        /// <summary>
        /// Cập nhật liên kết người dùng - phòng ban
        /// </summary>
        [HttpPut("{userId}/{departmentId}")]
        public async Task<IActionResult> Update(Guid userId, Guid departmentId, [FromBody] UserDepartmentUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                return BadRequest(ApiResponse<object>.Fail(null, firstError));
            }

            try
            {
                var result = await _userDepartmentService.UpdateAsync(userId, departmentId, dto);
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

        /// <summary>
        /// Xóa liên kết người dùng - phòng ban
        /// </summary>
        [HttpDelete("{userId}/{departmentId}")]
        public async Task<IActionResult> Delete(Guid userId, Guid departmentId)
        {
            try
            {
                var result = await _userDepartmentService.DeleteAsync(userId, departmentId);
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

        /// <summary>
        /// Xóa tất cả liên kết của người dùng
        /// </summary>
        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> DeleteByUserId(Guid userId)
        {
            try
            {
                var result = await _userDepartmentService.DeleteByUserIdAsync(userId);
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

        /// <summary>
        /// Lấy danh sách người dùng theo phòng ban
        /// </summary>
        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetByDepartment(Guid departmentId)
        {
            try
            {
                var result = await _userDepartmentService.GetByDepartmentAsync(departmentId);
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

        /// <summary>
        /// Lấy danh sách phòng ban theo người dùng
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            try
            {
                var result = await _userDepartmentService.GetByUserAsync(userId);
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

        /// <summary>
        /// Lấy danh sách liên kết theo công ty
        /// </summary>
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompany(Guid companyId)
        {
            try
            {
                var result = await _userDepartmentService.GetByCompanyAsync(companyId);
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

        /// <summary>
        /// Gán người dùng vào phòng ban
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignUserToDepartment([FromBody] AssignUserToDepartmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                return BadRequest(ApiResponse<object>.Fail(null, firstError));
            }

            try
            {
                var result = await _userDepartmentService.AssignUserToDepartmentAsync(dto);
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
















