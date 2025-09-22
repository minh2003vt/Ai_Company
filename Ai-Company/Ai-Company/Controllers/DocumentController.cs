
using Application.Service;
using Microsoft.AspNetCore.Mvc;
namespace Ai_Company.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly DocumentService _service;

        public DocumentController(DocumentService service)
        {
            _service = service;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] Guid aiConfigId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File missing");

            // Save temporarily
            var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            // Extract, chunk, and store in Weaviate
            await _service.StoreInWeaviateAsync(tempPath, aiConfigId);

            return Ok(new { message = "File processed and stored in Weaviate" });
        }
    }
}
