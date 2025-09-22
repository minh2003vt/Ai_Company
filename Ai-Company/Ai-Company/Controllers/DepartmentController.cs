using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/departments")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var result = await _departmentService.CreateAsync(dto);
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
                var result = await _departmentService.GetByIdAsync(id);
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
                var result = await _departmentService.GetAllAsync();
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetByCompanyId(Guid companyId)
        {
            try
            {
                var result = await _departmentService.GetByCompanyIdAsync(companyId);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DepartmentUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                var result = await _departmentService.UpdateAsync(id, dto);
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
                var result = await _departmentService.DeleteAsync(id);
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


