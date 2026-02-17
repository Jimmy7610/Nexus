using System;
using System.Threading.Tasks;
using Nexus.Core.Models;

namespace Nexus.Core.Services
{
    public interface IMetricsService
    {
        Task<SystemMetrics> GetSystemMetricsAsync();
        Task<IEnumerable<DiskMetrics>> GetDiskMetricsAsync();
        Task<NetworkMetrics> GetNetworkMetricsAsync();
    }

    public interface IStorageScanner
    {
        event Action<StorageAnalysisResult> ProgressUpdated;
        Task<StorageAnalysisResult> ScanDriveAsync(string driveLetter);
        void CancelScan();
    }
}
