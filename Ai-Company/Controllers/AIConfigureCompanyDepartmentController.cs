using Application.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/ai-configure-company-departments")]
    [Authorize]
    public class AIConfigureCompanyDepartmentController : ControllerBase
    {
        private readonly IAIConfigureCompanyDepartmentService _service;

        public AIConfigureCompanyDepartmentController(IAIConfigureCompanyDepartmentService service)
        {
            _service = service;
        }

        public class CreateAIConfigureCompanyDepartmentLinkDto
        {
            public Guid AIConfigureCompanyId { get; set; }
            public Guid DepartmentId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAIConfigureCompanyDepartmentLinkDto dto)
        {
            var result = await _service.CreateAsync(dto.AIConfigureCompanyId, dto.DepartmentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("by-company/{companyId}")]
        public async Task<IActionResult> GetByCompanyId(Guid companyId)
        {
            var result = await _service.GetByCompanyIdAsync(companyId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}


