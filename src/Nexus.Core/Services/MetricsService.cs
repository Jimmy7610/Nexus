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
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); 
            }
            catch { /* Fallback or log if counters are disabled */ }
        }

        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            var metrics = new SystemMetrics();
            
            try
            {
                if (_cpuCounter != null)
                    metrics.CpuUsage = Math.Round(_cpuCounter.NextValue(), 1);
            }
            catch { }

            // RAM & Uptime via WMI
            try
            {
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
            }
            catch { }

            // Top Processes
            try
            {
                metrics.Processes = Process.GetProcesses()
                    .OrderByDescending(p => p.PrivateMemorySize64)
                    .Take(5)
                    .Select(p => new ActiveProcess
                    {
                        Name = p.ProcessName,
                        RamUsedBytes = p.PrivateMemorySize64,
                        CpuUsage = 0 
                    })
                    .ToList();
            }
            catch { }

            return metrics;
        }

        public async Task<IEnumerable<DiskMetrics>> GetDiskMetricsAsync()
        {
            var disks = new List<DiskMetrics>();
            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    disks.Add(new DiskMetrics
                    {
                        DriveName = drive.Name,
                        TotalSpaceBytes = drive.TotalSize,
                        FreeSpaceBytes = drive.AvailableFreeSpace
                    });
                }
            }
            catch { }
            return disks;
        }

        public async Task<NetworkMetrics> GetNetworkMetricsAsync()
        {
            return new NetworkMetrics { DownloadSpeedMbps = 0, UploadSpeedMbps = 0 };
        }
    }
}
