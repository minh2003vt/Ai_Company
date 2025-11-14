using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Service
{
    public class AIModelConfigService : IAIModelConfigService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AIModelConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<AIModelConfigResponseDto>> CreateAsync(AIModelConfigDto dto)
        {
            try
            {
                var modelConfig = new AIModelConfig
                {
                    ModelName = dto.ModelName,
                    Temperature = dto.Temperature,
                    MaxOutputTokens = dto.MaxOutputTokens,
                    UseStreaming = dto.UseStreaming,
                    ApiKey = dto.ApiKey ?? "", // Có thể set khi tạo, hoặc để trống và set sau
                    TopP = dto.TopP,
                    TopK = dto.TopK
                    // Password sẽ được set sau bằng hàm change password riêng
                };

                await _unitOfWork.AIModelConfigs.AddAsync(modelConfig);
                await _unitOfWork.SaveChangesAsync();

                var response = new AIModelConfigResponseDto
                {
                    Id = modelConfig.Id,
                    ModelName = modelConfig.ModelName,
                    Temperature = modelConfig.Temperature,
                    MaxOutputTokens = modelConfig.MaxOutputTokens,
                    UseStreaming = modelConfig.UseStreaming,
                    TopP = modelConfig.TopP,
                    TopK = modelConfig.TopK,
                    Active = modelConfig.Active,
                    CreatedAt = modelConfig.CreatedAt
                };

                return ApiResponse<AIModelConfigResponseDto>.Ok(response, "Tạo ModelConfig thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIModelConfigResponseDto>.Fail(null, $"Lỗi khi tạo ModelConfig: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIModelConfigResponseDto>> GetByIdAsync(Guid id, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return ApiResponse<AIModelConfigResponseDto>.Fail(null, "Mật khẩu là bắt buộc");
                }

                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(id);
                if (modelConfig == null)
                {
                    return ApiResponse<AIModelConfigResponseDto>.Fail(null, "Không tìm thấy ModelConfig");
                }

                // Verify password if PasswordHash exists
                if (!string.IsNullOrWhiteSpace(modelConfig.PasswordHash))
                {
                    if (!PasswordHasher.VerifyPassword(password, modelConfig.PasswordHash))
                    {
                        return ApiResponse<AIModelConfigResponseDto>.Fail(null, "Mật khẩu không đúng");
                    }
                }

                var response = new AIModelConfigResponseDto
                {
                    Id = modelConfig.Id,
                    ModelName = modelConfig.ModelName,
                    Temperature = modelConfig.Temperature,
                    MaxOutputTokens = modelConfig.MaxOutputTokens,
                    UseStreaming = modelConfig.UseStreaming,
                    TopP = modelConfig.TopP,
                    TopK = modelConfig.TopK,
                    Active = modelConfig.Active,
                    CreatedAt = modelConfig.CreatedAt
                };

                return ApiResponse<AIModelConfigResponseDto>.Ok(response, "Lấy thông tin ModelConfig thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIModelConfigResponseDto>.Fail(null, $"Lỗi khi lấy thông tin ModelConfig: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ModelConfigApiKeyResponseDto>> GetApiKeyAsync(Guid id, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return ApiResponse<ModelConfigApiKeyResponseDto>.Fail(null, "Mật khẩu là bắt buộc");
                }

                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(id);
                if (modelConfig == null)
                {
                    return ApiResponse<ModelConfigApiKeyResponseDto>.Fail(null, "Không tìm thấy ModelConfig");
                }

                // Verify password if PasswordHash exists
                if (!string.IsNullOrWhiteSpace(modelConfig.PasswordHash))
                {
                    if (!PasswordHasher.VerifyPassword(password, modelConfig.PasswordHash))
                    {
                        return ApiResponse<ModelConfigApiKeyResponseDto>.Fail(null, "Mật khẩu không đúng");
                    }
                }

                var response = new ModelConfigApiKeyResponseDto
                {
                    Id = modelConfig.Id,
                    ApiKey = modelConfig.ApiKey
                };

                return ApiResponse<ModelConfigApiKeyResponseDto>.Ok(response, "Lấy ApiKey thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<ModelConfigApiKeyResponseDto>.Fail(null, $"Lỗi khi lấy ApiKey: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIModelConfigResponseDto>> UpdateAsync(Guid id, AIModelConfigDto dto)
        {
            try
            {
                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(id);
                if (modelConfig == null)
                {
                    return ApiResponse<AIModelConfigResponseDto>.Fail(null, "Không tìm thấy ModelConfig");
                }

                // Update fields (không update password và apiKey - dùng hàm riêng)
                modelConfig.ModelName = dto.ModelName;
                modelConfig.Temperature = dto.Temperature;
                modelConfig.MaxOutputTokens = dto.MaxOutputTokens;
                modelConfig.UseStreaming = dto.UseStreaming;
                modelConfig.TopP = dto.TopP;
                modelConfig.TopK = dto.TopK;
                modelConfig.UpdatedAt = DateTime.UtcNow.AddHours(7);

                _unitOfWork.AIModelConfigs.Update(modelConfig);
                await _unitOfWork.SaveChangesAsync();

                var response = new AIModelConfigResponseDto
                {
                    Id = modelConfig.Id,
                    ModelName = modelConfig.ModelName,
                    Temperature = modelConfig.Temperature,
                    MaxOutputTokens = modelConfig.MaxOutputTokens,
                    UseStreaming = modelConfig.UseStreaming,
                    TopP = modelConfig.TopP,
                    TopK = modelConfig.TopK,
                    Active = modelConfig.Active,
                    CreatedAt = modelConfig.CreatedAt
                };

                return ApiResponse<AIModelConfigResponseDto>.Ok(response, "Cập nhật ModelConfig thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIModelConfigResponseDto>.Fail(null, $"Lỗi khi cập nhật ModelConfig: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<AIModelConfigResponseDto>>> GetAllAsync()
        {
            try
            {
                var modelConfigs = await _unitOfWork.AIModelConfigs.GetAllAsync();
                var response = modelConfigs.Select(mc => new AIModelConfigResponseDto
                {
                    Id = mc.Id,
                    ModelName = mc.ModelName,
                    Temperature = mc.Temperature,
                    MaxOutputTokens = mc.MaxOutputTokens,
                    UseStreaming = mc.UseStreaming,
                    TopP = mc.TopP,
                    TopK = mc.TopK,
                    Active = mc.Active,
                    CreatedAt = mc.CreatedAt
                }).ToList();

                return ApiResponse<IEnumerable<AIModelConfigResponseDto>>.Ok(response, "Lấy danh sách ModelConfig thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<AIModelConfigResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách ModelConfig: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdatePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ApiResponse<bool>.Fail(false, "Mật khẩu mới không được để trống");
                }

                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(id);
                if (modelConfig == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy ModelConfig");
                }

                // Verify old password if PasswordHash exists
                if (!string.IsNullOrWhiteSpace(modelConfig.PasswordHash))
                {
                    if (!PasswordHasher.VerifyPassword(oldPassword, modelConfig.PasswordHash))
                    {
                        return ApiResponse<bool>.Fail(false, "Mật khẩu hiện tại không đúng");
                    }
                }
                else
                {
                    // Nếu chưa có password, vẫn yêu cầu oldPassword để đảm bảo an toàn
                    // Hoặc có thể bỏ qua nếu muốn cho phép set password lần đầu
                    // Tạm thời vẫn yêu cầu oldPassword (có thể là empty string nếu chưa set)
                }

                // Hash password mới
                modelConfig.PasswordHash = PasswordHasher.HashPassword(newPassword);
                modelConfig.UpdatedAt = DateTime.UtcNow.AddHours(7);

                _unitOfWork.AIModelConfigs.Update(modelConfig);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Cập nhật mật khẩu thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi cập nhật mật khẩu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateApiKeyAsync(Guid id, string password, string newApiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return ApiResponse<bool>.Fail(false, "Mật khẩu là bắt buộc");
                }

                if (string.IsNullOrWhiteSpace(newApiKey))
                {
                    return ApiResponse<bool>.Fail(false, "ApiKey mới không được để trống");
                }

                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(id);
                if (modelConfig == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy ModelConfig");
                }

                // Verify password if PasswordHash exists
                if (!string.IsNullOrWhiteSpace(modelConfig.PasswordHash))
                {
                    if (!PasswordHasher.VerifyPassword(password, modelConfig.PasswordHash))
                    {
                        return ApiResponse<bool>.Fail(false, "Mật khẩu không đúng");
                    }
                }

                // Update ApiKey
                modelConfig.ApiKey = newApiKey;
                modelConfig.UpdatedAt = DateTime.UtcNow.AddHours(7);

                _unitOfWork.AIModelConfigs.Update(modelConfig);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Cập nhật ApiKey thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi cập nhật ApiKey: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> SetActiveModelAsync(Guid id)
        {
            try
            {
                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(id);
                if (modelConfig == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy ModelConfig");
                }

                // Find the currently active model BEFORE we change anything
                var allModels = await _unitOfWork.AIModelConfigs.GetAllAsync();
                var previouslyActiveModel = allModels.FirstOrDefault(m => m.Active == true && m.Id != id);

                // Set all models to inactive first
                foreach (var model in allModels)
                {
                    if (model.Active)
                    {
                        model.Active = false;
                        model.UpdatedAt = DateTime.UtcNow.AddHours(7);
                        _unitOfWork.AIModelConfigs.Update(model);
                    }
                }

                // Set the selected model to active
                modelConfig.Active = true;
                modelConfig.UpdatedAt = DateTime.UtcNow.AddHours(7);
                _unitOfWork.AIModelConfigs.Update(modelConfig);

                // Update all AI_Configure that were using the previously active model to use the new active model
                if (previouslyActiveModel != null)
                {
                    var allAIConfigures = await _unitOfWork.AIConfigures.GetAllAsync();
                    foreach (var aiConfigure in allAIConfigures)
                    {
                        if (aiConfigure.ModelConfigId == previouslyActiveModel.Id)
                        {
                            // Update to use the new active model
                            aiConfigure.ModelConfigId = id;
                            _unitOfWork.AIConfigures.Update(aiConfigure);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Đã chuyển model active thành công và cập nhật tất cả AI_Configure");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi chuyển model active: {ex.Message}");
            }
        }
    }
}

