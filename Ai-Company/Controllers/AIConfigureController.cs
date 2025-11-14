using Application.Service.Interfaces;
using Application.Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Application.Helper;
using Application.Service;
using Domain.Entitites.Enums;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/ai-configures")]
    [Authorize]
    public class AIConfigureController : ControllerBase
    {
        private readonly IAIConfigureService _aiConfigureService;
        private readonly QdrantHelper _qdrantHelper;
        private readonly QdrantService _qdrantService;

        public AIConfigureController(IAIConfigureService aiConfigureService, QdrantHelper qdrantHelper, QdrantService qdrantService, Infrastructure.Repository.Interfaces.IUnitOfWork unitOfWork)
        {
            _aiConfigureService = aiConfigureService;
            _qdrantHelper = qdrantHelper;
            _qdrantService = qdrantService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AIConfigureDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ";
                    return BadRequest(ApiResponse<object>.Fail(null, firstError));
                }

                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
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

        [HttpPost("create-and-upload")]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> CreateAndUpload([FromForm] string name, [FromForm] string description, [FromForm] string rules, [FromForm] List<IFormFile> files)
        {
            try
            {
                // Lấy userId từ JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail(null, "Không thể xác định người dùng"));
                }

                // 1) Create AI Configure
                var createDto = new AIConfigureDto
                {
                    Name = name,
                    Description = description,
                    Rules = rules
                };
                var createResult = await _aiConfigureService.CreateAsync(createDto, userId);
                if (!createResult.Success || createResult.Data == null)
                {
                    return BadRequest(createResult);
                }

                var configId = createResult.Data.Id;

                // 2) Decode base64 files and upload
                var items = new List<object>();
                int success = 0;
                var failures = new List<object>();

                if (files != null && files.Count > 0)
                {
                    foreach (var formFile in files)
                    {
                        try
                        {
                            var results = await _qdrantHelper.ImportFileAsync(formFile, configId.ToString(), configId, userId);
                            items.Add(new { file = formFile.FileName, chunks = results.Select(r => new { r.pointId, r.knowledgeSourceId, r.pageNumber }) });
                            success++;
                        }
                        catch (Exception ex)
                        {
                            failures.Add(new { filename = formFile?.FileName, reason = ex.Message });
                        }
                    }
                }

                // Sau khi upload xong, cập nhật RagTopK
                await _aiConfigureService.RecalculateRagTopKAsync(configId);
                var updatedConfig = await _aiConfigureService.GetByIdAsync(configId);

                var response = new
                {
                    aiConfigure = updatedConfig.Success && updatedConfig.Data != null ? updatedConfig.Data : createResult.Data,
                    uploads = new
                    {
                        success,
                        failed = failures.Count,
                        failures,
                        items
                    }
                };

                return Ok(ApiResponse<object>.Ok(response, "Tạo cấu hình và tải lên tài liệu thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, $"Có lỗi xảy ra: {ex.Message}"));
            }
        }

        // Base64 upload is no longer used in this endpoint

        [HttpDelete("{id}/documents:collection")]
        public async Task<IActionResult> DeleteCollection(string id)
        {
            try
            {
                // 1) Xóa collection trong Qdrant
                await _qdrantService.DeleteCollectionAsync(id.ToString());
                // 2) Gợi ý: Xóa file Cloudinary liên quan và DB records sẽ cần thông tin public_id
                //    Hiện tại controller không thao tác trực tiếp DB theo yêu cầu, phần dọn dẹp DB/file
                //    có thể được thực hiện bởi một background job hoặc một service khác được gọi tại đây.
                return Ok(ApiResponse<object>.Ok(new { deleted = true }, "Đã xóa collection"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, $"Xóa collection lỗi: {ex.Message}"));
            }
        }

        [HttpDelete("{id}/documents:point/{pointId}")]
        public async Task<IActionResult> DeletePoint(Guid id, ulong pointId)
        {
            try
            {
                // 1) Xóa point trong Qdrant
                await _qdrantService.DeletePointsAsync(id.ToString(), new[] { pointId });
                // 2) Tương tự, dọn dẹp Cloudinary/DB nên thực hiện ở tầng service khác nếu cần
                return Ok(ApiResponse<object>.Ok(new { deleted = 1 }, "Đã xóa point"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, $"Xóa point lỗi: {ex.Message}"));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                // Lấy userId từ JWT token (nếu có)
                Guid? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var result = await _aiConfigureService.GetByIdAsync(id, userId);
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
                // Lấy userId từ JWT token (nếu có)
                Guid? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var result = await _aiConfigureService.GetAllAsync(userId);
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpPost("{id}/search")] // text vector search in Qdrant
        public async Task<IActionResult> Search(Guid id, [FromBody] SearchRequestDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Query))
                {
                    return BadRequest(ApiResponse<object>.Fail(null, "Thiếu query"));
                }

                var vector = await _qdrantHelper.GenerateEmbeddingAsync(dto.Query);
                var resultJson = await _qdrantService.SearchAsync(id.ToString(), vector);
                return Ok(ApiResponse<object>.Ok(System.Text.Json.JsonSerializer.Deserialize<object>(resultJson), "Kết quả tìm kiếm"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, $"Lỗi search: {ex.Message}"));
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
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
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

        [HttpPost("{id}/documents:upload")]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> UploadDocument(Guid id, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<object>.Fail(null, "Thiếu file upload"));
                }

                // Optional: check quyền sở hữu cấu hình trước khi import tài liệu
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail(null, "Không thể xác định người dùng"));
                }

                // Upload + Index + Persist via helper
                var results = await _qdrantHelper.ImportFileAsync(file, id.ToString(), id, userId);
                var response = new
                {
                    success = 1,
                    failed = 0,
                    chunks = results.Select(r => new { r.pointId, r.knowledgeSourceId, r.pageNumber })
                };
                // Cập nhật RagTopK theo số lượng file hiện có
                await _aiConfigureService.RecalculateRagTopKAsync(id);
                return Ok(ApiResponse<object>.Ok(response, "Tải lên và index tài liệu thành công"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(null, ex.Message));
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra khi xử lý file"));
            }
        }

        [HttpPost("{id}/documents:batch-upload")]
        [RequestSizeLimit(104_857_600)]
        public async Task<IActionResult> BatchUploadDocuments(Guid id, List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return BadRequest(ApiResponse<object>.Fail(null, "Thiếu file upload"));
                }
                if (files.Count > 5)
                {
                    return BadRequest(ApiResponse<object>.Fail(null, "Tối đa 5 file mỗi lần"));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                int success = 0;
                var failures = new List<object>();
                var items = new List<object>();

                foreach (var file in files)
                {
                    try
                    {
                        var results = await _qdrantHelper.ImportFileAsync(file, id.ToString(), id, Guid.Parse(userIdClaim.Value));
                        items.Add(new { file = file.FileName, chunks = results.Select(r => new { r.pointId, r.knowledgeSourceId, r.pageNumber }) });
                        success++;
                    }
                    catch (Exception ex)
                    {
                        failures.Add(new { filename = file.FileName, reason = ex.Message });
                    }
                }

                var response = new
                {
                    success,
                    failed = failures.Count,
                    failures,
                    items
                };
                // Cập nhật RagTopK theo số lượng file hiện có
                await _aiConfigureService.RecalculateRagTopKAsync(id);
                return Ok(ApiResponse<object>.Ok(response, "Kết quả tải lên"));
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra khi xử lý batch upload"));
            }
        }

        [HttpGet("my")] // get all AI available to current user (granted + created)
        public async Task<IActionResult> GetAllForCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail(null, "Không thể xác định người dùng"));
                }

                var result = await _aiConfigureService.GetAllForUserAsync(userId);
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet("by-kind/{kind}")] // get all AI by kind (Global or Company)
        public async Task<IActionResult> GetAllByKind(string kind)
        {
            try
            {
                if (!Enum.TryParse<AI_ConfigureKind>(kind, true, out var kindEnum))
                {
                    return BadRequest(ApiResponse<object>.Fail(null, $"Kind không hợp lệ. Chỉ chấp nhận: {string.Join(", ", Enum.GetNames(typeof(AI_ConfigureKind)))}"));
                }

                // Lấy userId từ JWT token (nếu có)
                Guid? userId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var result = await _aiConfigureService.GetAllByKindAsync(kindEnum, userId);
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        // Versioning endpoints
        [HttpGet("{id}/versions")]
        public async Task<IActionResult> GetVersions(Guid id)
        {
            try
            {
                var result = await _aiConfigureService.GetVersionsAsync(id);
                return Ok(result);
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail(null, "Có lỗi xảy ra, vui lòng thử lại."));
            }
        }

        [HttpGet("{id}/versions/{version}")]
        public async Task<IActionResult> GetVersion(Guid id, string version)
        {
            try
            {
                var result = await _aiConfigureService.GetVersionAsync(id, version);
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

        [HttpPost("{id}/versions/{version}:rollback")]
        public async Task<IActionResult> Rollback(Guid id, string version)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(ApiResponse<object>.Fail(null, "Không thể xác định người dùng"));
                }

                var result = await _aiConfigureService.RollbackAsync(id, version, userId);
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
