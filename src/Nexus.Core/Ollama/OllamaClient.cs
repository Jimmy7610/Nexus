using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nexus.Core.Models;

namespace Nexus.Core.Ollama
{
    public interface IOllamaClient
    {
        Task<string> GetRecommendationAsync(string prompt, SystemMetrics metrics, IEnumerable<DiskMetrics> disks);
        Task<List<string>> GetAvailableModelsAsync();
    }

    public class OllamaClient : IOllamaClient
    {
        private static readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };
        private readonly List<string> _fallbackModelOrder = new List<string> { "llama3:latest", "mistral:latest", "phi3:latest" };

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                if (!response.IsSuccessStatusCode) return new List<string>();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<OllamaTagsResponse>(content);
                var models = new List<string>();
                foreach (var m in data.Models) models.Add(m.Name);
                return models;
            }
            catch { return new List<string>(); }
        }

        public async Task<string> GetRecommendationAsync(string prompt, SystemMetrics metrics, IEnumerable<DiskMetrics> disks)
        {
            var availableModels = await GetAvailableModelsAsync();
            string selectedModel = "llama3:latest";

            foreach (var fallback in _fallbackModelOrder)
            {
                if (availableModels.Contains(fallback))
                {
                    selectedModel = fallback;
                    break;
                }
            }

            var context = BuildSystemContext(metrics, disks);
            var fullPrompt = $"System Context:\n{context}\n\nUser Question: {prompt}\n\nAnswer concisely as a system monitoring assistant.";

            var requestBody = new
            {
                model = selectedModel,
                prompt = fullPrompt,
                stream = false
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var response = await _httpClient.PostAsync("/api/generate", new StringContent(json, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return "Ollama server error.";

                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OllamaGenerateResponse>(resultJson);
                return result.Response;
            }
            catch (Exception ex)
            {
                return $"Error connecting to Ollama: {ex.Message}";
            }
        }

        private string BuildSystemContext(SystemMetrics metrics, IEnumerable<DiskMetrics> disks)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"- CPU Usage: {metrics.CpuUsage}%");
            sb.AppendLine($"- RAM: {metrics.RamUsedGb}/{metrics.RamTotalGb} GB used");
            foreach (var d in disks)
            {
                sb.AppendLine($"- Disk {d.DriveName}: {d.UsagePercentage:F1}% full");
            }
            return sb.ToString();
        }
    }

    public class OllamaTagsResponse { public List<OllamaModel>? Models { get; set; } }
    public class OllamaModel { [JsonProperty("name")] public string? Name { get; set; } }
    public class OllamaGenerateResponse { [JsonProperty("response")] public string? Response { get; set; } }
}
