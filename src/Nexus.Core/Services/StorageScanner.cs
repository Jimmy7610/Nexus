using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Nexus.Core.Models;

namespace Nexus.Core.Services
{
    public class StorageScanner : IStorageScanner
    {
        private CancellationTokenSource? _cts;
        public event Action<StorageAnalysisResult>? ProgressUpdated;

        public async Task<StorageAnalysisResult> ScanDriveAsync(string driveLetter)
        {
            _cts = new CancellationTokenSource();
            var startTime = DateTime.Now;
            var sw = Stopwatch.StartNew();
            var result = new StorageAnalysisResult 
            { 
                IsScanning = true,
                ScannedDrives = driveLetter
            };
            
            return await Task.Run(() =>
            {
                var folderWeights = new Dictionary<string, long>();
                var allFiles = new List<NexusFileEntry>();
                var extDist = new Dictionary<string, (int count, long size)>();
                long totalFiles = 0;
                int errorCount = 0;

                Queue<string> queue = new Queue<string>();
                queue.Enqueue(driveLetter);

                while (queue.Count > 0 && !_cts.IsCancellationRequested)
                {
                    string currentDir = queue.Dequeue();
                    try
                    {
                        var di = new DirectoryInfo(currentDir);
                        
                        foreach (var file in di.GetFiles())
                        {
                            if (_cts.IsCancellationRequested) break;
                            
                            totalFiles++;
                            long size = file.Length;
                            string ext = file.Extension.ToLower();
                            if (string.IsNullOrEmpty(ext)) ext = "none";

                            // File distribution
                            if (!extDist.ContainsKey(ext)) extDist[ext] = (0, 0);
                            var current = extDist[ext];
                            extDist[ext] = (current.count + 1, current.size + size);

                            allFiles.Add(new NexusFileEntry { Name = file.Name, Path = file.FullName, SizeBytes = size });
                            
                            // Attribute size to parent folders
                            string parent = currentDir;
                            while (parent.Length >= driveLetter.Length)
                            {
                                if (!folderWeights.ContainsKey(parent)) folderWeights[parent] = 0;
                                folderWeights[parent] += size;
                                parent = Path.GetDirectoryName(parent);
                                if (string.IsNullOrEmpty(parent)) break;
                            }
                        }

                        foreach (var subDir in di.GetDirectories())
                        {
                            if ((subDir.Attributes & FileAttributes.ReparsePoint) == 0)
                            {
                                queue.Enqueue(subDir.FullName);
                            }
                        }

                        // Periodic progress reporting
                        if (totalFiles % 1000 == 0)
                        {
                            result.TotalFiles = totalFiles;
                            result.ScanDuration = sw.Elapsed.ToString(@"mm\:ss");
                            ProgressUpdated?.Invoke(result);
                        }
                    }
                    catch (UnauthorizedAccessException) { errorCount++; }
                    catch (Exception) { errorCount++; }
                }

                sw.Stop();
                result.IsScanning = false;
                result.TotalFiles = totalFiles;
                result.ErrorCount = errorCount;
                result.ScanDuration = sw.Elapsed.ToString(@"mm\:ss");
                
                result.TopFiles = allFiles.OrderByDescending(f => f.SizeBytes).Take(20).ToList();
                result.TopFolders = folderWeights
                    .OrderByDescending(f => f.Value)
                    .Take(10)
                    .Select(f => new NexusFileEntry { Name = Path.GetFileName(f.Key) ?? f.Key, Path = f.Key, SizeBytes = f.Value })
                    .ToList();

                result.FileTypeDistribution = extDist
                    .Select(kvp => new FileCategory { Extension = kvp.Key, Count = kvp.Value.count, SizeBytes = kvp.Value.size })
                    .OrderByDescending(c => c.SizeBytes)
                    .Take(10)
                    .ToList();

                ProgressUpdated?.Invoke(result);
                return result;
            }, _cts.Token);
        }

        public void CancelScan()
        {
            _cts?.Cancel();
        }
    }
}
