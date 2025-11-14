using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xceed.Words.NET;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace Application.Service
{
    public class DocumentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _weaviateUrl;

        public DocumentService(HttpClient httpClient, string weaviateUrl = "http://localhost:8080")
        {
            _httpClient = httpClient;
            _weaviateUrl = weaviateUrl;
        }

        // Extract text
        public string ExtractText(string filePath)
        {
            if (filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = DocX.Load(filePath);
                return doc.Text;
            }
            else if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var text = "";
                using var pdf = new PdfDocument(new PdfReader(filePath));
                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    text += PdfTextExtractor.GetTextFromPage(pdf.GetPage(i)) + " ";
                }
                return text;
            }
            else
            {
                throw new Exception("Unsupported file type");
            }
        }

        // Chunk text with overlap
        public List<string> ChunkText(string text, int chunkSize = 500, int overlap = 50)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            int i = 0;
            while (i < words.Length)
            {
                var chunkWords = words.Skip(i).Take(chunkSize);
                chunks.Add(string.Join(' ', chunkWords));
                i += chunkSize - overlap;
            }
            return chunks;
        }

        // Simulated embedding for testing (replace with OpenAI later)
        public float[] GetDummyEmbedding(string chunk)
        {
            var rnd = new Random();
            return Enumerable.Range(0, 1536).Select(_ => (float)rnd.NextDouble()).ToArray();
        }

        // Store chunks in Weaviate
        public async Task StoreInWeaviateAsync(string filePath, Guid aiConfigId)
        {
            var text = ExtractText(filePath);
            var chunks = ChunkText(text);

            foreach (var chunk in chunks)
            {
                var vector = GetDummyEmbedding(chunk);
                var doc = new
                {
                    @class = "Document", // "class" is a reserved word, so prefix with @
                    properties = new
                    {
                        text = chunk,
                        source = System.IO.Path.GetFileName(filePath),
                        aiConfigId = aiConfigId
                    },
                    vector = vector
                };

                await _httpClient.PostAsJsonAsync($"{_weaviateUrl}/v1/objects", doc);
            }
        }
    }
}
