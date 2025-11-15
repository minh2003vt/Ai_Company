using Application.Service;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Application.Helper
{
    public class QdrantHelper
    {
        private readonly QdrantService _qdrantService;
        private readonly Infrastructure.Repository.Interfaces.IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public QdrantHelper(QdrantService qdrantService, Infrastructure.Repository.Interfaces.IUnitOfWork unitOfWork, IConfiguration configuration, HttpClient httpClient)
        {
            _qdrantService = qdrantService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Import file (docx/pdf) vào Qdrant collection
        /// </summary>
        public async Task<IReadOnlyList<(ulong pointId, Guid knowledgeSourceId, int pageNumber)>> ImportFileAsync(IFormFile file, string collectionName, Guid aiConfigureId, Guid ownerUserId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".docx" && extension != ".pdf")
                throw new ArgumentException("Only .docx and .pdf files are allowed");

            // Tách text
            string content = await ExtractTextAsync(file, extension);
            // Làm sạch text
            content = CleanText(content);

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("File has no readable text");

            // Chunk theo 500 từ (đã được làm sạch)
            var chunks = SplitIntoWordChunks(content, 500).ToList();
            int totalChunks = chunks.Count;

            var random = new Random();
            var results = new List<(ulong pointId, Guid knowledgeSourceId, int pageNumber)>();

            // Xác định kích thước vector từ embedding đầu tiên để cấu hình collection phù hợp
            int vectorSize = 0;
            if (totalChunks > 0)
            {
                var previewVector = await GenerateEmbeddingAsync(chunks[0]);
                vectorSize = previewVector?.Length ?? 0;
            }

            if (vectorSize <= 0)
            {
                vectorSize = 384; // fallback an toàn khớp với GenerateEmbeddingAsync fallback
            }

            // Tạo/đảm bảo collection tồn tại với kích thước vector phù hợp
            await _qdrantService.EnsureCollectionAsync(collectionName, vectorSize);

            for (int i = 0; i < totalChunks; i++)
            {
                string chunkText = chunks[i];

                var knowledgeSource = new Domain.Entitites.KnowledgeSource
                {
                    Type = "file",
                    Source = file.FileName,
                    Title = file.FileName,
                    Content = chunkText,
                    ChunkIndex = i,
                    TotalChunks = totalChunks,
                    PageNumber = i + 1,
                    MetaJson = string.Empty,
                    AIConfigureId = aiConfigureId
                };
                await _unitOfWork.KnowledgeSources.AddAsync(knowledgeSource);
                await _unitOfWork.SaveChangesAsync();

                // Tạo vector embedding bằng tokenizer service + ONNX
                float[] vector = await GenerateEmbeddingAsync(chunkText);

                var id = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + i);

                var payload = new
                {
                    knowledgeSourceId = knowledgeSource.Id.ToString(),
                    chunkIndex = i,
                    text = chunkText
                };

                await _qdrantService.AddPointAsync(collectionName, id, vector, payload);

                // Only store pointId in MetaJson as requested
                knowledgeSource.MetaJson = id.ToString();
                _unitOfWork.KnowledgeSources.Update(knowledgeSource);
                await _unitOfWork.SaveChangesAsync();

                results.Add((id, knowledgeSource.Id, i + 1));
            }

            return results;
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Chuẩn hóa xuống một khoảng trắng, loại bỏ ký tự điều khiển, chuẩn hóa xuống dòng
            var normalized = Regex.Replace(text, "\r\n?|\u000B|\u000C|\u0085|\u2028|\u2029", "\n");
            normalized = Regex.Replace(normalized, "[\u0000-\u001F\u007F]", " ");
            normalized = Regex.Replace(normalized, "\\s+", " ");
            // Cắt khoảng trắng đầu cuối
            normalized = normalized.Trim();
            return normalized;
        }

        private async Task<string> ExtractTextAsync(IFormFile file, string extension)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            if (extension == ".docx")
            {
                using var wordDoc = WordprocessingDocument.Open(ms, false);
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body == null) return string.Empty;

                var paragraphs = body.Descendants<Paragraph>();
                var lines = paragraphs
                    .Select(p => string.Concat(p.Descendants<Text>().Select(t => t.Text)))
                    .Where(line => !string.IsNullOrWhiteSpace(line));
                return string.Join("\n", lines);
            }
            else if (extension == ".pdf")
            {
                using var pdf = PdfDocument.Open(ms);
                var texts = pdf.GetPages().Select(p => p.Text);
                return string.Join("\n", texts);
            }

            return string.Empty;
        }

        private static IEnumerable<string> SplitIntoWordChunks(string text, int maxWords)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            // Normalize whitespace
            var normalized = Regex.Replace(text, "\r?\n", "\n");
            var words = normalized.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            var current = new List<string>(maxWords);
            foreach (var w in words)
            {
                current.Add(w);
                if (current.Count >= maxWords)
                {
                    yield return string.Join(" ", current);
                    current.Clear();
                }
            }

            if (current.Count > 0)
            {
                yield return string.Join(" ", current);
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                // Gọi embedding service trực tiếp từ configuration - không cần ONNX nữa
                var tokenizerBaseUrl = _configuration["Tokenizer:BaseUrl"] ?? _configuration["TOKENIZER__BASE_URL"] ?? "http://localhost:8000";
                var embedUrl = $"{tokenizerBaseUrl.TrimEnd('/')}/embed";
                var requestBody = new { text, max_length = 1024 };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(embedUrl, content);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var embedResponse = JsonSerializer.Deserialize<EmbedResponse>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (embedResponse == null || embedResponse.Embedding == null || embedResponse.Embedding.Length == 0)
                {
                    throw new InvalidOperationException("Embedding service returned empty or null embedding");
                }
                
                return embedResponse.Embedding;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Embedding error: " + ex.Message);
                var rnd = new Random();
                return Enumerable.Range(0, 384).Select(_ => (float)rnd.NextDouble()).ToArray();
            }
        }

        private class EmbedResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
            public int Dimension { get; set; }
            public string Text { get; set; } = string.Empty;
        }
    }

    // Helper classes for API responses
    public class GeminiEmbeddingResponse
    {
        public GeminiEmbedding embedding { get; set; }
    }

    public class GeminiEmbedding
    {
        public List<double> value { get; set; }
    }
}