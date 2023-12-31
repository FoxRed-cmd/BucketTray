﻿using System;
using System.Linq;
using System.Windows.Forms;

namespace BucketTrayForWin7_
{
    internal class Program
    {
        static Timer _timer;
        public static NotifyIcon _icon;
        static ContextMenuStrip _menu;

        static void Main(string[] args)
        {
            SettingsHelper.ReadSettings();
            Bucket.GetBusyPercent();

            _menu = new ContextMenuStrip()
            {
                ShowImageMargin = false,
                ShowCheckMargin = true,
            };
            _menu.Items.Add("Open", null, (s, e) => Bucket.Open());
            _menu.Items.Add("Clear", null, (s, e) => Bucket.Clear());
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Start with windows", null, (s, e) =>
            {
                var item = s as ToolStripItem;
                if (!SettingsHelper.IsAutoStart)
                {
                    SettingsHelper.IsAutoStart = true;
                    SettingsHelper.WriteSettings();
                    ((ToolStripMenuItem)_menu.Items[3]).Checked = true;
                }
                else
                {
                    SettingsHelper.IsAutoStart = false;
                    SettingsHelper.WriteSettings();
                    ((ToolStripMenuItem)_menu.Items[3]).Checked = false;
                }
            });
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Close", null, (s, e) => Application.Exit());

            if (SettingsHelper.IsAutoStart)
                ((ToolStripMenuItem)_menu.Items[3]).Checked = true;

            _icon = new NotifyIcon()
            {
                Visible = true,
                ContextMenuStrip = _menu,
            };

            _icon.MouseClick += Icon_MouseClick;
            _icon.MouseDoubleClick += (s, e) => Bucket.Open();

            _timer = new Timer()
            {
                Enabled = true,
                Interval = 2000,
            };

            _timer.Tick += (s, e) =>
            {
                SettingsHelper.CheckThemeChange();
                SettingsHelper.ChangeIcon(SettingsHelper.IsLightTheme);

                if (Bucket.GetMaxCapacity().Min() == Bucket.GetMaxCapacity().Max() && !Bucket.IsOnlyPhysicalSystemDrives)
                {
                    _icon.Text = $"BucketTray Busy ≈ {Math.Round(Bucket.BusyPercent.Sum(), 1)}%";
                }
                else
                {
                    _icon.Text = "BucketTray Busy:";
                    for (int i = 0; i < Bucket.BusyPercent.Length; i++)
                    {
                        _icon.Text += $"\n{Bucket.Drives[i]} ≈ {Math.Round(Bucket.BusyPercent[i], 1)}%";
                    }
                }
            };

            Application.ApplicationExit += (s, e) =>
            {
                _icon.Visible = false;
                _icon.Dispose();
            };

            Application.Run();
        }

        private static void Icon_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Middle:
                    Application.Exit();
                    break;
            }
        }
    }
}