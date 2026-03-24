using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;

[assembly: SupportedOSPlatform("windows")]

namespace CpuInfoWin32
{
    // ========================================
    // 配置区
    // ========================================
    // Windows Terminal检测(未修复)
    // public static class TerminalHelper
    // {
    //     // 核心：通过WMI获取指定进程的父进程ID
    //     private static int? GetParentProcessId(int processId)
    //     {
    //         try
    //         {
    //             using (var searcher = new ManagementObjectSearcher(
    //                 $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}"))
    //             {
    //                 foreach (var obj in searcher.Get())
    //                 {
    //                     return Convert.ToInt32(obj["ParentProcessId"]);
    //                 }
    //             }
    //         }
    //         catch
    //         {
    //             // 无权限/进程不存在时返回null，不影响主逻辑
    //         }
    //         return null;
    //     }

    //     // 对外公开的检测方法，遍历父/祖先进程找WindowsTerminal.exe
    //     public static bool IsRunningInWindowsTerminal()
    //     {
    //         try
    //         {
    //             int currentPid = Process.GetCurrentProcess().Id;
    //             // 遍历父进程，直到无父进程/找到WT/遍历次数过多（防止死循环）
    //             for (int i = 0; i < 10; i++)
    //             {
    //                 var parentPid = GetParentProcessId(currentPid);
    //                 if (!parentPid.HasValue || parentPid.Value == 0 || parentPid.Value == 1)
    //                     break; // 无父进程/到系统核心进程，终止遍历

    //                 var parentProcess = Process.GetProcessById(parentPid.Value);
    //                 // 检测是否是Windows Terminal核心进程
    //                 if (parentProcess.ProcessName.Equals("WindowsTerminal", StringComparison.OrdinalIgnoreCase))
    //                 {
    //                     return true;
    //                 }
    //                 // 继续遍历上一级父进程
    //                 currentPid = parentPid.Value;
    //             }
    //         }
    //         catch
    //         {
    //             // 任意异常直接返回false，不影响程序整体运行
    //         }
    //         return false;
    //     }
    // }
        
    
    public static class AppSettings
    {
        // 搜索引擎配置
        public const string SearchUrlTemplate = "https://www.bing.com/search?q={0}";

        // 监控配置
        public const int MonitorIntervalMs = 1000;

        // 颜色配置
        public const ConsoleColor AmdColor = ConsoleColor.Red;
        public const ConsoleColor IntelColor = ConsoleColor.Blue;
        public const ConsoleColor DefaultColor = ConsoleColor.White;
        public const ConsoleColor LinkColor = ConsoleColor.Cyan;

        // 硬盘品牌颜色配置
        public const ConsoleColor WesternDigitalColor = ConsoleColor.Blue;
        public const ConsoleColor SeagateColor = ConsoleColor.Green;
        public const ConsoleColor ChinaBrandColor = ConsoleColor.Magenta;
        public const ConsoleColor SamsungColor = ConsoleColor.DarkBlue;
        public const ConsoleColor KingstonColor = ConsoleColor.DarkCyan;
        public const ConsoleColor DefaultDiskColor = ConsoleColor.Green;

        // GPU颜色配置
        public const ConsoleColor NvidiaColor = ConsoleColor.Green;
        public const ConsoleColor AmdGpuColor = ConsoleColor.Red;
        public const ConsoleColor IntelGpuColor = ConsoleColor.Blue;
        public const ConsoleColor DefaultGpuColor = ConsoleColor.White;

        // 提示
        public const ConsoleColor SuccessColor = ConsoleColor.Green;
        public const ConsoleColor TipColor = ConsoleColor.Yellow;
    }

    // ========================================
    // 数据结构
    // ========================================
    public struct SystemData
    {
        public float CpuUsage;
        public double RamUsagePercent;
        public TimeSpan Elapsed;
    }

    // ========================================
    // 硬件信息采集模块
    // ========================================
    public static class HardwareInfoCollector
    {
        
        // CPU信息
        public static void DisplayCpuInfo()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string cpuName = obj["Name"]?.ToString() ?? "";
                    ConsoleColor color = GetCpuColor(cpuName);

