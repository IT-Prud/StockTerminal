using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Utils
{
    public class Instance
    {
        private const int ALT = 0xA4;
        private const int EXTENDEDKEY = 0x1;
        private const int KEYUP = 0x2;

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static readonly string InstanceId;

        private static Mutex MutexQuote = null;

        static Instance()
        {
            InstanceId = "Global\\" + Application.ProductName + "-a7b93a8dde-154c1e8d-8ea4-4510-8c37-554d449ca7a2";
        }

        public static int RegisterInstance()
        {
            try
            {
                MutexQuote = new Mutex(false, InstanceId);

                if (MutexQuote.WaitOne(0, false))
                    return 1;
            }
            catch (AbandonedMutexException) // Mutex abandoned by another process, can be treated as mutex obtained success.
            {
                UnregisterInstance();
                return 1;
            }
            catch  // Other more serious exception has been thrown, failed to obtain the mutex and should prompt the user.
            {
                UnregisterInstance();
                return -2;
            }

            // Another instance is running, own by same user, just bring up the existing instance to the front.
            UnregisterInstance();

            return -1;
        }

        public static void UnregisterInstance()
        {
            if (MutexQuote != null)
            {
                MutexQuote.Close();
                MutexQuote = null;
            }
        }

        public static void ShowInstance(string WindowTitle, FormWindowState WindowState)
        {
            int processFound = 0;

            try
            {
                for (int i = 0; i < 3; i++)
                {
                    foreach (Process p in Process.GetProcesses())
                    {
                        if (p.MainWindowTitle.StartsWith(WindowTitle, StringComparison.InvariantCultureIgnoreCase))
                        {
                            processFound++;
                            // Target process found.

                            // Guard: check if window already has focus.
                            if (p.MainWindowHandle == GetForegroundWindow()) break;

                            // Show window in WindowsState.
                            switch (WindowState)
                            {
                                case FormWindowState.Maximized:
                                    ShowWindow(p.MainWindowHandle, SW_SHOWMAXIMIZED);
                                    break;

                                case FormWindowState.Minimized:
                                    ShowWindow(p.MainWindowHandle, SW_MINIMIZE);
                                    break;

                                default:
                                    ShowWindow(p.MainWindowHandle, SW_RESTORE);
                                    break;
                            }

                            // Simulate an "ALT" key press.
                            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | 0, 0);

                            // Simulate an "ALT" key release.
                            keybd_event((byte)ALT, 0x45, EXTENDEDKEY | KEYUP, 0);

                            // Show window in forground.
                            SetForegroundWindow(p.MainWindowHandle);

                            return;
                        }
                    }

                    // Waiting for target process.
                    Thread.Sleep(100);
                }

                // processFound <= 0 means failed to find target process.");
            }
            catch { }
        }
    }
}
