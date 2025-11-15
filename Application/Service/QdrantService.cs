using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository;
using Microsoft.Extensions.Configuration;

namespace Application.Service
{
    public class QdrantService
    {
        private readonly    HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IUnitOfWork _unitofwork;
        public QdrantService(IUnitOfWork unitOfWork, HttpClient httpClient, IConfiguration configuration)
        {
            // Ưu tiên đọc từ environment variable (Render format)
            var qdrantBaseUrl = System.Environment.GetEnvironmentVariable("QDRANT__BASE_URL")
                ?? configuration["QDRANT__BASE_URL"]
                ?? configuration["Qdrant:BaseUrl"]
                ?? "http://localhost:6333";
            
            Console.WriteLine($"[QdrantService] QDRANT__BASE_URL env var: {System.Environment.GetEnvironmentVariable("QDRANT__BASE_URL")}");
            Console.WriteLine($"[QdrantService] Config QDRANT__BASE_URL: {configuration["QDRANT__BASE_URL"]}");
            Console.WriteLine($"[QdrantService] Config Qdrant:BaseUrl: {configuration["Qdrant:BaseUrl"]}");
            Console.WriteLine($"[QdrantService] Resolved qdrantBaseUrl: {qdrantBaseUrl}");
            
            _baseUrl = qdrantBaseUrl.TrimEnd('/');
            _httpClient = httpClient;
            _unitofwork = unitOfWork;
        }

        #region Collection Management

