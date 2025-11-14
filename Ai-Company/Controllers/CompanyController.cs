using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/companies")]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly IUserService _userService;

        public CompanyController(ICompanyService companyService, IUserService userService)
        {
            _companyService = companyService;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CompanyCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var result = await _companyService.CreateAsync(dto);
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
                var result = await _companyService.GetByIdAsync(id);
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
                var result = await _companyService.GetAllAsync();
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CompanyUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var result = await _companyService.UpdateAsync(id, dto);
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

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] CompanyStatusUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var result = await _companyService.UpdateStatusAsync(id, dto);
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
                var result = await _companyService.DeleteAsync(id);
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
        /// Lấy số lượng công ty hiện có
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetCompanyCount()
        {
            try
            {
                var result = await _companyService.GetCompanyCountAsync();
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
        /// Lấy danh sách người dùng theo công ty
        /// </summary>
        [HttpGet("{companyId}/users")]
        public async Task<IActionResult> GetUsers(Guid companyId)
        {
            try
            {
                var result = await _userService.GetUsersByCompanyAsync(companyId);
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


