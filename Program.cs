using System;
using System.Text;
using System.Windows.Forms;

namespace AshClicker
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 未处理异常/进程退出时，补发所有仍处于按下状态的输出键。
            Application.ThreadException += (s, e) => KeySender.EmergencyReleaseAllStatic();
            AppDomain.CurrentDomain.UnhandledException += (s, e) => KeySender.EmergencyReleaseAllStatic();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => KeySender.EmergencyReleaseAllStatic();

            Application.Run(new AshForm());
        }
    }
}