        public async Task CreateCollectionAsync(string collectionName, int vectorSize = 3072, string distance = "Cosine")
        {
            var payload = new
            {
                vectors = new
                {
                    size = vectorSize,
                    distance = distance
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/collections/{collectionName}", content);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                // Collection already exists; let caller decide if this is acceptable
                return;
            }
            response.EnsureSuccessStatusCode();
        }

        public async Task EnsureCollectionAsync(string collectionName, int vectorSize, string distance = "Cosine", bool recreateOnMismatch = true)
        {
            var existing = await GetCollectionAsync(collectionName);
            if (existing == null)
            {
                await CreateCollectionAsync(collectionName, vectorSize, distance);
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(existing);
                var root = doc.RootElement;
                // Qdrant response shape: { result: { config: { params: { vectors: { size: <int> }}}}}
                var result = root.GetProperty("result");
                var size = result.GetProperty("config")
                               .GetProperty("params")
                               .GetProperty("vectors")
                               .GetProperty("size")
                               .GetInt32();

                if (size != vectorSize && recreateOnMismatch)
                {
                    await DeleteCollectionAsync(collectionName);
                    await CreateCollectionAsync(collectionName, vectorSize, distance);
                    return;
                }

                // Check if collection has points but no indexed vectors (corrupt state)
                if (result.TryGetProperty("points_count", out var pointsCount) &&
                    result.TryGetProperty("indexed_vectors_count", out var indexedCount))
                {
                    var points = pointsCount.GetInt64();
                    var indexed = indexedCount.GetInt64();
                    if (points > 0 && indexed == 0)
                    {
                        Console.WriteLine($"[QdrantService] WARNING: Collection '{collectionName}' has {points} points but 0 indexed vectors. This may indicate corruption.");
                        // Optionally recreate collection if it's in a bad state
                        // Uncomment if you want to auto-fix:
                        // Console.WriteLine($"[QdrantService] Recreating collection to fix indexing issue...");
                        // await DeleteCollectionAsync(collectionName);
                        // await CreateCollectionAsync(collectionName, vectorSize, distance);
                    }
                }
            }
            catch
            {
                // If parsing fails, do nothing (assume compatible). Caller will see errors on insert if mismatched
            }
        }

        public async Task<string> GetCollectionAsync(string collectionName)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/collections/{collectionName}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task DeleteCollectionAsync(string collectionName)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/collections/{collectionName}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> ListCollectionsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/collections");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        #endregion

        #region Points Management

        public async Task AddPointAsync(string collectionName, ulong id, float[] vector, object payload)
        {
            // Validate vector
            if (vector == null || vector.Length == 0)
            {
                throw new ArgumentException("Vector cannot be null or empty");
            }

            // Validate vector for NaN or Infinity values
            for (int i = 0; i < vector.Length; i++)
            {
                if (float.IsNaN(vector[i]) || float.IsInfinity(vector[i]))
                {
                    throw new ArgumentException($"Vector contains invalid value at index {i}: {vector[i]}");
                }
            }

            var point = new
            {
                id = id,
                vector = vector,
                payload
            };

            var content = new StringContent(JsonSerializer.Serialize(new { points = new[] { point } }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/collections/{collectionName}/points", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[QdrantService] AddPoint failed - Status: {response.StatusCode}, Response: {errorBody}");
                throw new HttpRequestException($"Qdrant AddPoint failed with status {response.StatusCode}: {errorBody}");
            }
            
            // Wait a bit for indexing (Qdrant usually indexes quickly, but we ensure it's done)
            await Task.Delay(100);
        }

        public async Task UpdatePointAsync(string collectionName, ulong id, float[] vector = null, object payload = null)
        {
            var point = new Dictionary<string, object> { { "id", id } };
            if (vector != null) point.Add("vector", vector);
            if (payload != null) point.Add("payload", payload);

            var content = new StringContent(JsonSerializer.Serialize(new { points = new[] { point } }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/points", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> GetPointAsync(string collectionName, ulong id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/collections/{collectionName}/points/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task DeletePointsAsync(string collectionName, ulong[] ids)
        {
            var content = new StringContent(JsonSerializer.Serialize(new { ids }), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/collections/{collectionName}/points") { Content = content });
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> ScrollPointsAsync(string collectionName, int limit = 100)
        {
            var payload = new { limit };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/points/scroll", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<long> CountPointsAsync(string collectionName)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/points/count", new StringContent("{}", Encoding.UTF8, "application/json"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return 0; // Collection doesn't exist
            }
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("result", out var result) && result.TryGetProperty("count", out var count))
            {
                return count.GetInt64();
            }
            return 0;
        }

        #endregion

        #region Search

        public async Task<string> SearchAsync(string collectionName, float[] vector, object filter = null, int? limit = null, bool withPayload = true)
        {
            if (vector == null || vector.Length == 0)
            {
                throw new ArgumentException("Vector cannot be null or empty");
            }

            // Validate vector for NaN or Infinity values
            for (int i = 0; i < vector.Length; i++)
            {
                if (float.IsNaN(vector[i]) || float.IsInfinity(vector[i]))
                {
                    throw new ArgumentException($"Vector contains invalid value at index {i}: {vector[i]}");
                }
            }

            var collectionInfo = await GetCollectionAsync(collectionName);
            if (string.IsNullOrWhiteSpace(collectionInfo))
            {
                throw new ArgumentException($"Collection '{collectionName}' does not exist");
            }

            int? collectionVectorSize = null;
            try
            {
                using var doc = JsonDocument.Parse(collectionInfo);
                var root = doc.RootElement;
                if (root.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("config", out var config) &&
                    config.TryGetProperty("params", out var @params) &&
                    @params.TryGetProperty("vectors", out var vectors))
                {
                    if (vectors.ValueKind == JsonValueKind.Object)
                    {
                        if (vectors.TryGetProperty("named", out var named))
                        {
                            foreach (var prop in named.EnumerateObject())
                            {
                                if (prop.Value.TryGetProperty("size", out var sizeElement))
                                {
                                    collectionVectorSize = sizeElement.GetInt32();
                                    break;
                                }
                            }
                        }
                        else if (vectors.TryGetProperty("size", out var sizeElement))
                        {
                            collectionVectorSize = sizeElement.GetInt32();
                        }
                    }
                }

                if (collectionVectorSize.HasValue && vector.Length != collectionVectorSize.Value)
                {
                    throw new ArgumentException($"Vector dimension mismatch: query vector has {vector.Length} dimensions but collection expects {collectionVectorSize.Value}");
                }
            }
            catch (JsonException)
            {
                // Ignore parsing errors, continue with best effort
            }

            long pointCount = await CountPointsAsync(collectionName);
            if (pointCount == 0)
            {
                throw new InvalidOperationException($"Collection '{collectionName}' has no points to search");
            }

            // Check if vectors are indexed - if not, wait a bit and retry
            try
            {
                using var collectionDoc = JsonDocument.Parse(collectionInfo);
                var collectionRoot = collectionDoc.RootElement;
                if (collectionRoot.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("indexed_vectors_count", out var indexedCount))
                {
                    var indexed = indexedCount.GetInt64();
                    if (indexed == 0 && pointCount > 0)
                    {
                        Console.WriteLine($"[QdrantService] WARNING: Collection has {pointCount} points but 0 indexed vectors. Waiting for indexing...");
                        // Wait up to 5 seconds for indexing
                        for (int i = 0; i < 10; i++)
                        {
                            await Task.Delay(500);
                            var updatedInfo = await GetCollectionAsync(collectionName);
                            if (!string.IsNullOrWhiteSpace(updatedInfo))
                            {
                                using var updatedDoc = JsonDocument.Parse(updatedInfo);
                                var updatedRoot = updatedDoc.RootElement;
                                if (updatedRoot.TryGetProperty("result", out var updatedResult) &&
                                    updatedResult.TryGetProperty("indexed_vectors_count", out var updatedIndexed))
                                {
                                    var updatedIndexedCount = updatedIndexed.GetInt64();
                                    if (updatedIndexedCount > 0)
                                    {
                                        Console.WriteLine($"[QdrantService] Indexing completed: {updatedIndexedCount} vectors indexed");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors, proceed with search
            }

            var payload = new Dictionary<string, object>
            {
                { "vector", vector },
                { "limit", limit ?? 10 },
                { "with_payload", withPayload }
            };

            if (filter != null)
            {
                payload.Add("filter", filter);
            }

            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            // Log request for debugging
            Console.WriteLine($"[QdrantService] Search request - Collection: {collectionName}, Vector size: {vector.Length}, Limit: {limit ?? 10}");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/collections/{collectionName}/points/search", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[QdrantService] Search failed - Status: {response.StatusCode}, Response: {errorBody}");
                Console.WriteLine($"[QdrantService] Request payload (first 500 chars): {jsonPayload.Substring(0, Math.Min(500, jsonPayload.Length))}");
                
                // Try to parse error response for more details
                try
                {
                    using var errorDoc = JsonDocument.Parse(errorBody);
                    var errorRoot = errorDoc.RootElement;
                    if (errorRoot.TryGetProperty("status", out var status))
                    {
                        var statusText = status.GetRawText();
                        Console.WriteLine($"[QdrantService] Qdrant error status: {statusText}");
                    }
                    if (errorRoot.TryGetProperty("error", out var error))
                    {
                        var errorText = error.GetRawText();
                        Console.WriteLine($"[QdrantService] Qdrant error message: {errorText}");
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
                
                throw new HttpRequestException($"Qdrant search failed with status {response.StatusCode}: {errorBody}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        #endregion

        #region Advanced

        public async Task UpdateCollectionConfigAsync(string collectionName, object config)
        {
            var content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"{_baseUrl}/collections/{collectionName}", content);
            response.EnsureSuccessStatusCode();
        }

        #endregion
    }
}