                    Console.ForegroundColor = color;
                    Console.Write($"CPU: {cpuName}");
                    Console.Write("|");
                    CreateSearchableHyperlink("Search", cpuName, AppSettings.LinkColor);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"why cant write shit:{ex.Message}");
            }
        }

        // GPU信息
        // GPU信息
public static void DisplayGpuInfo()
{
    try
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
        var gpuList = new List<(string Name, string AdapterRam, string DriverVersion, string VideoMode)>();
        foreach (ManagementObject obj in searcher.Get())
        {
            string gpuName = obj["Name"]?.ToString() ?? "";
            string adapterRam = obj["AdapterRAM"]?.ToString() ?? "";
            string driverVersion = obj["DriverVersion"]?.ToString() ?? "Unknown";
            string videoModeDescription = obj["VideoModeDescription"]?.ToString() ?? "";
            gpuList.Add((gpuName, adapterRam, driverVersion, videoModeDescription));
        }

        // GPU按重要性排序
        var sortedGpus = gpuList.OrderBy(gpu =>
        {
            string name = gpu.Name.ToLower();
            // 第一梯队：独立显卡
            if (name.Contains("amd") && name.Contains("rx") || 
                name.Contains("nvidia") && (name.Contains("gtx") || name.Contains("rtx")))
                return 0;
            // 第二梯队：核显/集成显卡
            else if (name.Contains("intel") || (name.Contains("amd") && !name.Contains("rx")))
                return 1;
            // 第三梯队：虚拟显卡
            else
                return 2;
        }).ToList();

        int gpuIndex = 1;
        foreach (var gpu in sortedGpus)
        {
            string gpuName = gpu.Name;
            string adapterRam = gpu.AdapterRam;
            string driverVersion = gpu.DriverVersion;
            string videoModeDescription = gpu.VideoMode;

            // 显存（未修复）
            string gpuMemGB = "";
            if (!string.IsNullOrEmpty(adapterRam) && long.TryParse(adapterRam, out long adapterRamBytes) && adapterRamBytes > 1024 * 1024 * 1024)
            {
                double memGB = Math.Round((double)adapterRamBytes / (1024 * 1024 * 1024));
                gpuMemGB = memGB > 0 ? $"{memGB}GB" : "";
            }

            ConsoleColor color = GetGpuColor(gpuName);
            Console.ForegroundColor = color;
            Console.Write($"GPU {gpuIndex}: {gpuName}");
            Console.Write("|");
            CreateSearchableHyperlink("Search", $"{gpuName}", AppSettings.LinkColor);
            gpuIndex++;
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"why cant write shit:{ex.Message}");
    }
}

        // RAM信息
        public static void DisplayRamInfo()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
                int ramIndex = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    // 插槽标签
                    string bankLabel = obj["BankLabel"]?.ToString() ?? "";
                    string deviceLocator = obj["DeviceLocator"]?.ToString() ?? "";
                    string slotTag = string.IsNullOrEmpty(bankLabel) ?
                        (string.IsNullOrEmpty(deviceLocator) ? $"RAM{ramIndex++}" : deviceLocator) : bankLabel;

                    // 基本信息
                    string manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                    string partnumber = obj["PartNumber"]?.ToString() ?? "";
                    string speed = obj["Speed"]?.ToString() ?? "";

                    // 容量
                    ulong capacityInGB = 0;
                    if (obj["Capacity"] != null && ulong.TryParse(obj["Capacity"].ToString(), out ulong capacity))
                    {
                        capacityInGB = (capacity / (1024 * 1024 * 1024));
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"RAM {slotTag}: {manufacturer} {partnumber} {speed}MHz {capacityInGB}GB");
                    Console.Write("|");
                    CreateSearchableHyperlink("Search", $"{manufacturer} {partnumber} {speed}MHz {capacityInGB}GB", AppSettings.LinkColor);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"why cant write shit:{ex.Message}");
            }
        }

        // 主板信息
        public static void DisplayMotherboardInfo()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                    string product = obj["Product"]?.ToString() ?? "Unknown";

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write($"Motherboard: {manufacturer} {product}");
                    Console.Write("|");
                    CreateSearchableHyperlink("Search", product, AppSettings.LinkColor);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"why cant write shit:{ex.Message}");
            }
        }

        // 磁盘信息（新增物理硬盘名称/序列号识别 + 品牌颜色）
        public static void DisplayDiskInfo()
        {
            try
            {
                // Step 1: 获取物理硬盘信息 (型号、序列号、接口类型)
                Dictionary<string, (string Model, string SerialNumber, string InterfaceType)> physicalDisks = new Dictionary<string, (string, string, string)>();
                ManagementObjectSearcher diskDriveSearcher = new ManagementObjectSearcher("SELECT DeviceID, Model, SerialNumber, InterfaceType FROM Win32_DiskDrive");
                foreach (ManagementObject drive in diskDriveSearcher.Get())
                {
                    string deviceId = drive["DeviceID"]?.ToString() ?? "";
                    string model = drive["Model"]?.ToString()?.Trim() ?? "Unknown Model";
                    string serialNumber = drive["SerialNumber"]?.ToString()?.Trim() ?? "Unknown Serial";
                    string interfaceType = drive["InterfaceType"]?.ToString() ?? "Unknown Interface";

                    physicalDisks[deviceId] = (model, serialNumber, interfaceType);
                }

                // Step 2: 建立 分区ID → 物理磁盘ID 映射
                Dictionary<string, string> partitionToDiskMap = new Dictionary<string, string>();
                ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher("SELECT DeviceID, DiskIndex FROM Win32_DiskPartition");
                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    string partitionId = partition["DeviceID"]?.ToString() ?? "";
                    uint diskIndex = Convert.ToUInt32(partition["DiskIndex"]);
                    string physicalDiskId = $"\\\\.\\PHYSICALDRIVE{diskIndex}";

                    partitionToDiskMap[partitionId] = physicalDiskId;
                }

                // Step 3: 建立 逻辑盘符 → 分区ID 映射
                Dictionary<string, List<string>> driveLetterToPartitionMap = new Dictionary<string, List<string>>();
                ManagementObjectSearcher logicalDiskToPartitionSearcher = new ManagementObjectSearcher(
                    "SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition");
                foreach (ManagementObject map in logicalDiskToPartitionSearcher.Get())
                {
                    string antecedent = map["Antecedent"]?.ToString() ?? "";
                    string dependent = map["Dependent"]?.ToString() ?? "";

                    string partitionId = ExtractValueFromWmiPath(antecedent, "DeviceID");
                    string driveLetter = ExtractValueFromWmiPath(dependent, "DeviceID");

                    if (!string.IsNullOrEmpty(driveLetter) && !string.IsNullOrEmpty(partitionId))
                    {
                        if (!driveLetterToPartitionMap.ContainsKey(driveLetter))
                        {
                            driveLetterToPartitionMap[driveLetter] = new List<string>();
                        }
                        driveLetterToPartitionMap[driveLetter].Add(partitionId);
                    }
                }

                // Step 4: 查询逻辑盘信息并关联物理硬盘
                ManagementObjectSearcher logicalDiskSearcher = new ManagementObjectSearcher("select * from Win32_LogicalDisk where DriveType=3");
                foreach (ManagementObject obj in logicalDiskSearcher.Get())
                {
                    string deviceID = obj["DeviceID"]?.ToString() ?? "";
                    string volumeName = obj["VolumeName"]?.ToString() ?? "";
                    ulong sizeInGB = 0;
                    ulong freeSpaceInGB = 0;

                    if (obj["Size"] != null && ulong.TryParse(obj["Size"].ToString(), out ulong size))
                    {
                        sizeInGB = size / (1024 * 1024 * 1024);
                    }

                    if (obj["FreeSpace"] != null && ulong.TryParse(obj["FreeSpace"].ToString(), out ulong freeSpace))
                    {
                        freeSpaceInGB = freeSpace / (1024 * 1024 * 1024);
                    }

                    // 获取对应物理硬盘信息
                    string diskModel = "Unknown Model";
                    string diskSerial = "Unknown Serial";
                    string diskInterface = "Unknown Interface";
                    if (driveLetterToPartitionMap.TryGetValue(deviceID, out var partitionIds))
                    {
                        foreach (var partitionId in partitionIds)
                        {
                            if (partitionToDiskMap.TryGetValue(partitionId, out var physicalDiskId) &&
                                physicalDisks.TryGetValue(physicalDiskId, out var diskInfo))
                            {
                                diskModel = diskInfo.Model;
                                diskSerial = diskInfo.SerialNumber;
                                diskInterface = diskInfo.InterfaceType;
                                break;
                            }
                        }
                    }

                    // 根据硬盘型号获取品牌对应颜色
                    ConsoleColor diskColor = GetDiskBrandColor(diskModel);
                    
                    // 新增物理硬盘信息 + 品牌颜色
                    Console.ForegroundColor = diskColor; // 使用品牌对应颜色
                    Console.Write($"Disk {deviceID}: {volumeName} {sizeInGB}GB ({freeSpaceInGB}GB Free) | Physical Disk: {diskModel} (Serial: {diskSerial}, Interface: {diskInterface})");
                    Console.Write("|");
                    CreateSearchableHyperlink("Search", diskModel, AppSettings.LinkColor); // 搜索链接改为硬盘型号
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"why cant write shit:{ex.Message}");
            }
        }

        // 辅助方法：根据硬盘型号判断品牌并返回对应颜色
        private static ConsoleColor GetDiskBrandColor(string diskModel)
        {
            if (string.IsNullOrEmpty(diskModel))
                return AppSettings.DefaultDiskColor;

            string modelLower = diskModel.ToLower();

            if (modelLower.Contains("western digital") || modelLower.Contains("wd"))
                return AppSettings.WesternDigitalColor;

            if (modelLower.Contains("seagate") || modelLower.Contains("st"))
                return AppSettings.SeagateColor;

            if (modelLower.Contains("aigo") || modelLower.Contains("zhitai") || 
                modelLower.Contains("gloway") || modelLower.Contains("ymtc") ||
                modelLower.Contains("长鑫") || modelLower.Contains("忆联") || modelLower.Contains("大华") ||
                modelLower.Contains("海康威视") || modelLower.Contains("hikvision"))
                return AppSettings.ChinaBrandColor;

            if (modelLower.Contains("samsung"))
                return AppSettings.SamsungColor;

            if (modelLower.Contains("kingston"))
                return AppSettings.KingstonColor;

            // 其他品牌用默认颜色
            return AppSettings.DefaultDiskColor;
        }

        // 解析WMI路径中的属性值
        private static string ExtractValueFromWmiPath(string wmiPath, string propertyName)
        {
            if (string.IsNullOrEmpty(wmiPath))
                return string.Empty;

            string pattern = $"{propertyName}=\"([^\"]+)\"";
            var match = Regex.Match(wmiPath, pattern);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        // 辅助方法：CPU颜色
        private static ConsoleColor GetCpuColor(string cpuName)
        {
            if (cpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
            {
                return AppSettings.AmdColor;
            }
            else if (cpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                return AppSettings.IntelColor;
            }
            else
            {
                return AppSettings.DefaultColor;
            }
        }

        // 辅助方法：GPU颜色
        private static ConsoleColor GetGpuColor(string gpuName)
        {
            if (gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                return AppSettings.NvidiaColor;
            }
            else if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
            {
                return AppSettings.AmdGpuColor;
            }
            else if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                return AppSettings.IntelGpuColor;
            }
            else
            {
                return AppSettings.DefaultGpuColor;
            }
        }

        // 辅助方法：创建搜索链接
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
    // 系统监控模块
    // ========================================
    public class SystemMonitor : IDisposable
    {
        private Dictionary<int, TimeSpan> _lastCpuTimes = new Dictionary<int, TimeSpan>();
        private DateTime _lastCheckTime = DateTime.Now;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;
        private readonly double _totalRamGB;
        private bool _disposed = false;

        public SystemMonitor()
        {
            // 获取总内存
            _totalRamGB = GetTotalRamGB();

            // 初始化计数器
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // 第一次调用通常返回0，用于初始化
            _cpuCounter.NextValue();
            _ramCounter.NextValue();
        }

        // 启动监控
        public void Start()
        {
            Console.WriteLine($"=== System Monitor ===");

            while (true)
            {
                Thread.Sleep(AppSettings.MonitorIntervalMs);

                // 收集系统数据
                SystemData systemData = CollectSystemData();

                // 找到Top CPU和Top RAM进程
                var (topCpuProcess, maxCpuUsage) = FindTopCpuProcess(systemData.Elapsed);
                Process? topMemProcess = FindTopMemoryProcess();

                // 显示结果
                DisplayResult(systemData, topCpuProcess, maxCpuUsage, topMemProcess);
            }
        }

        // 收集系统数据
        private SystemData CollectSystemData()
        {
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - _lastCheckTime;
            if (elapsed.TotalMilliseconds == 0) elapsed = TimeSpan.FromMilliseconds(1);

            float cpuUsage = _cpuCounter.NextValue();
            float availableMB = _ramCounter.NextValue();
            double availableGB = availableMB / 1024.0;
            double ramUsagePercent = ((_totalRamGB - availableGB) / _totalRamGB) * 100;

            return new SystemData
            {
                CpuUsage = cpuUsage,
                RamUsagePercent = ramUsagePercent,
                Elapsed = elapsed
            };
        }

        // 找到CPU占用最高的进程
        private (Process? process, double usage) FindTopCpuProcess(TimeSpan elapsed)
        {
            Dictionary<int, TimeSpan> currentCpuTimes = new Dictionary<int, TimeSpan>();
            Process? topProcess = null;
            double maxCpuUsage = 0.0;

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    TimeSpan currentTotalTime = p.TotalProcessorTime;
                    currentCpuTimes[p.Id] = currentTotalTime;

                    if (_lastCpuTimes.TryGetValue(p.Id, out TimeSpan lastTimeSpan))
                    {
                        double processCpuUsed = (currentTotalTime - lastTimeSpan).TotalMilliseconds / elapsed.TotalMilliseconds * 100.0 / Environment.ProcessorCount;
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

            _lastCpuTimes = currentCpuTimes;
            _lastCheckTime = DateTime.Now;

            return (topProcess, maxCpuUsage);
        }

        // 找到内存占用最高的进程
        private Process? FindTopMemoryProcess()
        {
            Process? topProcess = null;
            long maxMemory = 0;

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.PrivateMemorySize64 > maxMemory)
                    {
                        maxMemory = p.PrivateMemorySize64;
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

        // 显示结果
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
                double topMemMB = topMemProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
                topMemInfo = $" | Top RAM: {topMemProcess.ProcessName} ({topMemMB:0.00} MB)";
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] CPU: {data.CpuUsage:0.0}% | RAM: {data.RamUsagePercent:0.00}% {topCpuInfo} {topMemInfo}");
        }

        // 获取总RAM
        private double GetTotalRamGB()
        {
            ulong totalRamBytes = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    // 【修复】安全转换
                    if (obj["TotalPhysicalMemory"] != null)
                    {
                        totalRamBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                        break;
                    }
                }
            }
            return totalRamBytes / (1024.0 * 1024.0 * 1024.0);
        }

        // 实现 IDisposable 模式
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
                    _cpuCounter?.Dispose();
                    _ramCounter?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    // ========================================
    // 主程序入口
    // ========================================
    class Program
    {
        static void Main(string[] args)
        {
            // 硬件信息检测（只运行一次）
            Console.WriteLine("=== Let me looking Ver081 ===");
            Console.WriteLine("By: Nekona Alice");
            Console.WriteLine("Better use [Windows Terminal] or it might look weird lol");
            // Windows Terminal提示
            // bool isWT = TerminalHelper.IsRunningInWindowsTerminal();
            // if (isWT)
            // {
            //     Console.ForegroundColor = AppSettings.SuccessColor;
            //     Console.WriteLine("✅ WT ON! Everything its OK!");
            //     Console.ResetColor();
            // }
            // else
            // {
            //     Console.ForegroundColor = AppSettings.TipColor;
            //     Console.WriteLine("💡 No WT, console might die lol");
            //     Console.ResetColor();
            // }
            HardwareInfoCollector.DisplayCpuInfo();
            HardwareInfoCollector.DisplayGpuInfo();
            HardwareInfoCollector.DisplayRamInfo();
            HardwareInfoCollector.DisplayMotherboardInfo();
            HardwareInfoCollector.DisplayDiskInfo();
            Console.WriteLine();
            Console.WriteLine("Enter to start system monitoring...");
            Console.ReadLine();
            // 系统实时监控
            try
            {
                using var monitor = new SystemMonitor();
                monitor.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"why cant write shit:{ex.Message}");
            }
        }
    }
}