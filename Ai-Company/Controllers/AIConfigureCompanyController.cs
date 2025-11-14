using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/ai-configure-companies")]
    [Authorize]
    public class AIConfigureCompanyController : ControllerBase
    {
        private readonly IAIConfigureCompanyService _service;

        public AIConfigureCompanyController(IAIConfigureCompanyService service)
        {
            _service = service;
        }

        public class CreateAIConfigureCompanyLinkDto
        {
            [Required]
            public Guid CompanyId { get; set; }
            [Required]
            public Guid AIConfigureId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAIConfigureCompanyLinkDto dto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                return BadRequest(ApiResponse<object>.Fail(null, firstError));
            }

            var result = await _service.CreateAsync(dto.CompanyId, dto.AIConfigureId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("by-ai-configure/{aiConfigureId}")]
        public async Task<IActionResult> GetCompaniesByAIConfigureId(Guid aiConfigureId)
        {
            var result = await _service.GetCompaniesByAIConfigureIdAsync(aiConfigureId);
            return Ok(result);
        }

        [HttpGet("by-company/{companyId}")]
        public async Task<IActionResult> GetAIConfiguresByCompanyId(Guid companyId)
        {
            var result = await _service.GetAIConfiguresByCompanyIdAsync(companyId);
            return Ok(result);
        }

        [HttpDelete("by-keys")]
        public async Task<IActionResult> DeleteByKeys([FromQuery] Guid companyId, [FromQuery] Guid aiConfigureId)
        {
            var result = await _service.DeleteByKeysAsync(companyId, aiConfigureId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}
