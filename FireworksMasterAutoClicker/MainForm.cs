using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using FMAC.EmulatorInterop;

using OpenCvSharp;

using Windows.Win32;
using Windows.Win32.Foundation;

using SystemIO = System.IO;

namespace FMAC
{
    public partial class MainForm : Form
    {

        private bool exit = false;
        private HWND hMuMu = HWND.Null;
        private string sdkLibraryPathMuMu = string.Empty;
        private string installDirMuMu = string.Empty;
        private int nemuHandle = 0;

        private const string TARGET_PKG_NAME = "com.hypergryph.skland";
        private const string DEBUG_PKG_NAME = "com.mumu.launcher";

        public MainForm()
        {
            InitializeComponent();

            Application.ApplicationExit += (object? sender, EventArgs e) => { ResetConnectionStatus(); exit = true; };

            btnToggle.Size = btnToggle.Size * 2;
            btnPreview.Size = btnToggle.Size;

            var buttonSize = btnPreview.Size;
            var paddingTop = (this.Size.Height - buttonSize.Height) / 2;
            var paddingLeft = (this.Size.Width - buttonSize.Width * 2) / 3;

            btnToggle.Left = paddingLeft;
            btnToggle.Top = paddingTop;

            btnPreview.Left = paddingLeft + buttonSize.Width + paddingLeft;
            btnPreview.Top = paddingTop;

            ThreadPool.QueueUserWorkItem(ConnectorMain);
            ThreadPool.QueueUserWorkItem(WorkerMain);
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {

        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {

        }

        private void ConnectorMain(object? status)
        {
            do
            {
                Thread.Sleep(100);

                if (hMuMu == HWND.Null || !Native.IsWindow(hMuMu))
                {
                    var processes = Process.GetProcessesByName("MuMuPlayer");
                    if (processes.Length == 0)
                    {
                        continue;
                    }
                    var emulatorProcess = processes[0];
                    emulatorProcess.Exited += EmulatorProcess_Exited;
                    hMuMu = new HWND(emulatorProcess.MainWindowHandle);
                    var parentPathInfo = SystemIO.Directory.GetParent(emulatorProcess.MainModule!.FileName);
                    sdkLibraryPathMuMu = SystemIO.Path.Combine(parentPathInfo!.FullName, "sdk", "external_renderer_ipc.dll");
                    installDirMuMu = parentPathInfo.Parent!.FullName;
                }

                if (!SystemIO.File.Exists(sdkLibraryPathMuMu))
                {
                    continue;
                }

                if (Native.LoadLibrary(sdkLibraryPathMuMu).IsNull)
                {
                    continue;
                }

                nemuHandle = MuMu.Connect(installDirMuMu, 0);
            } while (ShouldLoop());
        }

        private void WorkerMain(object? status)
        {
            while (ShouldLoop())
            {
                Thread.Sleep(100);
            }
            Log.Debug($"Successfully create nemu handle, id={nemuHandle}");
            do
            {
                Thread.Sleep(100);
                var mat = GetScreenCapture();
                if (mat is null)
                {
                    continue;
                }
                // TODO
            } while (!exit);
            ResetConnectionStatus();
        }

        private void EmulatorProcess_Exited(object? sender, EventArgs e)
        {
            ResetConnectionStatus();
            ThreadPool.QueueUserWorkItem(ConnectorMain);
            ThreadPool.QueueUserWorkItem(WorkerMain);
        }

        private Mat? GetScreenCapture()
        {
            int width = 0, height = 0;
            var displayId = MuMu.GetDisplayId(nemuHandle, DEBUG_PKG_NAME, 0);
            if (displayId < 0)
            {
                return default;
            }
            if (MuMu.CaptureDisplay(nemuHandle, displayId, 0, ref width, ref height, ref Unsafe.NullRef<byte>()) != 0)
            {
                return default;
            }
            var bufferSize = width * height * 4;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            ref var pixels = ref Unsafe.AsRef(ref buffer[0]);
            if (MuMu.CaptureDisplay(nemuHandle, displayId, bufferSize, ref width, ref height, ref pixels) != 0)
            {
                return default;
            }

            var mat = new Mat(height, width, MatType.CV_8UC4);
            unsafe
            {
                var raw = Mat.FromPixelData(height, width, MatType.CV_8UC4, (nint)Unsafe.AsPointer(ref pixels));
                Cv2.CvtColor(raw, mat, ColorConversionCodes.RGBA2BGR);
            }
            Cv2.Flip(mat, mat, FlipMode.X);

            return mat;
        }

        private bool ShouldLoop()
        {
            return !exit && (nemuHandle == 0);
        }

        private void ResetConnectionStatus()
        {
            Log.Debug("Reset connection status.");
            if (nemuHandle != 0)
            {
                MuMu.Disconnect(nemuHandle);
                nemuHandle = 0;
            }
        }
    }
}
