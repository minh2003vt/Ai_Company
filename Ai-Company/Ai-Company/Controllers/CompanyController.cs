using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/companies")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
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
    }
}


