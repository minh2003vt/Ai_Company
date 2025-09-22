using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Service
{
    public class AIConfigureService : IAIConfigureService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AIConfigureService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<AIConfigureResponseDto>> CreateAsync(AIConfigureCreateDto dto, Guid userId)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<AIConfigureResponseDto>.Fail(null, "Người dùng không tồn tại");
                }

                // Kiểm tra tên cấu hình AI đã tồn tại chưa
                var existingConfig = await _unitOfWork.AIConfigures.FindAsync(a => a.Name == dto.Name);
                if (existingConfig.Any())
                {
                    return ApiResponse<AIConfigureResponseDto>.Fail(null, "Tên cấu hình AI đã tồn tại");
                }

                var aiConfigure = new AI_Configure
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    RulesJson = dto.RulesJson,
                    ModelName = dto.ModelName,
                    Temperature = dto.Temperature,
                    MaxOutputTokens = dto.MaxOutputTokens,
                    UseStreaming = dto.UseStreaming,
                    UseRag = dto.UseRag,
                    RagTopK = dto.RagTopK,
                    CreatedByUserId = userId
                };

                await _unitOfWork.AIConfigures.AddAsync(aiConfigure);
                await _unitOfWork.SaveChangesAsync();

                var response = new AIConfigureResponseDto
                {
                    Id = aiConfigure.Id,
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    RulesJson = aiConfigure.RulesJson,
                    ModelName = aiConfigure.ModelName,
                    Temperature = aiConfigure.Temperature,
                    MaxOutputTokens = aiConfigure.MaxOutputTokens,
                    UseStreaming = aiConfigure.UseStreaming,
                    UseRag = aiConfigure.UseRag,
                    RagTopK = aiConfigure.RagTopK,
                    CreatedByUserId = aiConfigure.CreatedByUserId,
                    CreatedByUserName = user.FullName,
                    KnowledgeSourceCount = 0,
                    ChatSessionCount = 0
                };

                return ApiResponse<AIConfigureResponseDto>.Ok(response, "Tạo cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureResponseDto>.Fail(null, $"Lỗi khi tạo cấu hình AI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIConfigureResponseDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(id);
                if (aiConfigure == null)
                {
                    return ApiResponse<AIConfigureResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                }

                // Lấy thông tin người tạo
                var createdBy = await _unitOfWork.Users.GetByIdAsync(aiConfigure.CreatedByUserId);

                var response = new AIConfigureResponseDto
                {
                    Id = aiConfigure.Id,
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    RulesJson = aiConfigure.RulesJson,
                    ModelName = aiConfigure.ModelName,
                    Temperature = aiConfigure.Temperature,
                    MaxOutputTokens = aiConfigure.MaxOutputTokens,
                    UseStreaming = aiConfigure.UseStreaming,
                    UseRag = aiConfigure.UseRag,
                    RagTopK = aiConfigure.RagTopK,
                    CreatedByUserId = aiConfigure.CreatedByUserId,
                    CreatedByUserName = createdBy?.FullName ?? "Unknown",
                    KnowledgeSourceCount = aiConfigure.KnowledgeSources?.Count ?? 0,
                    ChatSessionCount = aiConfigure.ChatSessions?.Count ?? 0
                };

                return ApiResponse<AIConfigureResponseDto>.Ok(response, "Lấy thông tin cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureResponseDto>.Fail(null, $"Lỗi khi lấy thông tin cấu hình AI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllAsync()
        {
            try
            {
                var aiConfigures = await _unitOfWork.AIConfigures.GetAllAsync();
                var response = new List<AIConfigureResponseDto>();

                foreach (var config in aiConfigures)
                {
                    var createdBy = await _unitOfWork.Users.GetByIdAsync(config.CreatedByUserId);
                    response.Add(new AIConfigureResponseDto
                    {
                        Id = config.Id,
                        Name = config.Name,
                        Description = config.Description,
                        RulesJson = config.RulesJson,
                        ModelName = config.ModelName,
                        Temperature = config.Temperature,
                        MaxOutputTokens = config.MaxOutputTokens,
                        UseStreaming = config.UseStreaming,
                        UseRag = config.UseRag,
                        RagTopK = config.RagTopK,
                        CreatedByUserId = config.CreatedByUserId,
                        CreatedByUserName = createdBy?.FullName ?? "Unknown",
                        KnowledgeSourceCount = config.KnowledgeSources?.Count ?? 0,
                        ChatSessionCount = config.ChatSessions?.Count ?? 0
                    });
                }

                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Ok(response, "Lấy danh sách cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách cấu hình AI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIConfigureResponseDto>> UpdateAsync(Guid id, AIConfigureUpdateDto dto, Guid userId)
        {
            try
            {
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(id);
                if (aiConfigure == null)
                {
                    return ApiResponse<AIConfigureResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                }

                // Kiểm tra quyền sở hữu (chỉ người tạo mới có thể cập nhật)
                if (aiConfigure.CreatedByUserId != userId)
                {
                    return ApiResponse<AIConfigureResponseDto>.Fail(null, "Bạn không có quyền cập nhật cấu hình AI này");
                }

                // Kiểm tra tên cấu hình AI đã tồn tại chưa (trừ chính nó)
                var existingConfig = await _unitOfWork.AIConfigures.FindAsync(a => a.Name == dto.Name && a.Id != id);
                if (existingConfig.Any())
                {
                    return ApiResponse<AIConfigureResponseDto>.Fail(null, "Tên cấu hình AI đã tồn tại");
                }

                aiConfigure.Name = dto.Name;
                aiConfigure.Description = dto.Description;
                aiConfigure.RulesJson = dto.RulesJson;
                aiConfigure.ModelName = dto.ModelName;
                aiConfigure.Temperature = dto.Temperature;
                aiConfigure.MaxOutputTokens = dto.MaxOutputTokens;
                aiConfigure.UseStreaming = dto.UseStreaming;
                aiConfigure.UseRag = dto.UseRag;
                aiConfigure.RagTopK = dto.RagTopK;

                _unitOfWork.AIConfigures.Update(aiConfigure);
                await _unitOfWork.SaveChangesAsync();

                var createdBy = await _unitOfWork.Users.GetByIdAsync(aiConfigure.CreatedByUserId);
                var response = new AIConfigureResponseDto
                {
                    Id = aiConfigure.Id,
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    RulesJson = aiConfigure.RulesJson,
                    ModelName = aiConfigure.ModelName,
                    Temperature = aiConfigure.Temperature,
                    MaxOutputTokens = aiConfigure.MaxOutputTokens,
                    UseStreaming = aiConfigure.UseStreaming,
                    UseRag = aiConfigure.UseRag,
                    RagTopK = aiConfigure.RagTopK,
                    CreatedByUserId = aiConfigure.CreatedByUserId,
                    CreatedByUserName = createdBy?.FullName ?? "Unknown",
                    KnowledgeSourceCount = aiConfigure.KnowledgeSources?.Count ?? 0,
                    ChatSessionCount = aiConfigure.ChatSessions?.Count ?? 0
                };

                return ApiResponse<AIConfigureResponseDto>.Ok(response, "Cập nhật cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureResponseDto>.Fail(null, $"Lỗi khi cập nhật cấu hình AI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(id);
                if (aiConfigure == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy cấu hình AI");
                }

                // Kiểm tra xem có user nào đang sử dụng cấu hình AI này không
                var usersWithConfig = await _unitOfWork.Users.FindAsync(u => u.AIConfigureId == id);
                if (usersWithConfig.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không thể xóa cấu hình AI đang được sử dụng bởi người dùng");
                }

                _unitOfWork.AIConfigures.Delete(aiConfigure);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa cấu hình AI: {ex.Message}");
            }
        }
    }
}