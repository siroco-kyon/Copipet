using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Copipet.Forms;
using Copipet.Services;

namespace Copipet
{
    internal static class Program
    {
        private const string MutexName = @"Local\Copipet_SingleInstance";
        private const int SwRestore = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // 2つ目の起動では、すでに開いているウィンドウを前に出して終わる
                    // （2つ同時に開くと保存内容が上書きし合うため）
                    ActivateExistingInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var store = new DataStore();
                LoadResult result;
                try
                {
                    result = store.Load();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("データフォルダを準備できませんでした。\n\n" + store.DirectoryPath + "\n\n" + ex.Message,
                        "コピペット", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Application.Run(new MainForm(store, result.Data, result.Notice));
                GC.KeepAlive(mutex);
            }
        }

        private static void ActivateExistingInstance()
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id == current.Id || process.SessionId != current.SessionId) continue;
                    IntPtr handle = process.MainWindowHandle;
                    if (handle == IntPtr.Zero) continue;
                    ShowWindow(handle, SwRestore);
                    SetForegroundWindow(handle);
                    break;
                }
            }
            catch (Exception)
            {
                // 前に出せなくても問題はない
            }
        }
    }
}
