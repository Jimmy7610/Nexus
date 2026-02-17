using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Nexus.Core.Models;

namespace Nexus.Core.Services
{
    public class MetricsService : IMetricsService
    {
        private readonly PerformanceCounter _cpuCounter;

        public MetricsService()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // First call always returns 0
        }

        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            var metrics = new SystemMetrics();
            
            // CPU
            metrics.CpuUsage = Math.Round(_cpuCounter.NextValue(), 1);

            // RAM & Uptime via WMI
            using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory, LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var total = Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024 / 1024; // GB
                    var free = Convert.ToDouble(obj["FreePhysicalMemory"]) / 1024 / 1024; // GB
                    metrics.RamTotalGb = Math.Round(total, 1);
                    metrics.RamUsedGb = Math.Round(total - free, 1);

                    var lastBoot = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    metrics.Uptime = DateTime.Now - lastBoot;
                }
            }

            // Top Processes
            metrics.Processes = Process.GetProcesses()
                .OrderByDescending(p => p.PrivateMemorySize64)
                .Take(5)
                .Select(p => new ActiveProcess
                {
                    Name = p.ProcessName,
                    RamUsedBytes = p.PrivateMemorySize64,
                    CpuUsage = 0 // Complex to get per-process CPU instantly without polling, but placeholder
                })
                .ToList();

            return metrics;
        }

        public async Task<IEnumerable<DiskMetrics>> GetDiskMetricsAsync()
        {
            var disks = new List<DiskMetrics>();
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                disks.Add(new DiskMetrics
                {
                    DriveName = drive.Name,
                    TotalSpaceBytes = drive.TotalSize,
                    FreeSpaceBytes = drive.AvailableFreeSpace
                });
            }
            return disks;
        }

        public async Task<NetworkMetrics> GetNetworkMetricsAsync()
        {
            // Placeholder: Implementing real-time network speed requires polling interface statistics over time
            return new NetworkMetrics { DownloadSpeedMbps = 0, UploadSpeedMbps = 0 };
        }
    }
}
