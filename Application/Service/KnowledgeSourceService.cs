using Application.Service.Interfaces;
using Application.Service.Models;
using Infrastructure.Repository.Interfaces;

namespace Application.Service
{
    public class KnowledgeSourceService : IKnowledgeSourceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly QdrantService _qdrantService;

        public KnowledgeSourceService(IUnitOfWork unitOfWork, QdrantService qdrantService)
        {
            _unitOfWork = unitOfWork;
            _qdrantService = qdrantService;
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetBySourceAsync(string source)
        {
            try
            {
                var list = await _unitOfWork.KnowledgeSources.FindAsync(k => k.Source == source);
                var result = list.Select(k => new { k.Id, k.Type, k.Source, k.Title, k.PageNumber, k.ChunkIndex, k.TotalChunks, k.AIConfigureId });
                return ApiResponse<IEnumerable<object>>.Ok(result, "Danh sách knowledge theo source");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<object>>.Fail(null, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetByAIConfigureIdAsync(Guid aiConfigureId)
        {
            try
            {
                var list = await _unitOfWork.KnowledgeSources.FindAsync(k => k.AIConfigureId == aiConfigureId);
                var result = list.Select(k => new { k.Id, k.Type, k.Source, k.Title, k.PageNumber, k.ChunkIndex, k.TotalChunks });
                return ApiResponse<IEnumerable<object>>.Ok(result, "Danh sách knowledge theo AI");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<object>>.Fail(null, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteBySourceAsync(string source)
        {
            try
            {
                var items = await _unitOfWork.KnowledgeSources.FindAsync(k => k.Source == source);
                if (!items.Any()) return ApiResponse<bool>.Ok(false, "Không tìm thấy record");

                _unitOfWork.KnowledgeSources.RemoveRange(items);
                await _unitOfWork.SaveChangesAsync();

                // Không xóa collection khi xóa theo từng source (tránh mất dữ liệu các file khác)
                return ApiResponse<bool>.Ok(true, "Đã xóa KnowledgeSource theo source (Qdrant giữ nguyên collection)");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteByAIConfigureIdAsync(Guid aiConfigureId)
        {
            try
            {
                var list = await _unitOfWork.KnowledgeSources.FindAsync(k => k.AIConfigureId == aiConfigureId);
                if (list.Any())
                {
                    _unitOfWork.KnowledgeSources.RemoveRange(list);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Xóa collection tương ứng trong Qdrant
                await _qdrantService.DeleteCollectionAsync(aiConfigureId.ToString());

                return ApiResponse<bool>.Ok(true, "Đã xóa KnowledgeSource và collection Qdrant theo AIConfigureId");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi: {ex.Message}");
            }
        }
    }
}


