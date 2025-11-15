using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Application.Service
{
    public class AIConfigureService : IAIConfigureService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKnowledgeSourceService _knowledgeSourceService;
        private readonly QdrantService _qdrantService;
        private readonly FirebaseService _firebaseService;

        public AIConfigureService(IUnitOfWork unitOfWork, IKnowledgeSourceService knowledgeSourceService, QdrantService qdrantService, FirebaseService firebaseService)
        {
            _unitOfWork = unitOfWork;
            _knowledgeSourceService = knowledgeSourceService;
            _qdrantService = qdrantService;
            _firebaseService = firebaseService;
        }

        private async Task<bool> IsSystemAdminAsync(Guid userId)
        {
            try
            {
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == userId);
                if (userDepartments == null || !userDepartments.Any())
                {
                    return false;
                }
                
                foreach (var ud in userDepartments)
                {
                    if (ud.RoleId == Guid.Empty)
                    {
                        continue;
                    }
                    
                    var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                    if (role != null && string.Equals(role.Name, "SystemAdmin", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IsSystemAdminAsync] Error checking SystemAdmin role for user {userId}: {ex.Message}");
                // Return false on error to avoid blocking the request
                return false;
            }
        }

        public async Task<ApiResponse<AIConfigureDetailResponseDto>> CreateAsync(AIConfigureDto dto, Guid userId)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Người dùng không tồn tại");
                }

                // Kiểm tra role của user để xác định Kind (dùng helper method để tái sử dụng)
                var isSystemAdmin = await IsSystemAdminAsync(userId);

                // Xác định Kind và CompanyId
                Domain.Entitites.Enums.AI_ConfigureKind kind;
                Guid? companyId = null;
                
                if (isSystemAdmin)
                {
                    kind = Domain.Entitites.Enums.AI_ConfigureKind.Global;
                    companyId = null;
                }
                else
                {
                    kind = Domain.Entitites.Enums.AI_ConfigureKind.Company;
                    companyId = user.CompanyId;
                    
                    if (!companyId.HasValue)
                    {
                        return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Người dùng phải thuộc một công ty để tạo cấu hình AI");
                    }
                }

                // Kiểm tra tên cấu hình AI đã tồn tại chưa
                var existingConfig = await _unitOfWork.AIConfigures.FindAsync(a => a.Name == dto.Name);
                if (existingConfig.Any())
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Tên cấu hình AI đã tồn tại");
                }

                var baseRule = "only answer information in the RAG";
                var combinedRules = string.IsNullOrWhiteSpace(dto.Rules)
                    ? baseRule
                    : string.Concat(baseRule, "; ", dto.Rules);

                // Handle ModelConfig: create new or use existing
                AIModelConfig modelConfig;
                if (dto.ModelConfigId.HasValue)
                {
                    // Use existing ModelConfig
                    modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(dto.ModelConfigId.Value);
                    if (modelConfig == null)
                    {
                        return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "ModelConfig không tồn tại");
                    }
                }
                else if (dto.ModelConfig != null)
                {
                    // Create new ModelConfig
                    modelConfig = new AIModelConfig
                    {
                        ModelName = dto.ModelConfig.ModelName,
                        Temperature = dto.ModelConfig.Temperature,
                        MaxOutputTokens = dto.ModelConfig.MaxOutputTokens,
                        UseStreaming = dto.ModelConfig.UseStreaming,
                        ApiKey = dto.ModelConfig.ApiKey ?? "", // Có thể set khi tạo, hoặc để trống và set sau
                        TopP = dto.ModelConfig.TopP,
                        TopK = dto.ModelConfig.TopK,
                        Active = false // New model is not active by default
                        // Password sẽ được set sau bằng hàm change password riêng
                    };
                    await _unitOfWork.AIModelConfigs.AddAsync(modelConfig);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    // Auto-select active model if available, otherwise create default
                    var activeModel = await _unitOfWork.AIModelConfigs.FindAsync(m => m.Active == true);
                    if (activeModel != null && activeModel.Any())
                    {
                        modelConfig = activeModel.First();
                    }
                    else
                    {
                        // Create default ModelConfig
                        modelConfig = new AIModelConfig
                        {
                            ModelName = "gemini-2.5-pro",
                            Temperature = 0.85f,
                            MaxOutputTokens = 8192,
                            UseStreaming = false,
                            ApiKey = "", // Will need to be set later or from appsettings
                            Active = false
                        };
                        await _unitOfWork.AIModelConfigs.AddAsync(modelConfig);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                var aiConfigure = new AI_Configure
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Rules = combinedRules,
                    Kind = kind,
                    CompanyId = companyId,
                    CreatedByUserId = userId,
                    CurrentVersion = string.IsNullOrWhiteSpace(dto.Version) ? "v1.0.0" : dto.Version,
                    ModelConfigId = modelConfig.Id
                };

                await _unitOfWork.AIConfigures.AddAsync(aiConfigure);
                await _unitOfWork.SaveChangesAsync();

                // Create initial version snapshot (Version = string)
                var v1 = new AI_ConfigureVersion
                {
                    AIConfigureId = aiConfigure.Id,
                    Version = aiConfigure.CurrentVersion!,
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    Rules = aiConfigure.Rules,
                    ModelConfigId = modelConfig.Id,
                    RagTopK = aiConfigure.RagTopK,
                    Kind = aiConfigure.Kind,
                    CompanyId = aiConfigure.CompanyId,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AIConfigureVersions.AddAsync(v1);
                await _unitOfWork.SaveChangesAsync();

                // Nếu Kind = Company và có CompanyId, tự động tạo link AIConfigureCompany
                if (kind == Domain.Entitites.Enums.AI_ConfigureKind.Company && companyId.HasValue)
                {
                    // Kiểm tra link đã tồn tại chưa
                    var existingLink = await _unitOfWork.AIConfigureCompanies.FindAsync(l => 
                        l.CompanyId == companyId.Value && l.AIConfigureId == aiConfigure.Id);
                    
                    if (!existingLink.Any())
                    {
                        var aiConfigureCompany = new Domain.Entitites.AI_ConfigureCompany
                        {
                            CompanyId = companyId.Value,
                            AIConfigureId = aiConfigure.Id,
                            CreatedAt = DateTime.UtcNow.AddHours(7)
                        };
                        await _unitOfWork.AIConfigureCompanies.AddAsync(aiConfigureCompany);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                var response = new AIConfigureDetailResponseDto
                {
                    Id = aiConfigure.Id,
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    Rules = aiConfigure.Rules,
                    RagTopK = aiConfigure.RagTopK,
                    CreatedByUserId = aiConfigure.CreatedByUserId,
                    CreatedByUserName = user.FullName,
                    KnowledgeSourceCount = 0,
                    ChatSessionCount = 0,
                    CurrentVersion = aiConfigure.CurrentVersion,
                    ModelConfig = new AIModelConfigResponseDto
                    {
                        Id = modelConfig.Id,
                        ModelName = modelConfig.ModelName,
                        Temperature = modelConfig.Temperature,
                        MaxOutputTokens = modelConfig.MaxOutputTokens,
                        UseStreaming = modelConfig.UseStreaming,
                        TopP = modelConfig.TopP,
                        TopK = modelConfig.TopK,
                        CreatedAt = modelConfig.CreatedAt
                    }
                };

                return ApiResponse<AIConfigureDetailResponseDto>.Ok(response, "Tạo cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, $"Lỗi khi tạo cấu hình AI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIConfigureDetailResponseDto>> GetByIdAsync(Guid id, Guid? userId = null)
        {
            try
            {
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(id);
                if (aiConfigure == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                }

                // Nếu có userId, check role và filter
                if (userId.HasValue)
                {
                    var isSystemAdmin = await IsSystemAdminAsync(userId.Value);
                    // Nếu là SystemAdmin, chỉ hiện Kind = Global (0)
                    if (isSystemAdmin && aiConfigure.Kind != Domain.Entitites.Enums.AI_ConfigureKind.Global)
                    {
                        return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                    }
                    // Nếu không phải SystemAdmin, chỉ hiện Kind = Company và cùng CompanyId
                    if (!isSystemAdmin)
                    {
                        if (aiConfigure.Kind != Domain.Entitites.Enums.AI_ConfigureKind.Company)
                        {
                            return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                        }
                        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                        if (user == null || !user.CompanyId.HasValue || aiConfigure.CompanyId != user.CompanyId.Value)
                        {
                            return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                        }
                    }
                }

                // Load ModelConfig
                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(aiConfigure.ModelConfigId);
                if (modelConfig == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "ModelConfig không tồn tại");
                }

                // Lấy thông tin người tạo
                var createdBy = await _unitOfWork.Users.GetByIdAsync(aiConfigure.CreatedByUserId);

                var response = new AIConfigureDetailResponseDto
                {
                    Id = aiConfigure.Id,
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    Rules = aiConfigure.Rules,
                    RagTopK = aiConfigure.RagTopK,
                    CreatedByUserId = aiConfigure.CreatedByUserId,
                    CreatedByUserName = createdBy?.FullName ?? "Unknown",
                    KnowledgeSourceCount = aiConfigure.KnowledgeSources?.Count ?? 0,
                    ChatSessionCount = aiConfigure.ChatSessions?.Count ?? 0,
                    CurrentVersion = aiConfigure.CurrentVersion,
                    ModelConfig = new AIModelConfigResponseDto
                    {
                        Id = modelConfig.Id,
                        ModelName = modelConfig.ModelName,
                        Temperature = modelConfig.Temperature,
                        MaxOutputTokens = modelConfig.MaxOutputTokens,
                        UseStreaming = modelConfig.UseStreaming,
                        TopP = modelConfig.TopP,
                        TopK = modelConfig.TopK,
                        CreatedAt = modelConfig.CreatedAt
                    }
                };

                return ApiResponse<AIConfigureDetailResponseDto>.Ok(response, "Lấy thông tin cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, $"Lỗi khi lấy thông tin cấu hình AI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllAsync(Guid? userId = null)
        {
            try
            {
                IEnumerable<AI_Configure> aiConfigures;
                
                // Nếu có userId, check role và filter
                if (userId.HasValue)
                {
                    try
                    {
                        var isSystemAdmin = await IsSystemAdminAsync(userId.Value);
                        // Nếu là SystemAdmin, chỉ lấy Kind = Global (0)
                        if (isSystemAdmin)
                        {
                            aiConfigures = await _unitOfWork.AIConfigures.FindAsync(a => a.Kind == Domain.Entitites.Enums.AI_ConfigureKind.Global);
                        }
                        else
                        {
                            // Không phải SystemAdmin, chỉ lấy Kind = Company và cùng CompanyId với user
                            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                            if (user != null && user.CompanyId.HasValue)
                            {
                                aiConfigures = await _unitOfWork.AIConfigures.FindAsync(a => 
                                    a.Kind == Domain.Entitites.Enums.AI_ConfigureKind.Company && 
                                    a.CompanyId == user.CompanyId.Value);
                            }
                            else
                            {
                                // User không có CompanyId, không có AI_Configure nào
                                aiConfigures = new List<AI_Configure>();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GetAllAsync] Error checking user role: {ex.Message}");
                        Console.WriteLine($"[GetAllAsync] StackTrace: {ex.StackTrace}");
                        // Fallback: return empty list if role check fails
                        aiConfigures = new List<AI_Configure>();
                    }
                }
                else
                {
                    aiConfigures = await _unitOfWork.AIConfigures.GetAllAsync();
                }

                var response = new List<AIConfigureResponseDto>();

                foreach (var config in aiConfigures)
                {
                    try
                    {
                        // Không cần load CreatedBy nếu không dùng trong response
                        var dto = new AIConfigureResponseDto
                        {
                            Id = config.Id,
                            Name = config.Name ?? "",
                            Description = config.Description ?? "",
                            CurrentVersion = config.CurrentVersion
                        };
                        response.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GetAllAsync] Error processing config {config.Id}: {ex.Message}");
                        // Skip this config and continue
                        continue;
                    }
                }

                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Ok(response, "Lấy danh sách cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAllAsync] Fatal error: {ex.Message}");
                Console.WriteLine($"[GetAllAsync] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[GetAllAsync] InnerException: {ex.InnerException.Message}");
                }
                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách cấu hình AI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIConfigureDetailResponseDto>> UpdateAsync(Guid id, AIConfigureUpdateDto dto, Guid userId)
        {
            try
            {
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(id);
                if (aiConfigure == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                }

                // Kiểm tra quyền sở hữu (chỉ người tạo mới có thể cập nhật)
                if (aiConfigure.CreatedByUserId != userId)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Bạn không có quyền cập nhật cấu hình AI này");
                }

                // Kiểm tra tên cấu hình AI đã tồn tại chưa (trừ chính nó)
                var existingConfig = await _unitOfWork.AIConfigures.FindAsync(a => a.Name == dto.Name && a.Id != id);
                if (existingConfig.Any())
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Tên cấu hình AI đã tồn tại");
                }

                aiConfigure.Name = dto.Name;
                aiConfigure.Description = dto.Description;
                var baseRuleUpdate = "only answer information in the RAG";
                aiConfigure.Rules = string.IsNullOrWhiteSpace(dto.Rules)
                    ? baseRuleUpdate
                    : string.Concat(baseRuleUpdate, "; ", dto.Rules);

                // Update current version name if provided
                if (!string.IsNullOrWhiteSpace(dto.Version))
                {
                    aiConfigure.CurrentVersion = dto.Version;
                }

                // Handle ModelConfig update: create new or use existing
                if (dto.ModelConfigId.HasValue || dto.ModelConfig != null)
                {
                    AIModelConfig modelConfig;
                    if (dto.ModelConfigId.HasValue)
                    {
                        // Use existing ModelConfig
                        modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(dto.ModelConfigId.Value);
                        if (modelConfig == null)
                        {
                            return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "ModelConfig không tồn tại");
                        }
                        aiConfigure.ModelConfigId = modelConfig.Id;
                    }
                    else if (dto.ModelConfig != null)
                    {
                        // Create new ModelConfig
                        modelConfig = new AIModelConfig
                        {
                            ModelName = dto.ModelConfig.ModelName,
                            Temperature = dto.ModelConfig.Temperature,
                            MaxOutputTokens = dto.ModelConfig.MaxOutputTokens,
                            UseStreaming = dto.ModelConfig.UseStreaming,
                            ApiKey = dto.ModelConfig.ApiKey ?? "", // Có thể set khi tạo, hoặc để trống và set sau
                            TopP = dto.ModelConfig.TopP,
                            TopK = dto.ModelConfig.TopK
                            // Password sẽ được set sau bằng hàm change password riêng
                        };
                        await _unitOfWork.AIModelConfigs.AddAsync(modelConfig);
                        await _unitOfWork.SaveChangesAsync();
                        aiConfigure.ModelConfigId = modelConfig.Id;
                    }
                }

                // Write snapshot with current version name (no auto-increment for string)
                _unitOfWork.AIConfigures.Update(aiConfigure);
                await _unitOfWork.SaveChangesAsync();

                // Load ModelConfig for version snapshot
                var currentModelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(aiConfigure.ModelConfigId);

                var version = new AI_ConfigureVersion
                {
                    AIConfigureId = aiConfigure.Id,
                    Version = aiConfigure.CurrentVersion ?? "v1.0.0",
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    Rules = aiConfigure.Rules,
                    ModelConfigId = aiConfigure.ModelConfigId,
                    RagTopK = aiConfigure.RagTopK,
                    Kind = aiConfigure.Kind,
                    CompanyId = aiConfigure.CompanyId,
                    CreatedByUserId = aiConfigure.CreatedByUserId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AIConfigureVersions.AddAsync(version);
                await _unitOfWork.SaveChangesAsync();

                var createdBy = await _unitOfWork.Users.GetByIdAsync(aiConfigure.CreatedByUserId);
                var response = new AIConfigureDetailResponseDto
                {
                    Id = aiConfigure.Id,
                    Name = aiConfigure.Name,
                    Description = aiConfigure.Description,
                    Rules = aiConfigure.Rules,
                    RagTopK = aiConfigure.RagTopK,
                    CreatedByUserId = aiConfigure.CreatedByUserId,
                    CreatedByUserName = createdBy?.FullName ?? "Unknown",
                    KnowledgeSourceCount = aiConfigure.KnowledgeSources?.Count ?? 0,
                    ChatSessionCount = aiConfigure.ChatSessions?.Count ?? 0,
                    CurrentVersion = aiConfigure.CurrentVersion,
                    ModelConfig = currentModelConfig != null ? new AIModelConfigResponseDto
                    {
                        Id = currentModelConfig.Id,
                        ModelName = currentModelConfig.ModelName,
                        Temperature = currentModelConfig.Temperature,
                        MaxOutputTokens = currentModelConfig.MaxOutputTokens,
                        UseStreaming = currentModelConfig.UseStreaming,
                        TopP = currentModelConfig.TopP,
                        TopK = currentModelConfig.TopK,
                        CreatedAt = currentModelConfig.CreatedAt
                    } : null
                };

                return ApiResponse<AIConfigureDetailResponseDto>.Ok(response, "Cập nhật cấu hình AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, $"Lỗi khi cập nhật cấu hình AI: {ex.Message}");
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

                // Kiểm tra xem cấu hình AI có đang được gán qua bảng liên kết UserAiConfig không
                var linksUsingConfig = await _unitOfWork.UserAiConfigs.FindAsync(uac => uac.AIConfigureId == id);
                if (linksUsingConfig.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không thể xóa cấu hình AI đang được sử dụng bởi người dùng (UserAiConfig)");
                }

                // 1. Xóa tất cả ChatSession và Firebase documents
                var chatSessions = await _unitOfWork.ChatSessions.FindAsync(cs => cs.AIConfigureId == id);
                foreach (var session in chatSessions)
                {
                    // Xóa Firebase document
                    if (!string.IsNullOrEmpty(session.ExternalSessionId))
                    {
                        try
                        {
                            var sessionRef = _firebaseService.Db.Collection("chatSessions").Document(session.ExternalSessionId);
                            await sessionRef.DeleteAsync();
                        }
                        catch (Exception ex)
                        {
                            // Log but continue
                            Console.WriteLine($"Failed to delete Firebase session {session.ExternalSessionId}: {ex.Message}");
                        }
                    }
                    // Xóa từ database
                    _unitOfWork.ChatSessions.Delete(session);
                }

                // 2. Xóa KnowledgeSource và Qdrant collection
                try
                {
                    await _knowledgeSourceService.DeleteByAIConfigureIdAsync(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete KnowledgeSource and Qdrant collection: {ex.Message}");
                    // Continue - có thể collection đã không tồn tại
                }

                // 3. Xóa AI_ConfigureCompany links và các department links liên quan
                var companyLinks = await _unitOfWork.AIConfigureCompanies.FindAsync(acc => acc.AIConfigureId == id);
                // Lấy AIConfigureCompanyId từ các company links trước khi xóa
                var aiConfigureCompanyIds = companyLinks.Select(l => l.Id).ToList();
                if (aiConfigureCompanyIds.Any())
                {
                    // Xóa AI_ConfigureCompanyDepartment links trước
                    var deptLinks = await _unitOfWork.AIConfigureCompanyDepartments.FindAsync(accd => aiConfigureCompanyIds.Contains(accd.AIConfigureCompanyId));
                    foreach (var deptLink in deptLinks)
                    {
                        _unitOfWork.AIConfigureCompanyDepartments.Delete(deptLink);
                    }
                }
                // Sau đó xóa AI_ConfigureCompany links
                foreach (var link in companyLinks)
                {
                    _unitOfWork.AIConfigureCompanies.Delete(link);
                }

                // 4. Xóa AI_ConfigureVersion records
                var versions = await _unitOfWork.AIConfigureVersions.FindAsync(v => v.AIConfigureId == id);
                foreach (var version in versions)
                {
                    _unitOfWork.AIConfigureVersions.Delete(version);
                }

                // 5. Xóa AI_Configure entity
                _unitOfWork.AIConfigures.Delete(aiConfigure);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa cấu hình AI và tất cả dữ liệu liên quan thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa cấu hình AI: {ex.Message}");
            }
        }


        public async Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllForUserAsync(Guid userId)
        {
            try
            {
                var result = new Dictionary<Guid, AIConfigureResponseDto>();

                // 1) AIs created by user
                var createdConfigs = await _unitOfWork.AIConfigures.FindAsync(a => a.CreatedByUserId == userId);
                foreach (var cfg in createdConfigs)
                {
                    if (!result.ContainsKey(cfg.Id))
                    {
                        result[cfg.Id] = new AIConfigureResponseDto { Id = cfg.Id, Name = cfg.Name, Description = cfg.Description };
                    }
                }

                // 2) AIs granted via UserAiConfig links
                var links = await _unitOfWork.UserAiConfigs.GetByUserIdAsync(userId);
                foreach (var link in links)
                {
                    var cfg = await _unitOfWork.AIConfigures.GetByIdAsync(link.AIConfigureId);
                    if (cfg != null && !result.ContainsKey(cfg.Id))
                    {
                        result[cfg.Id] = new AIConfigureResponseDto { Id = cfg.Id, Name = cfg.Name, Description = cfg.Description };
                    }
                }

                // 3) AIs granted via AI_ConfigureCompany (check if user is Admin and company has access)
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user != null && user.CompanyId.HasValue)
                {
                    // Check if user has Admin role
                    var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == userId);
                    bool isAdmin = false;
                    
                    foreach (var ud in userDepartments)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                        if (role != null && string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            isAdmin = true;
                            break;
                        }
                    }

                    // If user is Admin and has CompanyId, check AI_ConfigureCompany links
                    if (isAdmin)
                    {
                        var companyLinks = await _unitOfWork.AIConfigureCompanies.FindAsync(l => l.CompanyId == user.CompanyId.Value);
                        foreach (var link in companyLinks)
                        {
                            var cfg = await _unitOfWork.AIConfigures.GetByIdAsync(link.AIConfigureId);
                            if (cfg != null && !result.ContainsKey(cfg.Id))
                            {
                                result[cfg.Id] = new AIConfigureResponseDto 
                                { 
                                    Id = cfg.Id, 
                                    Name = cfg.Name, 
                                    Description = cfg.Description,
                                    CurrentVersion = cfg.CurrentVersion
                                };
                            }
                        }
                    }
                }

                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Ok(result.Values.ToList(), "Danh sách AI khả dụng của người dùng");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách AI của người dùng: {ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> RecalculateRagTopKAsync(Guid aiConfigureId)
        {
            try
            {
                var ai = await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureId);
                if (ai == null)
                {
                    return ApiResponse<int>.Fail(0, "Không tìm thấy cấu hình AI");
                }

                // Đếm số lượng file nguồn phân biệt. Ở đây dùng Title (tên file gốc) nếu có, nếu không fallback Source
                var sources = await _unitOfWork.KnowledgeSources.FindAsync(k => k.AIConfigureId == aiConfigureId);
                var distinctFiles = sources
                    .Select(k => string.IsNullOrWhiteSpace(k.Title) ? (k.Source ?? string.Empty) : k.Title)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                // Quy tắc: topK = clamp(1, 5, distinctFiles)
                var newTopK = Math.Max(1, Math.Min(5, distinctFiles));
                if (ai.RagTopK != newTopK)
                {
                    ai.RagTopK = newTopK;
                    _unitOfWork.AIConfigures.Update(ai);
                    await _unitOfWork.SaveChangesAsync();
                }

                return ApiResponse<int>.Ok(newTopK, "Đã cập nhật RagTopK theo số lượng file");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail(0, $"Lỗi khi tính RagTopK: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllByKindAsync(Domain.Entitites.Enums.AI_ConfigureKind kind, Guid? userId = null)
        {
            try
            {
                IEnumerable<AI_Configure> aiConfigures;
                
                // Nếu có userId và là SystemAdmin, chỉ lấy Kind = Global (0)
                if (userId.HasValue)
                {
                    var isSystemAdmin = await IsSystemAdminAsync(userId.Value);
                    if (isSystemAdmin)
                    {
                        // SystemAdmin chỉ được xem Global
                        if (kind != Domain.Entitites.Enums.AI_ConfigureKind.Global)
                        {
                            return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Ok(new List<AIConfigureResponseDto>(), $"SystemAdmin chỉ được xem Global");
                        }
                        aiConfigures = await _unitOfWork.AIConfigures.FindAsync(a => a.Kind == Domain.Entitites.Enums.AI_ConfigureKind.Global);
                    }
                    else
                    {
                        // Không phải SystemAdmin, filter theo Kind và CompanyId của user
                        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                        if (user != null && user.CompanyId.HasValue)
                        {
                            aiConfigures = await _unitOfWork.AIConfigures.FindAsync(a => 
                                a.Kind == kind && 
                                a.CompanyId == user.CompanyId.Value);
                        }
                        else
                        {
                            // User không có CompanyId, không có AI_Configure nào
                            aiConfigures = new List<AI_Configure>();
                        }
                    }
                }
                else
                {
                    aiConfigures = await _unitOfWork.AIConfigures.FindAsync(a => a.Kind == kind);
                }

                var response = new List<AIConfigureResponseDto>();

                foreach (var config in aiConfigures)
                {
                    response.Add(new AIConfigureResponseDto
                    {
                        Id = config.Id,
                        Name = config.Name,
                        Description = config.Description
                    });
                }

                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Ok(response, $"Lấy danh sách cấu hình AI ({kind}) thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<AIConfigureResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách cấu hình AI: {ex.Message}");
            }
        }

        // Versioning APIs
        public async Task<ApiResponse<IEnumerable<(string version, DateTime createdAt)>>> GetVersionsAsync(Guid aiConfigureId)
        {
            try
            {
                var versions = await _unitOfWork.AIConfigureVersions.FindAsync(v => v.AIConfigureId == aiConfigureId);
                var result = versions
                    .OrderBy(v => v.CreatedAt)
                    .Select(v => (v.Version, v.CreatedAt))
                    .ToList();
                return ApiResponse<IEnumerable<(string, DateTime)>>.Ok(result, "Danh sách version");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<(string, DateTime)>>.Fail(null, $"Lỗi khi lấy versions: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIConfigureDetailResponseDto>> GetVersionAsync(Guid aiConfigureId, string version)
        {
            try
            {
                var v = (await _unitOfWork.AIConfigureVersions.FindAsync(x => x.AIConfigureId == aiConfigureId && x.Version == version)).FirstOrDefault();
                if (v == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy version");
                }

                // Load ModelConfig from version snapshot
                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(v.ModelConfigId);
                if (modelConfig == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "ModelConfig không tồn tại cho version này");
                }

                var dto = new AIConfigureDetailResponseDto
                {
                    Id = aiConfigureId,
                    Name = v.Name,
                    Description = v.Description,
                    Rules = v.Rules,
                    RagTopK = v.RagTopK,
                    CreatedByUserId = v.CreatedByUserId,
                    CreatedByUserName = string.Empty,
                    KnowledgeSourceCount = 0,
                    ChatSessionCount = 0,
                    CurrentVersion = v.Version,
                    ModelConfig = new AIModelConfigResponseDto
                    {
                        Id = modelConfig.Id,
                        ModelName = modelConfig.ModelName,
                        Temperature = modelConfig.Temperature,
                        MaxOutputTokens = modelConfig.MaxOutputTokens,
                        UseStreaming = modelConfig.UseStreaming,
                        TopP = modelConfig.TopP,
                        TopK = modelConfig.TopK,
                        CreatedAt = modelConfig.CreatedAt
                    }
                };
                return ApiResponse<AIConfigureDetailResponseDto>.Ok(dto, "Chi tiết version");
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, $"Lỗi khi lấy version: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AIConfigureDetailResponseDto>> RollbackAsync(Guid aiConfigureId, string version, Guid userId)
        {
            try
            {
                var ai = await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureId);
                if (ai == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                }

                // Permission: only creator can rollback
                if (ai.CreatedByUserId != userId)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Bạn không có quyền rollback cấu hình này");
                }

                var v = (await _unitOfWork.AIConfigureVersions.FindAsync(x => x.AIConfigureId == aiConfigureId && x.Version == version)).FirstOrDefault();
                if (v == null)
                {
                    return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, "Không tìm thấy version để rollback");
                }

                // Apply snapshot into current - use ModelConfigId from version
                ai.Name = v.Name;
                ai.Description = v.Description;
                ai.Rules = v.Rules;
                ai.ModelConfigId = v.ModelConfigId; // Restore ModelConfigId from version
                ai.RagTopK = v.RagTopK;
                ai.Kind = v.Kind;
                ai.CompanyId = v.CompanyId;
                ai.CurrentVersion = v.Version;

                _unitOfWork.AIConfigures.Update(ai);
                await _unitOfWork.SaveChangesAsync();

                var newV = new AI_ConfigureVersion
                {
                    AIConfigureId = ai.Id,
                    Version = ai.CurrentVersion,
                    Name = ai.Name,
                    Description = ai.Description,
                    Rules = ai.Rules,
                    ModelConfigId = ai.ModelConfigId,
                    RagTopK = ai.RagTopK,
                    Kind = ai.Kind,
                    CompanyId = ai.CompanyId,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AIConfigureVersions.AddAsync(newV);
                await _unitOfWork.SaveChangesAsync();

                return await GetByIdAsync(ai.Id);
            }
            catch (Exception ex)
            {
                return ApiResponse<AIConfigureDetailResponseDto>.Fail(null, $"Lỗi khi rollback: {ex.Message}");
            }
        }
    }
}