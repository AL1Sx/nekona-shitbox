using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace LinuxSysInfo
{
    // ========================================
    // Configuration
    // ========================================
    public static class AppSettings
    {
        public const string SearchUrlTemplate = "https://www.bing.com/search?q={0}";
        public const int MonitorIntervalMs = 1000;
        
        // Colors
        public const ConsoleColor AmdColor = ConsoleColor.Red;
        public const ConsoleColor IntelColor = ConsoleColor.Blue;
        public const ConsoleColor DefaultColor = ConsoleColor.White;
        public const ConsoleColor LinkColor = ConsoleColor.Cyan;
        public const ConsoleColor NvidiaColor = ConsoleColor.Green;
        public const ConsoleColor AmdGpuColor = ConsoleColor.Red;
        public const ConsoleColor IntelGpuColor = ConsoleColor.Blue;
        public const ConsoleColor WesternDigitalColor = ConsoleColor.Blue;
        public const ConsoleColor SeagateColor = ConsoleColor.Green;
        public const ConsoleColor SamsungColor = ConsoleColor.DarkBlue;
        public const ConsoleColor SuccessColor = ConsoleColor.Green;
        public const ConsoleColor TipColor = ConsoleColor.Yellow;
    }

    // ========================================
    // Data Structure
    // ========================================
    public struct SystemData
    {
        public float CpuUsage;
        public double RamUsagePercent;
        public TimeSpan Elapsed;
    }

    // ========================================
    // Hardware Info Collector (Linux Version)
    // ========================================
    public static class HardwareInfoCollector
    {
        public static void DisplayCpuInfo()
        {
            try
            {
                string cpuName = GetCpuName();
                if (!string.IsNullOrEmpty(cpuName))
                {
                    ConsoleColor color = GetCpuColor(cpuName);
                    Console.ForegroundColor = color;
                    Console.Write($"CPU: {cpuName}");
                    Console.Write(" | ");
                    CreateSearchableHyperlink("Search", cpuName, AppSettings.LinkColor);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CPU info: {ex.Message}");
            }
        }

        public static void DisplayGpuInfo()
        {
            try
            {
                var gpuList = GetGpuInfo();
                int gpuIndex = 1;
                foreach (var gpu in gpuList)
                {
                    ConsoleColor color = GetGpuColor(gpu);
                    Console.ForegroundColor = color;
                    Console.Write($"GPU {gpuIndex}: {gpu}");
                    Console.Write(" | ");
                    CreateSearchableHyperlink("Search", gpu, AppSettings.LinkColor);
                    Console.ResetColor();
                    gpuIndex++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading GPU info: {ex.Message}");
            }
        }

        public static void DisplayRamInfo()
        {
            try
            {
                var ramModules = GetRamInfo();
                int ramIndex = 1;
                foreach (var ram in ramModules)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"RAM {ramIndex++}: {ram}");
                    Console.Write(" | ");
                    CreateSearchableHyperlink("Search", ram, AppSettings.LinkColor);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading RAM info: {ex.Message}");
            }
        }

        public static void DisplayMotherboardInfo()
        {
            try
            {
                string motherboard = GetMotherboardInfo();
                if (!string.IsNullOrEmpty(motherboard))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write($"Motherboard: {motherboard}");
                    Console.Write(" | ");
                    CreateSearchableHyperlink("Search", motherboard, AppSettings.LinkColor);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading motherboard info: {ex.Message}");
            }
        }

        public static void DisplayDiskInfo()
        {
            try
            {
                var disks = GetDiskInfo();
                foreach (var disk in disks)
                {
                    ConsoleColor color = GetDiskBrandColor(disk.Model);
                    Console.ForegroundColor = color;
                    Console.Write($"Disk {disk.Device}: {disk.SizeGB}GB ({disk.FreeGB}GB Free) | Model: {disk.Model}");
                    Console.Write(" | ");
                    CreateSearchableHyperlink("Search", disk.Model, AppSettings.LinkColor);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading disk info: {ex.Message}");
            }
        }

        // ========================================
        // Linux-specific implementations
        // ========================================
        
        private static string GetCpuName()
        {
            string cpuInfo = File.ReadAllText("/proc/cpuinfo");
            Match match = Regex.Match(cpuInfo, @"model name\s*:\s*(.+)");
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown CPU";
        }

        private static List<string> GetGpuInfo()
        {
            var gpus = new List<string>();
            
            // Use shell to run pipeline command
            string output = RunCommandWithShell("lspci | grep -E 'VGA|3D|Display'");
            
            foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                // Extract after the first colon
                int colonIndex = line.IndexOf(':');
                if (colonIndex >= 0 && colonIndex + 1 < line.Length)
                {
                    string gpuName = line.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(gpuName))
                    {
                        gpus.Add(gpuName);
                    }
                }
            }
            
            return gpus.Count > 0 ? gpus : new List<string> { "Unknown GPU" };
        }

        private static List<string> GetRamInfo()
        {
            var ramModules = new List<string>();
            
            // Try to get detailed RAM info via dmidecode
            string output = RunCommandWithShell("dmidecode -t memory 2>/dev/null | grep -E 'Size:|Manufacturer:|Part Number:|Speed:'");
            
            var currentModule = new Dictionary<string, string>();
            foreach (string line in output.Split('\n'))
            {
                if (line.Contains("Size:"))
                {
                    string size = ExtractValue(line);
                    if (!size.Contains("No Module") && !size.Contains("Not Specified") && size != "Unknown")
                    {
                        currentModule["Size"] = size;
                    }
                }
                else if (line.Contains("Manufacturer:"))
                {
                    string manufacturer = ExtractValue(line);
                    if (manufacturer != "Not Specified" && manufacturer != "Unknown")
                    {
                        currentModule["Manufacturer"] = manufacturer;
                    }
                }
                else if (line.Contains("Part Number:"))
                {
                    string partNumber = ExtractValue(line);
                    if (partNumber != "Not Specified" && partNumber != "Unknown")
                    {
                        currentModule["PartNumber"] = partNumber;
                    }
                }
                else if (line.Contains("Speed:"))
                {
                    string speed = ExtractValue(line);
                    if (speed != "Unknown" && !speed.Contains("Not Specified"))
                    {
                        currentModule["Speed"] = speed;
                    }
                    
                    // Complete module if we have size
                    if (currentModule.ContainsKey("Size"))
                    {
                        string manufacturer = currentModule.GetValueOrDefault("Manufacturer", "Unknown");
                        string partNumber = currentModule.GetValueOrDefault("PartNumber", "");
                        string speedStr = currentModule.GetValueOrDefault("Speed", "Unknown");
                        string size = currentModule["Size"];
                        
                        string ramInfo = $"{manufacturer} {partNumber} {speedStr}MHz {size}";
                        ramModules.Add(ramInfo.Trim());
                        currentModule.Clear();
                    }
                }
            }
            
            // Fallback: get total RAM if dmidecode fails or no modules found
            if (ramModules.Count == 0)
            {
                double totalGB = GetTotalRamGB();
                ramModules.Add($"Total: {totalGB:F1} GB");
            }
            
            return ramModules;
        }

        private static string GetMotherboardInfo()
        {
            // Try reading from sysfs first
            string vendor = ReadFirstLine("/sys/devices/virtual/dmi/id/board_vendor");
            string name = ReadFirstLine("/sys/devices/virtual/dmi/id/board_name");
            
            if (!string.IsNullOrEmpty(vendor) && !string.IsNullOrEmpty(name))
            {
                return $"{vendor} {name}".Trim();
            }
            
            // Fallback to dmidecode
            string output = RunCommandWithShell("dmidecode -t baseboard 2>/dev/null | grep -E 'Manufacturer:|Product Name:'");
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                string manufacturer = ExtractValue(lines[0]);
                string product = ExtractValue(lines[1]);
                if (!string.IsNullOrEmpty(manufacturer) && !string.IsNullOrEmpty(product))
                {
                    return $"{manufacturer} {product}".Trim();
                }
            }
            
            return "Unknown Motherboard";
        }

        private static List<(string Device, string Model, long SizeGB, long FreeGB)> GetDiskInfo()
        {
            var disks = new List<(string, string, long, long)>();
            
            // Get disk info from lsblk
            string output = RunCommandWithShell("lsblk -o NAME,SIZE,TYPE,MODEL -n -b 2>/dev/null");
            
            foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[2] == "disk")
                {
                    string device = parts[0];
                    if (long.TryParse(parts[1], out long sizeBytes))
                    {
                        long sizeGB = sizeBytes / (1024 * 1024 * 1024);
                        string model = parts.Length >= 4 ? string.Join(" ", parts.Skip(3)) : "Unknown Model";
                        
                        // Get free space for the device's mounted partitions
                        long freeGB = GetFreeSpaceForDevice(device);
                        
                        disks.Add((device, model, sizeGB, freeGB));
                    }
                }
            }
            
            return disks;
        }

        private static long GetFreeSpaceForDevice(string device)
        {
            try
            {
                // Get all mount points for this device
                string output = RunCommandWithShell($"df -B1 | grep '/dev/{device}' | awk '{{print $4}}'");
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                long totalFree = 0;
                foreach (string line in lines)
                {
                    if (long.TryParse(line, out long freeBytes))
                    {
                        totalFree += freeBytes;
                    }
                }
                
                return totalFree / (1024 * 1024 * 1024);
            }
            catch
            {
                return 0;
            }
        }

        private static double GetTotalRamGB()
        {
            string memInfo = File.ReadAllText("/proc/meminfo");
            Match match = Regex.Match(memInfo, @"MemTotal:\s+(\d+)\s+kB");
            if (match.Success && long.TryParse(match.Groups[1].Value, out long totalKb))
            {
                return totalKb / (1024.0 * 1024.0);
            }
            return 0;
        }

        // ========================================
        // Helper methods
        // ========================================
        
        private static string RunCommandWithShell(string command)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private static string ReadFirstLine(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path).Trim();
                }
            }
            catch { }
            return null;
        }

        private static string ExtractValue(string line)
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex >= 0 && colonIndex + 1 < line.Length)
            {
                return line.Substring(colonIndex + 1).Trim();
            }
            return line;
        }

        private static ConsoleColor GetCpuColor(string cpuName)
        {
            if (cpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
                return AppSettings.AmdColor;
            if (cpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                return AppSettings.IntelColor;
            return AppSettings.DefaultColor;
        }

        private static ConsoleColor GetGpuColor(string gpuName)
        {
            if (gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                return AppSettings.NvidiaColor;
            if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
                return AppSettings.AmdGpuColor;
            if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                return AppSettings.IntelGpuColor;
            return AppSettings.DefaultColor;
        }

        private static ConsoleColor GetDiskBrandColor(string diskModel)
        {
            if (string.IsNullOrEmpty(diskModel))
                return ConsoleColor.Green;
            
            string modelLower = diskModel.ToLower();
            
            if (modelLower.Contains("western digital") || modelLower.Contains("wd"))
                return AppSettings.WesternDigitalColor;
            if (modelLower.Contains("seagate"))
                return AppSettings.SeagateColor;
            if (modelLower.Contains("samsung"))
                return AppSettings.SamsungColor;
            
            return ConsoleColor.Green;
        }

        private static void CreateSearchableHyperlink(string displayText, string searchQuery, ConsoleColor color)
        {
            string searchUrl = string.Format(AppSettings.SearchUrlTemplate, Uri.EscapeDataString(searchQuery));
            string hyperlink = $"\u001b]8;id={searchUrl};{searchUrl}\u001b\\{displayText}\u001b]8;;\u001b\\";
            Console.ForegroundColor = color;
            Console.WriteLine(hyperlink);
            Console.ResetColor();
        }
    }

    // ========================================
    // System Monitor (Linux Version)
    // ========================================
    public class SystemMonitor : IDisposable
    {
        private long _lastIdle, _lastTotal;
        private DateTime _lastCheckTime;
        private bool _disposed = false;
        private readonly double _totalRamGB;
        private Dictionary<int, TimeSpan> _cpuTimes = new Dictionary<int, TimeSpan>();

        public SystemMonitor()
        {
            _totalRamGB = GetTotalRamGB();
            ReadCpuStat(out _lastIdle, out _lastTotal);
            _lastCheckTime = DateTime.Now;
        }

        public void Start()
        {
            Console.WriteLine("=== System Monitor ===");
            
            while (true)
            {
                Thread.Sleep(AppSettings.MonitorIntervalMs);
                
                SystemData data = CollectSystemData();
                var (topCpuProcess, maxCpuUsage) = FindTopCpuProcess();
                Process? topMemProcess = FindTopMemoryProcess();
                
                DisplayResult(data, topCpuProcess, maxCpuUsage, topMemProcess);
            }
        }

        private SystemData CollectSystemData()
        {
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - _lastCheckTime;
            if (elapsed.TotalMilliseconds == 0) elapsed = TimeSpan.FromMilliseconds(1);
            
            float cpuUsage = GetCpuUsage();
            double ramUsagePercent = GetRamUsagePercent();
            
            _lastCheckTime = now;
            
            return new SystemData
            {
                CpuUsage = cpuUsage,
                RamUsagePercent = ramUsagePercent,
                Elapsed = elapsed
            };
        }

        private float GetCpuUsage()
        {
            ReadCpuStat(out long idle, out long total);
            long diffIdle = idle - _lastIdle;
            long diffTotal = total - _lastTotal;
            _lastIdle = idle;
            _lastTotal = total;
            
            if (diffTotal == 0) return 0;
            return (1.0f - (float)diffIdle / diffTotal) * 100.0f;
        }

        private void ReadCpuStat(out long idle, out long total)
        {
            string line = File.ReadLines("/proc/stat").First();
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // parts[0] is "cpu"
            long user = long.Parse(parts[1]);
            long nice = long.Parse(parts[2]);
            long system = long.Parse(parts[3]);
            long idleStat = long.Parse(parts[4]);
            long iowait = long.Parse(parts[5]);
            long irq = long.Parse(parts[6]);
            long softirq = long.Parse(parts[7]);
            
            idle = idleStat + iowait;
            total = user + nice + system + idleStat + iowait + irq + softirq;
        }

        private double GetRamUsagePercent()
        {
            string memInfo = File.ReadAllText("/proc/meminfo");
            Match memTotalMatch = Regex.Match(memInfo, @"MemTotal:\s+(\d+)\s+kB");
            Match memAvailMatch = Regex.Match(memInfo, @"MemAvailable:\s+(\d+)\s+kB");
            
            if (memTotalMatch.Success && memAvailMatch.Success)
            {
                long totalKb = long.Parse(memTotalMatch.Groups[1].Value);
                long availKb = long.Parse(memAvailMatch.Groups[1].Value);
                long usedKb = totalKb - availKb;
                return (double)usedKb / totalKb * 100.0;
            }
            
            return 0;
        }

        private (Process? process, double usage) FindTopCpuProcess()
        {
            Process? topProcess = null;
            double maxCpuUsage = 0.0;
            
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    TimeSpan currentTime = p.TotalProcessorTime;
                    TimeSpan lastTime = GetLastCpuTime(p.Id);
                    SetLastCpuTime(p.Id, currentTime);
                    
                    if (lastTime.Ticks > 0)
                    {
                        double processCpuUsed = (currentTime - lastTime).TotalMilliseconds / 
                            AppSettings.MonitorIntervalMs * 100.0 / Environment.ProcessorCount;
                        
                        if (processCpuUsed > maxCpuUsage)
                        {
                            maxCpuUsage = processCpuUsed;
                            topProcess = p;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            return (topProcess, maxCpuUsage);
        }

        private Process? FindTopMemoryProcess()
        {
            Process? topProcess = null;
            long maxMemory = 0;
            
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.WorkingSet64 > maxMemory)
                    {
                        maxMemory = p.WorkingSet64;
                        topProcess = p;
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            return topProcess;
        }
        
        private TimeSpan GetLastCpuTime(int pid)
        {
            return _cpuTimes.GetValueOrDefault(pid);
        }
        
        private void SetLastCpuTime(int pid, TimeSpan time)
        {
            _cpuTimes[pid] = time;
        }

        private double GetTotalRamGB()
        {
            string memInfo = File.ReadAllText("/proc/meminfo");
            Match match = Regex.Match(memInfo, @"MemTotal:\s+(\d+)\s+kB");
            if (match.Success && long.TryParse(match.Groups[1].Value, out long totalKb))
            {
                return totalKb / (1024.0 * 1024.0);
            }
            return 0;
        }

        private void DisplayResult(SystemData data, Process? topCpuProcess, double maxCpuUsage, Process? topMemProcess)
        {
            string topCpuInfo = "";
            if (topCpuProcess != null)
            {
                topCpuInfo = $" | Top CPU: {topCpuProcess.ProcessName} ({maxCpuUsage:0.00}%)";
            }
            
            string topMemInfo = "";
            if (topMemProcess != null)
            {
                double topMemMB = topMemProcess.WorkingSet64 / (1024.0 * 1024.0);
                topMemInfo = $" | Top RAM: {topMemProcess.ProcessName} ({topMemMB:0.00} MB)";
            }
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] CPU: {data.CpuUsage:0.0}% | RAM: {data.RamUsagePercent:0.00}%{topCpuInfo}{topMemInfo}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Cleanup managed resources
                }
                _disposed = true;
            }
        }
    }

    // ========================================
    // Main Entry Point
    // ========================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Let me looking Ver081 Linux===");
            Console.WriteLine("By: Nekona Alice");
            Console.WriteLine("Development & design on [Manjaro] and [Zsh]; others untested.");
            Console.WriteLine();
            
            HardwareInfoCollector.DisplayCpuInfo();
            HardwareInfoCollector.DisplayGpuInfo();
            HardwareInfoCollector.DisplayRamInfo();
            HardwareInfoCollector.DisplayMotherboardInfo();
            HardwareInfoCollector.DisplayDiskInfo();
            
            Console.WriteLine();
            Console.WriteLine("Press Enter to start system monitoring...");
            Console.ReadLine();
            
            try
            {
                using var monitor = new SystemMonitor();
                monitor.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}