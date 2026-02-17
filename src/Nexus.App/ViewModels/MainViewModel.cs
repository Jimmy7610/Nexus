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

        [ObservableProperty] private SystemMetrics _currentMetrics = new();
        [ObservableProperty] private NetworkMetrics _networkMetrics = new();
        [ObservableProperty] private StorageAnalysisResult _storageResult = new();
        [ObservableProperty] private string _chatInput = "";
        [ObservableProperty] private string _aiStatus = "OLLAMA: INITIALIZING";
        [ObservableProperty] private string _activeAiModel = "NONE";
        [ObservableProperty] private object _currentView = "OVERVIEW";

        public ObservableCollection<DiskMetrics> Disks { get; } = new();
        public ObservableCollection<ChatMessage> ChatHistory { get; } = new();

        public IAsyncRelayCommand<string> StartScanCommand { get; }
        public ICommand CancelScanCommand { get; }
        public IAsyncRelayCommand SendChatCommand { get; }
        public IAsyncRelayCommand<string> QuickPromptCommand { get; }
        public ICommand OpenWindowsSettingsCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            _metricsService = new MetricsService();
            _storageScanner = new StorageScanner();
            _ollamaClient = new OllamaClient();

            _storageScanner.ProgressUpdated += (res) => {
                App.Current.Dispatcher.Invoke(() => StorageResult = res);
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += async (s, e) => await UpdateTelemetryAsync();
            _timer.Start();

            StartScanCommand = new AsyncRelayCommand<string>(async (drive) => {
                await _storageScanner.ScanDriveAsync(drive ?? "C:\\");
            });

            CancelScanCommand = new RelayCommand(() => _storageScanner.CancelScan());
            SendChatCommand = new AsyncRelayCommand(SendChatAsync);
            QuickPromptCommand = new AsyncRelayCommand<string>(async (p) => {
                ChatInput = p ?? "";
                await SendChatAsync();
            });

            OpenWindowsSettingsCommand = new RelayCommand(OpenWindowsSettings);
            RestartCommand = new RelayCommand(ExecuteRestart);
            NavigateCommand = new RelayCommand<string>((view) => CurrentView = view ?? "OVERVIEW");

            ChatHistory.Add(new ChatMessage { Role = "CORE", Content = "NEXUS AI online. System monitors active." });
            Task.Run(InitializeAiStatusAsync);
        }

        private async Task InitializeAiStatusAsync()
        {
            var status = await _ollamaClient.GetStatusAsync();
            AiStatus = status.isOnline ? "OLLAMA: READY" : "OLLAMA: OFFLINE";
            ActiveAiModel = status.activeModel;
        }

        private async Task UpdateTelemetryAsync()
        {
            try
            {
                CurrentMetrics = await _metricsService.GetSystemMetricsAsync();
                NetworkMetrics = await _metricsService.GetNetworkMetricsAsync();
                var disks = await _metricsService.GetDiskMetricsAsync();
                
                App.Current.Dispatcher.Invoke(() => {
                    var currentDisks = disks.ToList();
                    if (Disks.Count != currentDisks.Count)
                    {
                        Disks.Clear();
                        foreach (var d in currentDisks) Disks.Add(d);
                    }
                    else
                    {
                        for (int i = 0; i < currentDisks.Count; i++)
                        {
                            Disks[i].TotalSpaceBytes = currentDisks[i].TotalSpaceBytes;
                            Disks[i].FreeSpaceBytes = currentDisks[i].FreeSpaceBytes;
                        }
                    }
                });
            }
            catch { }
        }

        private async Task SendChatAsync()
        {
            if (string.IsNullOrWhiteSpace(ChatInput)) return;

            var userMsg = ChatInput;
            ChatInput = string.Empty;
            ChatHistory.Add(new ChatMessage { Role = "USER", Content = userMsg });

            AiStatus = "OLLAMA: THINKING";
            var response = await _ollamaClient.GetRecommendationAsync(userMsg, CurrentMetrics, Disks, StorageResult);
            ChatHistory.Add(new ChatMessage { Role = "NEXUS", Content = response });
            
            var status = await _ollamaClient.GetStatusAsync();
            AiStatus = status.isOnline ? "OLLAMA: READY" : "OLLAMA: OFFLINE";
            ActiveAiModel = status.activeModel;
        }

        private void ExecuteRestart()
        {
            var result = System.Windows.MessageBox.Show("Confirm System Decoupling (Restart NEXUS)?", "RESTART_PROMPT", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    var currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (currentExe != null)
                    {
                        System.Diagnostics.Process.Start(currentExe);
                        System.Windows.Application.Current.Shutdown();
                    }
                }
                catch
                {
                    System.Windows.MessageBox.Show("Restart Protocol Failed.", "ERROR", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
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
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string Timestamp { get; } = DateTime.Now.ToString("HH:mm");
    }
}
