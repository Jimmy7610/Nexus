using Xunit;
using Nexus.Core.Models;

namespace Nexus.Tests
{
    public class ModelTests
    {
        [Fact]
        public void FileEntry_FormattedSize_ReturnsCorrectSuffix()
        {
            var entry = new FileEntry { SizeBytes = 1024 * 1024 * 5 }; // 5 MB
            Assert.Contains("5.0 MB", entry.FormattedSize);

            entry.SizeBytes = 2048; // 2 KB
            Assert.Contains("2.0 KB", entry.FormattedSize);
        }

        [Fact]
        public void SystemMetrics_RamPercentage_CalculatesCorrectly()
        {
            var metrics = new SystemMetrics { RamUsedGb = 4, RamTotalGb = 16 };
            Assert.Equal(25.0, metrics.RamUsagePercentage);
        }
    }
}
