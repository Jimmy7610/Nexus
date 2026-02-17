using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nexus.Core.Models;
using Nexus.Core.Ollama;
using Nexus.Core.Services;

namespace Nexus.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IMetricsService _metricsService;
        private readonly IStorageScanner _storageScanner;
        private readonly IOllamaClient _ollamaClient;
        private readonly DispatcherTimer _timer;

        [ObservableProperty] private SystemMetrics _currentMetrics;
        [ObservableProperty] private StorageAnalysisResult _storageResult;
        [ObservableProperty] private string _chatInput;
        [ObservableProperty] private string _aiStatus = "OLLAMA: READY";
        [ObservableProperty] private object _currentView;

        public ObservableCollection<DiskMetrics> Disks { get; } = new();
        public ObservableCollection<ChatMessage> ChatHistory { get; } = new();

        public ICommand StartScanCommand { get; }
        public IAsyncRelayCommand SendChatCommand { get; }
        public ICommand OpenWindowsSettingsCommand { get; }

        public MainViewModel()
        {
            _metricsService = new MetricsService();
            _storageScanner = new StorageScanner();
            _ollamaClient = new OllamaClient();

            _storageScanner.ProgressUpdated += (res) => StorageResult = res;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (s, e) => await UpdateMetricsAsync();
            _timer.Start();

            StartScanCommand = new AsyncRelayCommand(async () => {
                AiStatus = "SCANNING DISK...";
                StorageResult = await _storageScanner.ScanDriveAsync("C:\\");
                AiStatus = "SCAN COMPLETE";
            });

            SendChatCommand = new AsyncRelayCommand(SendChatAsync);
            OpenWindowsSettingsCommand = new RelayCommand(OpenWindowsSettings);

            ChatHistory.Add(new ChatMessage { Role = "System", Content = "NEXUS AI online. Assessing system health..." });
            Task.Run(UpdateMetricsAsync);
        }

        private async Task UpdateMetricsAsync()
        {
            try
            {
                CurrentMetrics = await _metricsService.GetSystemMetricsAsync();
                var disks = await _metricsService.GetDiskMetricsAsync();
                
                App.Current.Dispatcher.Invoke(() => {
                    Disks.Clear();
                    foreach (var d in disks) Disks.Add(d);
                });
            }
            catch { }
        }

        private async Task SendChatAsync()
        {
            if (string.IsNullOrWhiteSpace(ChatInput)) return;

            var userMsg = ChatInput;
            ChatInput = string.Empty;
            ChatHistory.Add(new ChatMessage { Role = "User", Content = userMsg });

            AiStatus = "THINKING...";
            var response = await _ollamaClient.GetRecommendationAsync(userMsg, CurrentMetrics, Disks);
            ChatHistory.Add(new ChatMessage { Role = "Nexus", Content = response });
            AiStatus = "OLLAMA: IDLE";
        }

        private void OpenWindowsSettings()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:personalization-background") { UseShellExecute = true });
            }
            catch { }
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; } = DateTime.Now.ToString("HH:mm");
    }
}
