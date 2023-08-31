using BucketTrayForWin7_;
using BucketTrayForWin7_.Properties;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32.TaskScheduler;
using System.Reflection;

internal static class SettingsHelper
{
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
        int maxPercent = Bucket.GetBusyPercentRounded().ToList().Max();

        if (!isLight)
        {
            switch (maxPercent)
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
            switch (maxPercent)
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
        using (TaskService ts = new TaskService())
        {
            if (ts.FindTask("BinStart") != null)
                IsAutoStart = true;
            else
                IsAutoStart = false;
        }
    }

    public static void WriteSettings()
    {
        using (TaskService ts = new TaskService())
        {
            if (IsAutoStart && ts.FindTask("BinStart") == null)
            {
                TaskDefinition td = ts.NewTask();
                td.Triggers.Add(new LogonTrigger() { UserId = $"{Environment.UserDomainName}\\{Environment.UserName}", Enabled = true });
                td.Actions.Add(new ExecAction(Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe")));
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Settings.DisallowStartIfOnBatteries = false;
                ts.RootFolder.RegisterTaskDefinition(@"BinStart", td);

                IsAutoStart = true;
            }
            else
            {
                ts.RootFolder.DeleteTask("BinStart");
                IsAutoStart = false;
            }
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
        Process.Start(new ProcessStartInfo()
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "cmd",
            Arguments = $"/c mklink \"{ShortcutPath}\" \"{TargetPath}\"",
            Verb = "runas",
        });
    }
}