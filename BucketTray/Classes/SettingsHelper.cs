using Microsoft.Win32;
using System.Reflection;
using BucketTray.Properties;
using System;
using System.IO;
using System.Management.Automation;
using BucketTray;

internal static class SettingsHelper
{
    private static PowerShell _powerShell;
    public static bool IsAutoStart { get; set; } = false;
    public static bool IsLightTheme
    {
        get => _isLightTheme;
        set
        {
            if (_isLightTheme == value)
                return;

            _isLightTheme = value;

            ChangeIcon(IsLightTheme);
        }
    }

    private static string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup", "BucketTray.lnk");
    private static RegistryKey _wrReg;
    private static bool _isLightTheme;

    public static void ChangeIcon(bool isLight)
    {
        if (!isLight)
        {
            switch (Bucket.GetBusyPercentRounded())
            {
                case 0:
                    if (Program._icon.Icon != Resources.Bin0)
                        Program._icon.Icon = Resources.Bin0;
                    break;
                case 25:
                    if (Program._icon.Icon != Resources.Bin25)
                        Program._icon.Icon = Resources.Bin25;
                    break;
                case 50:
                    if (Program._icon.Icon != Resources.Bin50)
                        Program._icon.Icon = Resources.Bin50;
                    break;
                case 75:
                    if (Program._icon.Icon != Resources.Bin75)
                        Program._icon.Icon = Resources.Bin75;
                    break;
                case 100:
                    if (Program._icon.Icon != Resources.Bin100)
                        Program._icon.Icon = Resources.Bin100;
                    break;
            }
        }
        else
        {
            switch (Bucket.GetBusyPercentRounded())
            {
                case 0:
                    if (Program._icon.Icon != Resources.Bin0B)
                        Program._icon.Icon = Resources.Bin0B;
                    break;
                case 25:
                    if (Program._icon.Icon != Resources.Bin25B_1)
                        Program._icon.Icon = Resources.Bin25B_1;
                    break;
                case 50:
                    if (Program._icon.Icon != Resources.Bin50B_1)
                        Program._icon.Icon = Resources.Bin50B_1;
                    break;
                case 75:
                    if (Program._icon.Icon != Resources.Bin75B_1)
                        Program._icon.Icon = Resources.Bin75B_1;
                    break;
                case 100:
                    if (Program._icon.Icon != Resources.Bin100B)
                        Program._icon.Icon = Resources.Bin100B;
                    break;
            }
        }
        
    }

    public static void ReadSettings()
    {
        if (File.Exists(_path))
            IsAutoStart = true;
        else
            IsAutoStart = false;
    }

    public static void WriteSettings()
    {
        if (IsAutoStart && !File.Exists(_path))
        {
            Create(_path, Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));
            IsAutoStart = true;
        }
        else if (File.Exists(_path))
        {
            File.Delete(_path);
            IsAutoStart = false;
        }
    }

    public static void CheckThemeChange()
    {
        using (_wrReg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
        {
            if (_wrReg is null)
                return;

            if (_wrReg.GetValue("SystemUsesLightTheme") != null)
                IsLightTheme = Convert.ToBoolean(_wrReg.GetValue("SystemUsesLightTheme"));
        }
    }

    private static void Create(string ShortcutPath, string TargetPath)
    {
        using (_powerShell = PowerShell.Create())
        {
            _powerShell.AddScript($"$WshShell = New-Object -comObject WScript.Shell\r\n" +
                $"$Shortcut = $WshShell.CreateShortcut(\"{ShortcutPath}\")\r\n" +
                $"$Shortcut.TargetPath = \"{TargetPath}\"\r\n" +
                $"$Shortcut.WorkingDirectory = \"{TargetPath.Remove(TargetPath.LastIndexOf("\\"))}\"\r\n" +
                $"$Shortcut.Save()");
            _powerShell.Invoke();
        }
    }
}