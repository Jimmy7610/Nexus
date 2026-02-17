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
        Task<string> GetRecommendationAsync(string prompt, SystemMetrics metrics, IEnumerable<DiskMetrics> disks, StorageAnalysisResult? lastScan = null);
        Task<List<string>> GetAvailableModelsAsync();
        Task<(bool isOnline, string activeModel)> GetStatusAsync();
    }

    public class OllamaClient : IOllamaClient
    {
        private static readonly HttpClient _httpClient = new HttpClient 
        { 
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        private readonly List<string> _fallbackModelOrder = new List<string> { "llama3:latest", "mistral:latest", "phi3:latest", "llama2:latest" };

        public async Task<(bool isOnline, string activeModel)> GetStatusAsync()
        {
            try
            {
                var models = await GetAvailableModelsAsync();
                if (models.Count == 0) return (false, "OFFLINE");

                foreach (var fallback in _fallbackModelOrder)
                {
                    if (models.Contains(fallback)) return (true, fallback);
                }

                return (true, models[0]); // Return first available if no fallbacks found
            }
            catch { return (false, "CONNECTION ERROR"); }
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                if (!response.IsSuccessStatusCode) return new List<string>();

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<OllamaTagsResponse>(content);
                var models = new List<string>();
                if (data?.Models != null)
                {
                    foreach (var m in data.Models) if (m.Name != null) models.Add(m.Name);
                }
                return models;
            }
            catch { return new List<string>(); }
        }

        public async Task<string> GetRecommendationAsync(string prompt, SystemMetrics metrics, IEnumerable<DiskMetrics> disks, StorageAnalysisResult? lastScan = null)
        {
            var status = await GetStatusAsync();
            if (!status.isOnline) return "Ollama server is offline. Please start Ollama to enable AI diagnostics.";

            var context = BuildSystemContext(metrics, disks, lastScan);
            var fullPrompt = $"[SYSTEM_HUD_CONTEXT]\n{context}\n\n[USER_QUERY]\n{prompt}\n\n[INSTRUCTION]\nAnswer as NEXUS CORE, a high-level system entity. Be concise, technical but helpful. Suggest specific actions if performance is degraded.";

            var requestBody = new
            {
                model = status.activeModel,
                prompt = fullPrompt,
                stream = false
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var response = await _httpClient.PostAsync("/api/generate", new StringContent(json, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return "NEXUS AI encountered a processing error.";

                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OllamaGenerateResponse>(resultJson);
                return result?.Response ?? "AI response empty.";
            }
            catch (Exception ex)
            {
                return $"Neural Link Failure: {ex.Message}";
            }
        }

        private string BuildSystemContext(SystemMetrics metrics, IEnumerable<DiskMetrics> disks, StorageAnalysisResult? lastScan)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Current System Health:");
            sb.AppendLine($"- CPU Load: {metrics.CpuUsage}%");
            sb.AppendLine($"- RAM Utilization: {metrics.RamUsedGb}/{metrics.RamTotalGb} GB");
            sb.AppendLine($"- Uptime: {metrics.Uptime:hh\\:mm\\:ss}");
            
            sb.AppendLine("\nStorage Status:");
            foreach (var d in disks)
            {
                sb.AppendLine($"- {d.DriveName}: {d.UsagePercentage:F1}% utilized");
            }

            if (lastScan != null && !lastScan.IsScanning)
            {
                sb.AppendLine("\nLast Storage Scan Results:");
                sb.AppendLine($"- Total Files: {lastScan.TotalFiles}");
                sb.AppendLine($"- Scanned Drives: {lastScan.ScannedDrives}");
                if (lastScan.TopFolders.Any())
                    sb.AppendLine($"- Largest Folder: {lastScan.TopFolders[0].Path} ({lastScan.TopFolders[0].FormattedSize})");
            }

            sb.AppendLine("\nActive Core Processes:");
            foreach (var p in metrics.Processes.Take(3))
            {
                sb.AppendLine($"- {p.Name}: {p.FormattedRam}");
            }

            return sb.ToString();
        }
    }

    public class OllamaTagsResponse { public List<OllamaModel>? Models { get; set; } }
    public class OllamaModel { [JsonProperty("name")] public string? Name { get; set; } }
    public class OllamaGenerateResponse { [JsonProperty("response")] public string? Response { get; set; } }
}
