using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BucketTrayForWin7_
{
    internal static class Bucket
    {
        public static int[] BusyPercent { get; set; }
        public static string[] Drives { get; private set; }
        public static bool IsOnlyPhysicalSystemDrives { get; private set; }

        private static RegistryKey _bucketKey;
        private static List<long> _maxSize;
        private static List<DirectoryInfo> binPaths = new List<DirectoryInfo>();

        public static IEnumerable<long> GetMaxCapacity()
        {
            if (_maxSize != null && _maxSize?.Count != 0 &&
                Drives.Length == GetLogicalDrivesWithBin().Length)
                return _maxSize;

            Drives = GetLogicalDrivesWithBin();
            _maxSize = new List<long>();
            using (_bucketKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket"))
            {
                if (_bucketKey == null)
                    return _maxSize;

                string[] bucketsEnum = (string[])_bucketKey.GetValue("LastEnum");

                if (bucketsEnum.Length == Drives.Length)
                {
                    IsOnlyPhysicalSystemDrives = true;
                    using (_bucketKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume"))
                    {
                        List<string> bins = _bucketKey.GetSubKeyNames().ToList();

                        for (int i = 0; i < bucketsEnum.Length; i++)
                        {
                            bins.Remove(bucketsEnum[i].Substring(2, 38));
                        }

                        foreach (string bin in bins)
                        {
                            Registry.CurrentUser.DeleteSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume\{bin}");
                        }
                    }

                    for (int i = 0; i < bucketsEnum.Length; i++)
                    {
                        bucketsEnum[i] = bucketsEnum[i].Substring(2, 38);
                        using (_bucketKey = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume\{bucketsEnum[i]}"))
                        {
                            if (_bucketKey == null)
                                return _maxSize;

                            _maxSize.Add((int)_bucketKey.GetValue("MaxCapacity"));
                        }
                    }
                }
                else
                {
                    int size = 0;
                    IsOnlyPhysicalSystemDrives = false;

                    using (_bucketKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume"))
                    {
                        if (_bucketKey == null)
                            return _maxSize;

                        string[] bins = _bucketKey.GetSubKeyNames();

                        if (bins == null || bins.Length == 0)
                            return _maxSize;

                        foreach (string bin in bins)
                        {
                            using (_bucketKey = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume\{bin}"))
                            {
                                if (_bucketKey == null)
                                    continue;

                                size += (int)_bucketKey.GetValue("MaxCapacity");
                            }
                        }
                        _maxSize = new List<long>();
                        for (int i = 0; i < Drives.Length; i++)
                        {
                            _maxSize.Add(size);
                        }
                    }
                }
            }
            return _maxSize;
        }

        public static IEnumerable<long> GetBusyCapacity()
        {
            long totalSizeInBytes = 0;
            List<long> busyList = new List<long>();

            if (binPaths.Count == _maxSize.Count)
            {
                foreach (var binPath in binPaths)
                {
                    totalSizeInBytes = 0;
                    foreach (var file in binPath.GetFiles("*", SearchOption.AllDirectories))
                    {
                        totalSizeInBytes += file.Length;
                    }
                    totalSizeInBytes = totalSizeInBytes / (1024 * 1024);
                    busyList.Add(totalSizeInBytes);
                }

                return busyList;
            }

            binPaths = new List<DirectoryInfo>();
            foreach (var drive in Drives)
            {

                totalSizeInBytes = 0;
                DirectoryInfo recycleBin = new DirectoryInfo($"{drive}$Recycle.Bin");
                if (recycleBin.Exists)
                {
                    var dirs = recycleBin.GetDirectories();

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        recycleBin = new DirectoryInfo($"{drive}$Recycle.Bin\\{dirs[i]}");
                        try
                        {
                            foreach (var file in recycleBin.GetFiles("*", SearchOption.AllDirectories))
                            {
                                totalSizeInBytes += file.Length;
                            }

                            if (binPaths.Find(x => x.FullName == recycleBin.FullName) == null)
                                binPaths.Add(recycleBin);

                            totalSizeInBytes = totalSizeInBytes / (1024 * 1024);
                            busyList.Add(totalSizeInBytes);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }

            return busyList;
        }

        private static string[] GetLogicalDrivesWithBin()
        {
            List<string> drives = new List<string>();
            foreach (var drive in Environment.GetLogicalDrives())
            {
                DirectoryInfo recycleBin = new DirectoryInfo($"{drive}$Recycle.Bin");
                if (recycleBin.Exists)
                {
                    drives.Add(drive);
                }
            }
            return drives.ToArray();
        }

        public static double[] GetBusyPercent()
        {
            _ = GetMaxCapacity();
            double[] busyPercents = new double[_maxSize.Count];
            List<long> capasitys = GetBusyCapacity().ToList();

            if (capasitys.Count == 0)
                BusyPercent = new int[_maxSize.Count];

            for (int i = 0; i < capasitys.Count; i++)
            {
                busyPercents[i] = Math.Round((double)capasitys[i] * 100 / _maxSize[i]);
                if (BusyPercent != null && BusyPercent.Length == _maxSize.Count)
                {
                    BusyPercent[i] = (int)busyPercents[i];
                }
                else
                {
                    BusyPercent = new int[busyPercents.Length];
                    BusyPercent[i] = (int)busyPercents[i];
                }
            }
            return busyPercents;
        }

        public static IEnumerable<int> GetBusyPercentRounded()
        {
            double[] percents = GetBusyPercent();

            int[] percentsRounded = new int[percents.Length];

            for (int i = 0; i < percents.Length; i++)
            {
                if (percents[i] > 0 && percents[i] <= 25)
                    percentsRounded[i] = 25;
                else if (percents[i] > 25 && percents[i] <= 50)
                    percentsRounded[i] = 50;
                else if (percents[i] > 50 && percents[i] <= 75)
                    percentsRounded[i] = 75;
                else if (percents[i] > 75)
                    percentsRounded[i] = 100;
                else
                    percentsRounded[i] = 0;

            }
            return percentsRounded;
        }

        public static void Open()
        {
            Process.Start(new ProcessStartInfo()
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd",
                Arguments = "/c start shell:RecycleBinFolder",
            });
        }

        public static void Clear()
        {
            if (binPaths.Count != 0)
            {
                foreach (var binPath in binPaths)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd",
                        Arguments = $"/c rd /s /q {binPath.FullName}",
                    });
                }
                binPaths.Clear();
            }
        }
    }
}