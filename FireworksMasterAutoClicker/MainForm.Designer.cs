using SystemIO = System.IO;
using System.Diagnostics;

using Windows.Win32;
using Windows.Win32.Foundation;

using OpenCvSharp;
using FMAC.EmulatorInterop;
using System.Runtime.CompilerServices;
using System.Buffers;

namespace FMAC
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private Button btnToggle = new Button();
        private Button btnPreview = new Button();

        private bool exit = false;
        private HWND hMuMu = HWND.Null;
        private string sdkLibraryPathMuMu = string.Empty;
        private string installDirMuMu = string.Empty;
        private nint hNemuSDK = 0;
        private int nemuHandle = 0;

        private const string TARGET_PKG_NAME = "com.hypergryph.skland";
        private const string DEBUG_PKG_NAME = "com.mumu.launcher";

        private void MainForm_Load(object sender, EventArgs e)
        {
            Application.ApplicationExit += (object sender, EventArgs e) => { ResetConnectionStatus(); exit = true; };

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

        private void ConnectorMain(object status)
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
                    var parentPathInfo = SystemIO.Directory.GetParent(emulatorProcess.MainModule.FileName);
                    sdkLibraryPathMuMu = SystemIO.Path.Combine(parentPathInfo.FullName, "sdk", "external_renderer_ipc.dll");
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

        private void WorkerMain(object status)
        {
            while (ShouldLoop())
            {
                Thread.Sleep(100);
            }
            Log.Debug($"Successfully create nemu handle, id={nemuHandle}");
            do
            {
                Thread.Sleep(100);
                int width = 0, height = 0;
                var displayId = MuMu.GetDisplayId(nemuHandle, TARGET_PKG_NAME, 0);
                if (displayId < 0)
                {
                    continue;
                }
                if (MuMu.CaptureDisplay(nemuHandle, displayId, 0, ref width, ref height, ref Unsafe.NullRef<byte>()) != 0)
                {
                    continue;
                }
                var bufferSize = width * height * 4;
                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                ref var pixels = ref Unsafe.AsRef(ref buffer[0]);
                if (MuMu.CaptureDisplay(nemuHandle, displayId, bufferSize, ref width, ref height, ref pixels) != 0)
                {
                    continue;
                }
                Mat<byte> mat = Mat<byte>.FromArray(buffer);
                // TODO
            } while (!exit);
            ResetConnectionStatus();
        }

        private void EmulatorProcess_Exited(object sender, EventArgs e)
        {
            ResetConnectionStatus();
            ThreadPool.QueueUserWorkItem(ConnectorMain);
            ThreadPool.QueueUserWorkItem(WorkerMain);
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

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(533, 300);
            this.Text = "FMAC";
            this.Load += MainForm_Load;
            this.MaximizeBox = false;

            this.Controls.Add(btnToggle);
            this.Controls.Add(btnPreview);

            this.btnToggle.AutoSize = true;
            this.btnToggle.Text = "Toggle";
            this.btnToggle.Click += BtnToggle_Click;

            this.btnPreview.AutoSize = true;
            this.btnPreview.Text = "Preview";
            this.btnPreview.Click += BtnPreview_Click;
        }

        #endregion
    }
}
