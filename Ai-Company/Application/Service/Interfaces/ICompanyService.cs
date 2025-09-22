using Application.Service.Models;
using Domain.Entitites;

namespace Application.Service.Interfaces
{
    public interface ICompanyService
    {
        Task<ApiResponse<CompanyResponseDto>> CreateAsync(CompanyCreateDto dto);
        Task<ApiResponse<CompanyResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<IEnumerable<CompanyResponseDto>>> GetAllAsync();
        Task<ApiResponse<CompanyResponseDto>> UpdateAsync(Guid id, CompanyUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
