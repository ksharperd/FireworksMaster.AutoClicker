using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

using FMAC.EmulatorInterop;

using OpenCvSharp;
using OpenCvSharp.Extensions;

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
        private int displayId = 0;
        private int nemuHandle = 0;
        private bool stopCapture = false;
        private readonly Settings settings;

        private const string TARGET_PKG_NAME = "com.hypergryph.skland";

        public MainForm()
        {
            InitializeComponent();

            Application.ApplicationExit += (object? sender, EventArgs e) => { ResetConnectionStatus(); exit = true; };

            previewBox.Size = Size * 8 / 10;
            previewBox.Left = (ClientSize.Width - previewBox.Size.Width) / 2;
            previewBox.Top = 32;

            btnToggle.Size = btnToggle.Size * 2;
            btnPreview.Size = btnToggle.Size;

            var buttonSize = btnPreview.Size;
            var paddingTop = previewBox.Bottom + 25;
            var paddingLeft = (ClientSize.Width - buttonSize.Width * 2) / 3;

            btnToggle.Left = paddingLeft;
            btnToggle.Top = paddingTop;

            btnPreview.Left = paddingLeft + buttonSize.Width + paddingLeft;
            btnPreview.Top = paddingTop;

            settings = new();
            const string JsonSettingsFileName = "settings.json";
            if (File.Exists(JsonSettingsFileName))
            {
                try
                {
                    var loaded = JsonSerializer.Deserialize(File.ReadAllText(JsonSettingsFileName), SettingsSerializerContext.Default.Settings);
                    if (loaded is not null)
                    {
                        settings = loaded;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to deserialize settings: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var jsonString = JsonSerializer.Serialize(settings, SettingsSerializerContext.Default.Settings);
                    if (!string.IsNullOrEmpty(jsonString))
                    {
                        File.WriteAllText(JsonSettingsFileName, jsonString);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to deserialize settings: {ex.Message}");
                }
            }

            ThreadPool.QueueUserWorkItem(ConnectorMain);
            ThreadPool.QueueUserWorkItem(WorkerMain);
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            stopCapture = !stopCapture;
            Log.Debug($"StopCapture: {stopCapture}");
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            var image = GetScreenCapture();
            if (image is null)
            {
                return;
            }
            using var greenPen = new Pen(Color.Green);
            using var redPen = new Pen(Color.Red);
            using var bluePen = new Pen(Color.Blue);
            using var font = new Font("Arial", 12, FontStyle.Bold);

            DrawRectanglesOnImage(image, greenPen.Brush, settings.FactorCheckPointX, settings.FactorCheckPointY, settings.FactorPaddingX, settings.FactorPaddingY, 1, 1);
            DrawRectanglesOnImage(image, redPen.Brush, settings.FactorRedPointX, settings.FactorRedPointY, settings.FactorPaddingX, settings.FactorPaddingY);
            DrawRectanglesOnImage(image, bluePen.Brush, settings.FactorBluePointX, settings.FactorBluePointY, settings.FactorPaddingX, settings.FactorPaddingY);

            DrawButtonsTextOnImage(image, font, greenPen.Brush);

            previewBox.Image = image;
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

                nemuHandle = MuMu.Connect(installDirMuMu, settings.MultiEmulatorInstanceIndex);
            } while (ShouldLoop());
        }

        private void WorkerMain(object? status)
        {
            while (ShouldLoop())
            {
                Thread.Sleep(100);
            }
            Log.Debug($"Successfully create nemu handle, id={nemuHandle}");
            btnPreview.BeginInvoke(btnPreview.PerformClick);
            int mode = 0;
            do
            {
                if (stopCapture)
                {
                    continue;
                }
                var image = GetScreenCapture();
                if (image is null)
                {
                    continue;
                }

                var checkColour = image.GetPixel(PRF(image.Width, settings.FactorCheckPointX), PRF(image.Height, settings.FactorCheckPointY));
                if ((Math.Abs(checkColour.R - 180) >= 15) && (Math.Abs(checkColour.G - 180) >= 15))
                {
                    mode = 0;
                    continue;
                }
                var count = 0;
                if (mode == 0)
                {
                    Log.Debug("Found");
                    Thread.Sleep(2000);

                    image.Dispose();
                    image = GetScreenCapture()!;

                    var redTargets = ProcessRectanglesOnImage(image, settings.FactorRedPointX, settings.FactorRedPointY, settings.FactorPaddingX, settings.FactorPaddingY);
                    var blueTargets = ProcessRectanglesOnImage(image, settings.FactorBluePointX, settings.FactorBluePointY, settings.FactorPaddingX, settings.FactorPaddingY);
                    for (int i = 0; i < redTargets.Count; i++)
                    {
                        var redRect = redTargets[i];
                        var redPixel = image.GetPixel(redRect.Left, redRect.Top);
                        var blueRect = blueTargets[i];
                        var bluePixel = image.GetPixel(blueRect.Left, blueRect.Top);
                        if ((Math.Abs(redPixel.B - 150) < 15) && (Math.Abs(redPixel.G - 150) < 15) && (Math.Abs(bluePixel.B - 150) < 15) && (Math.Abs(bluePixel.G - 150) < 15))
                        {
                            count += 1;
                        }
                    }
                    count = 25 - count;

                    if (count != 0)
                    {
                        Log.Debug($"Entering {count}");
                        var pos = GetButtonsPos(image)[count / 10];
                        RaiseTouchEventOnEmu(pos.Item1, pos.Item2);
                        pos = GetButtonsPos(image)[count % 10];
                        RaiseTouchEventOnEmu(pos.Item1, pos.Item2);
                        btnPreview.BeginInvoke(btnPreview.PerformClick);
                    }

                    mode = 1;

                    image.Dispose();
                }
            } while (!exit);
            ResetConnectionStatus();
        }

        private void EmulatorProcess_Exited(object? sender, EventArgs e)
        {
            ResetConnectionStatus();
            ThreadPool.QueueUserWorkItem(ConnectorMain);
            ThreadPool.QueueUserWorkItem(WorkerMain);
        }

        private Bitmap? GetScreenCapture()
        {
            int width = 0, height = 0;
            displayId = MuMu.GetDisplayId(nemuHandle, TARGET_PKG_NAME, settings.MultiAppInstanceIndex);
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

            return mat.ToBitmap();
        }

        private void RaiseTouchEventOnEmu(int pointX, int pointY)
        {
            MuMu.InputEventTouchDown(nemuHandle, displayId, pointX, pointY);
            Thread.Sleep(20);
            MuMu.InputEventTouchUp(nemuHandle, displayId);
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

        private Dictionary<int, (int, int)> GetButtonsPos(Image image)
        {
            Dictionary<int, (int, int)> buttons = [];
            var result = ProcessRectanglesOnImage(image, settings.FactorBtn0X, settings.FactorBtn0Y, settings.FactorPaddingBtnX, settings.FactorPaddingBtnY, 4, 3);
            InitBtnPos(buttons, 0, result[0]);
            InitBtnPos(buttons, 6, result[2]);
            InitBtnPos(buttons, 7, result[5]);
            InitBtnPos(buttons, 8, result[8]);
            InitBtnPos(buttons, 9, result[11]);
            result = ProcessRectanglesOnImage(image, settings.FactorBtn1X, settings.FactorBtn1Y, settings.FactorPaddingBtnX, settings.FactorPaddingBtnY, 5, 1);
            InitBtnPos(buttons, 1, result[0]);
            InitBtnPos(buttons, 2, result[1]);
            InitBtnPos(buttons, 3, result[2]);
            InitBtnPos(buttons, 4, result[3]);
            InitBtnPos(buttons, 5, result[4]);
            return buttons;
        }

        private void DrawButtonsTextOnImage(Image image, Font font, Brush brush)
        {
            using var g = Graphics.FromImage(image);
            var buttons = GetButtonsPos(image);
            foreach (var (id, pos) in buttons)
            {
                g.DrawString($"Btn-{id}", font, brush, pos.Item1, pos.Item2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InitBtnPos(Dictionary<int, (int, int)> buttons, int id, Rectangle pos)
        {
            buttons.Add(id, (pos.Left, pos.Top));
        }

        private static void DrawRectanglesOnImage(Image image, Brush brush, float factorX, float factorY, float factorPaddingX, float factorPaddingY, int cols = 5, int rows = 5)
        {
            using var g = Graphics.FromImage(image);
            var rectangles = ProcessRectanglesOnImage(image, factorX, factorY, factorPaddingX, factorPaddingY, cols, rows, 10, 10);
            foreach (var rectangle in rectangles)
            {
                g.FillRectangle(brush, rectangle);
            }
        }

        private static List<Rectangle> ProcessRectanglesOnImage(Image image, float factorX, float factorY, float factorPaddingX, float factorPaddingY, int cols = 5, int rows = 5, int rectWidth = 1, int rectHeight = 1)
        {
            var rectangles = new List<Rectangle>(cols * rows);
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var x = (int)Math.Round(image.Width * factorX + image.Width * factorPaddingX * col);
                    var y = (int)Math.Round(image.Height * factorY + image.Height * factorPaddingY * row);
                    var rect = new Rectangle(x, y, rectWidth, rectHeight);
                    rectangles.Add(rect);
                }
            }
            return rectangles;
        }

        private static int PRF(int pos, float factor)
        {
            return (int)Math.Round(pos * factor);
        }
    }
}
