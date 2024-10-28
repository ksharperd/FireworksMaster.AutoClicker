using System.Runtime.InteropServices;

using Microsoft.Win32;

using Windows.Win32;

namespace FMAC
{
    internal static partial class Program
    {

        private const string DarkModeKeyPath = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
        private const string DarkModeKey = "AppsUseLightTheme";
        private const int SystemDarkModeDisabled = 1;

        enum PreferredAppMode
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight,
            Max
        };

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        internal static void Main()
        {
            Native.AllocConsole();
            Console.Title = "FMAC Console";

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitor);
            //ApplicationConfiguration.Initialize();

            if (GetSystemColour() == 0)
            {
#if NET9_0_OR_GREATER
                Log.Debug("Setting up dark mode.");
#pragma warning disable WFO5001
                Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001
                if (Environment.OSVersion.Version.Build >= 18362)
                {
                    SetPreferredAppMode(PreferredAppMode.AllowDark);
                }
#else
                Log.Warn("Skip setting dark mode.");
#endif
            }

            Application.Run(new MainForm());
        }

        private static int GetSystemColour()
        {
            int systemColourMode = SystemDarkModeDisabled;

            try
            {
                systemColourMode = Math.Abs((Registry.GetValue(
                    keyName: DarkModeKeyPath,
                    valueName: DarkModeKey,
                    defaultValue: SystemDarkModeDisabled) as int?) ?? systemColourMode);
            }
            catch (Exception)
            {
            }

            return systemColourMode;
        }

        [LibraryImport("UxTheme", EntryPoint = "#135")]
        private static partial PreferredAppMode SetPreferredAppMode(PreferredAppMode appMode);

    }
}