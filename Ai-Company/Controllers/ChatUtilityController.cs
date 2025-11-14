using Application.Service.Interfaces;
using Domain.Entitites;
using Domain.Entitites.Enums;
using Infrastructure.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatUtilityController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly IUnitOfWork _unitOfWork;

        public ChatUtilityController(IGeminiService geminiService, IUnitOfWork unitOfWork)
        {
            _geminiService = geminiService;
            _unitOfWork = unitOfWork;
        }

        public class GenerateNameRequest
        {
            public string Question { get; set; }
        }

        [HttpPost("generate-session-name")]
        public async Task<IActionResult> GenerateSessionName([FromBody] GenerateNameRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Question))
            {
                return BadRequest("question is required");
            }

            // Lấy userId từ JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Không thể xác định người dùng");
            }

            // Lấy AI_Configure từ database - ưu tiên Global, nếu không có thì lấy một cái bất kỳ mà user có quyền
            AI_Configure aiConfigure = null;
            
            // Thử lấy Global AI_Configure trước
            var globalConfigs = await _unitOfWork.AIConfigures.FindAsync(a => a.Kind == AI_ConfigureKind.Global);
            aiConfigure = globalConfigs.FirstOrDefault();

            // Nếu không có Global, lấy một cái từ danh sách user có quyền
            if (aiConfigure == null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user != null && user.CompanyId.HasValue)
                {
                    // Lấy AI_ConfigureCompany links
                    var companyLinks = await _unitOfWork.AIConfigureCompanies.FindAsync(
                        ac => ac.CompanyId == user.CompanyId.Value
                    );
                    if (companyLinks.Any())
                    {
                        var firstLink = companyLinks.First();
                        aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(firstLink.AIConfigureId);
                    }
                }

                // Nếu vẫn không có, lấy một cái mà user tạo
                if (aiConfigure == null)
                {
                    var userConfigs = await _unitOfWork.AIConfigures.FindAsync(a => a.CreatedByUserId == userId);
                    aiConfigure = userConfigs.FirstOrDefault();
                }
            }

            // Nếu vẫn không có, trả về lỗi
            if (aiConfigure == null)
            {
                return BadRequest("Không tìm thấy cấu hình AI để sử dụng");
            }

            // Load ModelConfig
            var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(aiConfigure.ModelConfigId);
            if (modelConfig == null)
            {
                return BadRequest("ModelConfig không tồn tại");
            }

            var instruction = "You are a helpful assistant to name chat sessions. Based on the user's question, propose a concise, descriptive session title in at most 10 words. Do not add quotes, punctuation at the ends, or explanations. Return ONLY the title text.";
            var prompt = $"{instruction}\n\nUser question: {req.Question}";

            var generated = await _geminiService.GenerateResponseAsync(prompt, aiConfigure, modelConfig);

            // Normalize and enforce <= 10 words
            var text = (generated ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text)) text = "New chat";
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 10)
            {
                text = string.Join(" ", words.Take(10));
            }

            return Ok(new { answer = text });
        }
    }
}


