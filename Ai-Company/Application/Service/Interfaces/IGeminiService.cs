using Domain.Entitites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Service.GeminiService;

namespace Application.Service.Interfaces
{
    public interface IGeminiService
    {
        Task<string> GenerateContentAsync(List<Chat> history);
    }
}
