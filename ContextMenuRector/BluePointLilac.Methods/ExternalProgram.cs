using ContextMenuRector.Methods;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ContextMenuRector.BluePointLilac.Methods
{
    /// <summary>外部程序</summary>
    public static class ExternalProgram
    {
        /// <summary>在 Regedit 中跳转指定路径并定位指定键名</summary>
        /// <param name="regPath">注册表项路径</param>
        /// <param name="valueName">注册表键名</param>
        /// <param name="moreOpen">窗口是否多开</param>
        public static void JumpRegEdit(string regPath, string valueName = null, bool moreOpen = false)
        {
            // 还有一种方法，修改 HKCU\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit
            // 中的 LastKey 键值（记录上次关闭注册表编辑器时的注册表路径）为要跳转的注册表项路径 regPath，
            // 再使用 Process.Start("regedit.exe", "-m")打开注册表编辑器
            // 优点：代码少、不会有Bug。缺点：不能定位具体键，没有逐步展开效果
            if (regPath == null) return;
            Process process;
            IntPtr hMain = FindWindow("RegEdit_RegEdit", null);
            if (hMain != IntPtr.Zero && !moreOpen)
            {
                GetWindowThreadProcessId(hMain, out int id);
                process = Process.GetProcessById(id);
            }
            else
            {
                // 注册表编辑器窗口多开
                process = StartRegEdit("-m");
                if (process == null) return; // 启动失败时已提示用户，直接返回
                // regedit 启动后不一定会有窗口：可能启动即退出、被安全软件拦截，或与已有实例合并。
                // 此时对已退出的进程调用 WaitForInputIdle 会抛 InvalidOperationException
                if (!WaitRegEditWindow(process, out hMain)) return; // 等不到窗口时已提示用户
            }

            ShowWindowAsync(hMain, SW_SHOWNORMAL);
            SetForegroundWindow(hMain);
            IntPtr hTree = FindWindowEx(hMain, IntPtr.Zero, "SysTreeView32", null);
            IntPtr hList = FindWindowEx(hMain, IntPtr.Zero, "SysListView32", null);

            SetForegroundWindow(hTree);
            SetFocus(hTree);
            TryWaitForInputIdle(process);
            SendMessage(hTree, WM_KEYDOWN, VK_HOME, null);
            Thread.Sleep(100);
            TryWaitForInputIdle(process);
            SendMessage(hTree, WM_KEYDOWN, VK_RIGHT, null);
            foreach (char chr in Encoding.Default.GetBytes(regPath))
            {
                TryWaitForInputIdle(process);
                if (chr == '\\')
                {
                    Thread.Sleep(100);
                    SendMessage(hTree, WM_KEYDOWN, VK_RIGHT, null);
                }
                else
                {
                    SendMessage(hTree, WM_CHAR, Convert.ToInt16(chr), null);
                }
            }

            if (string.IsNullOrEmpty(valueName)) return;
            using (RegistryKey key = RegistryEx.GetRegistryKey(regPath))
            {
                if (key?.GetValue(valueName) == null) return;
            }
            Thread.Sleep(100);
            SetForegroundWindow(hList);
            SetFocus(hList);
            TryWaitForInputIdle(process);
            SendMessage(hList, WM_KEYDOWN, VK_HOME, null);
            foreach (char chr in Encoding.Default.GetBytes(valueName))
            {
                TryWaitForInputIdle(process);
                SendMessage(hList, WM_CHAR, Convert.ToInt16(chr), null);
            }
            process.Dispose();
        }

        /// <summary>在 Explorer 中选中指定文件或文件夹</summary>
        /// <param name="filePath">文件或文件夹路径</param>
        /// <param name="moreOpen">窗口是否多开</param>
        public static void JumpExplorer(string filePath, bool moreOpen = false)
        {
            if (filePath == null) return;
            if (!moreOpen)
            {
                IntPtr pidlList = ILCreateFromPathW(filePath);
                if (pidlList == IntPtr.Zero) return;
                SHOpenFolderAndSelectItems(pidlList, 0, IntPtr.Zero, 0);
                ILFree(pidlList);
            }
            else
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "explorer.exe";
                    process.StartInfo.Arguments = $"/select, {filePath}";
                    process.Start();
                }
            }
        }

        /// <summary>在 Explorer 中打开指定目录</summary>
        /// <param name="dirPath">目录路径</param>
        public static void OpenDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return;
            Process.Start("explorer.exe", dirPath);
        }

        /// <summary>打开文件或文件夹的属性对话框</summary>
        /// <param name="filePath">文件或文件夹路径</param>
        public static bool ShowPropertiesDialog(string filePath)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO
            {
                lpVerb = "Properties",
                // 显示详细信息选项卡，此处有语言差异
                // lpParameters = ResourceString.GetDirectString("@shell32.dll,-31433"), // "详细信息",
                lpFile = filePath,
                nShow = SW_SHOW,
                fMask = SEE_MASK_INVOKEIDLIST,
                cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO))
            };
            return ShellExecuteEx(ref info);
        }

        /// <summary>打开指定未关联打开方式的扩展名的打开方式对话框</summary>
        /// <param name="extension">文件扩展名</param>
        public static void ShowOpenWithDialog(string extension)
        {
            //Win10 调用 SHOpenWithDialog API 或调用 OpenWith.exe -override "%1"
            //或调用 "rundll32.exe shell32.dll, OpenAs_RunDLL %1" 能显示打开方式对话框，但都不能设置默认应用
            //以下方法只针对未关联打开方式的扩展名显示系统打开方式对话框，对于已关联打开方式的扩展名会报错
            string tempPath = $"{Path.GetTempPath()}{Guid.NewGuid()}{extension}";
            File.WriteAllText(tempPath, "");
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = tempPath,
                    Verb = "openas"
                };
                process.Start();
            }
            File.Delete(tempPath);
        }

        /// <summary>重启 Explorer</summary>
        public static void RestartExplorer()
        {
            using (Process process = new Process())
            {
                // 有些系统有 tskill.exe 可以直接调用 tskill explorer 命令
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = "-f -im explorer.exe",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                process.Start();
                process.WaitForExit();
                process.StartInfo = new ProcessStartInfo("explorer.exe");
                process.Start();
            }
        }

        /// <summary>调用默认浏览器打开指定网址</summary>
        /// <param name="url">网址</param>
        public static void OpenWebUrl(string url)
        {
            if (url == null) return;
            // 替换网址转义符
            url = url.Replace("%", "%25").Replace("#", "%23").Replace("&", "%26").Replace("+", "%2B");
            // 通过 Explorer 来调用默认浏览器打开链接
            Process.Start("explorer.exe", $"\"{url}\"");
        }

        /// <summary>启动注册表编辑器</summary>
        /// <param name="arguments">命令行参数</param>
        /// <returns>启动失败时(如系统 regedit.exe 缺失或损坏)提示用户并返回 null，不抛出异常</returns>
        public static Process StartRegEdit(string arguments)
        {
            try
            {
                return Process.Start("regedit.exe", arguments);
            }
            catch (Win32Exception)
            {
                // 例如 C:\Windows\System32\regedit.exe 被其它文件覆盖损坏时，Windows 返回 ERROR_BAD_EXE_FORMAT
                string path = Path.Combine(Environment.SystemDirectory, "regedit.exe");
                AppMessageBox.Show($"{AppString.Message.RegEditUnavailable}\r\n{path}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>等待注册表编辑器窗口出现</summary>
        /// <param name="process">StartRegEdit 启动的进程，可能已经退出</param>
        /// <param name="hMain">找到的注册表编辑器窗口句柄</param>
        /// <returns>是否在超时前等到了窗口</returns>
        private static bool WaitRegEditWindow(Process process, out IntPtr hMain)
        {
            for (int i = 0; i < 30; i++) // 最多等待 3 秒
            {
                hMain = FindWindow("RegEdit_RegEdit", null);
                if (hMain != IntPtr.Zero) return true;
                Thread.Sleep(100);
                if (process.HasExited) break; // 进程已退出，且始终没有窗口
            }
            hMain = IntPtr.Zero;
            if (!process.HasExited) process.Dispose(); // 进程仍在运行但没有窗口，句柄留着也没用
            AppMessageBox.Show(AppString.Message.RegEditNoWindow, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        /// <summary>等待进程进入空闲状态；进程没有图形界面或已退出时忽略，不抛出异常</summary>
        private static void TryWaitForInputIdle(Process process)
        {
            try
            {
                process.WaitForInputIdle();
            }
            catch (InvalidOperationException)
            {
                Thread.Sleep(100); // 退化为固定延时，后续窗口消息照常发送
            }
        }

        /// <summary>导出指定注册表项的 .reg 文件</summary>
        /// <param name="regPath">注册表项路径</param>
        /// <param name="filePath">.reg 文件保存路径</param>
        /// <returns>是否成功导出了 .reg 文件</returns>
        public static bool ExportRegistry(string regPath, string filePath)
        {
            Process process = StartRegEdit($"/e \"{filePath}\" \"{regPath}\"");
            if (process == null) return false;
            using (process) process.WaitForExit();
            // regedit.exe 的退出码不可靠，这里以文件是否真的写出了内容为准
            return File.Exists(filePath) && new FileInfo(filePath).Length > 0;
        }

        /// <summary>打开记事本显示指定文本</summary>
        /// <param name="text">要显示的文本</param>
        public static void OpenNotepadWithText(string text)
        {
            using (Process process = Process.Start("notepad.exe"))
            {
                TryWaitForInputIdle(process);
                IntPtr handle = FindWindowEx(process.MainWindowHandle, IntPtr.Zero, "Edit", null);
                SendMessage(handle, WM_SETTEXT, 0, text);
            }
        }

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;
        private const int WM_SETTEXT = 0xC;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_CHAR = 0x102;
        private const int VK_HOME = 0x24;
        private const int VK_RIGHT = 0x27;

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(IntPtr hwnd, out int ID);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChild, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidlList);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern IntPtr SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }
    }
}