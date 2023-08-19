using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace BucketTray
{
    internal static class Bucket
    {
        public static int BusyPercent { get; set; }

        private static RegistryKey _bucketKey;
        private static PowerShell _powerShell;
        private static long _maxSize;
        private static string[] drives = Environment.GetLogicalDrives();
        private static List<DirectoryInfo> binPaths = new List<DirectoryInfo>();

        public static long GetMaxCapacity()
        {
            if (_maxSize != 0)
                return _maxSize;

            using (_bucketKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket"))
            {
                if (_bucketKey == null)
                    return _maxSize;

                string[] bucketsEnum = (string[])_bucketKey.GetValue("LastEnum");

                for (int i = 0; i < bucketsEnum.Length; i++)
                {
                    bucketsEnum[i] = bucketsEnum[i].Substring(2, 38);
                    using (_bucketKey = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume\{bucketsEnum[i]}"))
                    {
                        if (_bucketKey == null)
                            return _maxSize;

                        _maxSize += (int)_bucketKey.GetValue("MaxCapacity");
                    }
                }
            }
            return _maxSize;
        }

        public static long GetBusyCapacity()
        {
            long totalSizeInBytes = 0;

            if (binPaths.Count != 0)
            {
                foreach (var binPath in binPaths) 
                {
                    foreach (var file in binPath.GetFiles("*", SearchOption.AllDirectories))
                    {
                        totalSizeInBytes += file.Length;
                    }
                }
                totalSizeInBytes = totalSizeInBytes / (1024 * 1024);
                return totalSizeInBytes;
            }

            foreach (var drive in drives)
            {
                DirectoryInfo recycleBin = new DirectoryInfo($"{drive}$Recycle.Bin");
                if (recycleBin.Exists)
                {
                    var dirs = recycleBin.GetDirectories();

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        try
                        {
                            recycleBin = new DirectoryInfo($"{drive}$Recycle.Bin\\{dirs[i]}");
                            foreach (var file in recycleBin.GetFiles("*", SearchOption.AllDirectories))
                            {
                                if (binPaths.Find(x => x.FullName == recycleBin.FullName) == null)
                                    binPaths.Add(recycleBin);
                                
                                totalSizeInBytes += file.Length;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }
            totalSizeInBytes = totalSizeInBytes / (1024 * 1024);
            return totalSizeInBytes;
        }

        private static double GetBusyPercent()
        {
            double busyPercent = Math.Round((double)((double)GetBusyCapacity() * 100 / GetMaxCapacity()), 10);
            BusyPercent = (int)busyPercent;
            return busyPercent;
        }

        public static int GetBusyPercentRounded()
        {
            double percent = GetBusyPercent();

            if (percent > 0 && percent <= 25)
                return 25;
            else if (percent > 25 && percent <= 50)
                return 50;
            else if (percent > 50 && percent <= 75)
                return 75;
            else if (percent > 75)
                return 100;

            return 0;
        }

        public static void Open()
        {
            using (_powerShell = PowerShell.Create())
            {
                _powerShell.AddCommand("start").AddArgument("shell:RecycleBinFolder");
                _powerShell.Invoke();
            }
        }

        public static void Clear()
        {
            using (_powerShell = PowerShell.Create())
            {
                _powerShell.AddScript("Clear-RecycleBin -force");
                _powerShell.Invoke();
            }
        }
    }
}