using Application.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/knowledge-sources")]
    [Authorize]
    public class KnowledgeSourceController : ControllerBase
    {
        private readonly IKnowledgeSourceService _service;

        public KnowledgeSourceController(IKnowledgeSourceService service)
        {
            _service = service;
        }

        [HttpGet("by-source")]
        public async Task<IActionResult> GetBySource([FromQuery] string source)
        {
            var result = await _service.GetBySourceAsync(source);
            return Ok(result);
        }

        [HttpGet("by-ai/{aiConfigureId}")]
        public async Task<IActionResult> GetByAIConfigureId(Guid aiConfigureId)
        {
            var result = await _service.GetByAIConfigureIdAsync(aiConfigureId);
            return Ok(result);
        }

        [HttpDelete("by-source")]
        public async Task<IActionResult> DeleteBySource([FromQuery] string source)
        {
            var result = await _service.DeleteBySourceAsync(source);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("by-ai/{aiConfigureId}")]
        public async Task<IActionResult> DeleteByAIConfigureId(Guid aiConfigureId)
        {
            var result = await _service.DeleteByAIConfigureIdAsync(aiConfigureId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}


