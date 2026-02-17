using System;
using System.Collections.Generic;

namespace Nexus.Core.Models
{
    public class SystemMetrics
    {
        public double CpuUsage { get; set; }
        public double RamUsedGb { get; set; }
        public double RamTotalGb { get; set; }
        public double RamUsagePercentage => (RamUsedGb / RamTotalGb) * 100;
        public TimeSpan Uptime { get; set; }
        public double GpuUsage { get; set; } // Placeholder
        public List<ActiveProcess> Processes { get; set; } = new();
        public string ActiveModel { get; set; } = "OLLAMA: READY";
    }

    public class ActiveProcess
    {
        public string Name { get; set; }
        public double CpuUsage { get; set; }
        public long RamUsedBytes { get; set; }
        public string FormattedRam => FormatSize(RamUsedBytes);

        private static string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }

    public class DiskMetrics
    {
        public string DriveName { get; set; }
        public long TotalSpaceBytes { get; set; }
        public long FreeSpaceBytes { get; set; }
        public double UsagePercentage => 100 - ((double)FreeSpaceBytes / TotalSpaceBytes * 100);
    }

    public class NetworkMetrics
    {
        public double DownloadSpeedMbps { get; set; }
        public double UploadSpeedMbps { get; set; }
    }

    public class StorageAnalysisResult
    {
        public List<NexusFileEntry> TopFolders { get; set; } = new();
        public List<NexusFileEntry> TopFiles { get; set; } = new();
        public List<FileCategory> FileTypeDistribution { get; set; } = new();
        public long TotalFiles { get; set; }
        public int ProgressPercentage { get; set; }
        public bool IsScanning { get; set; }
        public string ScanDuration { get; set; } = "00:00";
        public string ScannedDrives { get; set; } = "";
        public int ErrorCount { get; set; }
    }

    public class FileCategory
    {
        public string Extension { get; set; } = "";
        public int Count { get; set; }
        public long SizeBytes { get; set; }
        public string FormattedSize => NexusFileEntry.FormatSize(SizeBytes);
    }

    public class NexusFileEntry
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        public string FormattedSize => FormatSize(SizeBytes);

        public static string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
