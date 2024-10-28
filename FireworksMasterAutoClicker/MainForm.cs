using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        private const string TARGET_PKG_NAME = "com.hypergryph.skland";
        private const string DEBUG_PKG_NAME = "com.mumu.launcher";
        private const float FACTOR_CHECKPOINT_X = 0.588888f;
        private const float FACTOR_CHECKPOINT_Y = 0.384375f;
        private const float FACTOR_REDPOINT_X = 0.248611f;
        private const float FACTOR_REDPOINT_Y = 0.177343f;
        private const float FACTOR_BLUEPOINT_X = 0.236111f;
        private const float FACTOR_BLUEPOINT_Y = 0.397656f;
        private const float FACTOR_PADDING_X = 0.061111f;
        private const float FACTOR_PADDING_Y = 0.034375f;
        private const float FACTOR_PADDING_BTN_X = 0.194444f;
        private const float FACTOR_PADDING_BTN_Y = 0.084375f;
        private const float FACTOR_BTN_0_X = 0.229166f;
        private const float FACTOR_BTN_0_Y = 0.711718f;
        private const float FACTOR_BTN_1_X = 0.120833f;
        private const float FACTOR_BTN_1_Y = 0.796093f;

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

            DrawRectanglesOnImage(image, greenPen.Brush, FACTOR_CHECKPOINT_X, FACTOR_CHECKPOINT_Y, cols: 1, rows: 1);
            DrawRectanglesOnImage(image, redPen.Brush, FACTOR_REDPOINT_X, FACTOR_REDPOINT_Y);
            DrawRectanglesOnImage(image, bluePen.Brush, FACTOR_BLUEPOINT_X, FACTOR_BLUEPOINT_Y);

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

                nemuHandle = MuMu.Connect(installDirMuMu, 1);
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

                var checkColour = image.GetPixel(PRF(image.Width, FACTOR_CHECKPOINT_X), PRF(image.Height, FACTOR_CHECKPOINT_Y));
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

                    var redTargets = ProcessRectanglesOnImage(image, FACTOR_REDPOINT_X, FACTOR_REDPOINT_Y);
                    var blueTargets = ProcessRectanglesOnImage(image, FACTOR_BLUEPOINT_X, FACTOR_BLUEPOINT_Y);
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
            displayId = MuMu.GetDisplayId(nemuHandle, TARGET_PKG_NAME, 0);
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

        private static Dictionary<int, (int, int)> GetButtonsPos(Image image)
        {
            Dictionary<int, (int, int)> buttons = [];
            var result = ProcessRectanglesOnImage(image, FACTOR_BTN_0_X, FACTOR_BTN_0_Y, FACTOR_PADDING_BTN_X, FACTOR_PADDING_BTN_Y, 4, 3);
            InitBtnPos(buttons, 0, result[0]);
            InitBtnPos(buttons, 6, result[2]);
            InitBtnPos(buttons, 7, result[5]);
            InitBtnPos(buttons, 8, result[8]);
            InitBtnPos(buttons, 9, result[11]);
            result = ProcessRectanglesOnImage(image, FACTOR_BTN_1_X, FACTOR_BTN_1_Y, FACTOR_PADDING_BTN_X, FACTOR_PADDING_BTN_Y, 5, 1);
            InitBtnPos(buttons, 1, result[0]);
            InitBtnPos(buttons, 2, result[1]);
            InitBtnPos(buttons, 3, result[2]);
            InitBtnPos(buttons, 4, result[3]);
            InitBtnPos(buttons, 5, result[4]);
            return buttons;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InitBtnPos(Dictionary<int, (int, int)> buttons, int id, Rectangle pos)
        {
            buttons.Add(id, (pos.Left, pos.Top));
        }

        private static void DrawButtonsTextOnImage(Image image, Font font, Brush brush)
        {
            using var g = Graphics.FromImage(image);
            var buttons = GetButtonsPos(image);
            foreach (var (id, pos) in buttons)
            {
                g.DrawString($"Btn-{id}", font, brush, pos.Item1, pos.Item2);
            }
        }

        private static void DrawRectanglesOnImage(Image image, Brush brush, float factorX, float factorY, float factorPaddingX = FACTOR_PADDING_X, float factorPaddingY = FACTOR_PADDING_Y, int cols = 5, int rows = 5)
        {
            using var g = Graphics.FromImage(image);
            var rectangles = ProcessRectanglesOnImage(image, factorX, factorY, factorPaddingX, factorPaddingY, cols, rows, 10, 10);
            foreach (var rectangle in rectangles)
            {
                g.FillRectangle(brush, rectangle);
            }
        }

        private static List<Rectangle> ProcessRectanglesOnImage(Image image, float factorX, float factorY, float factorPaddingX = FACTOR_PADDING_X, float factorPaddingY = FACTOR_PADDING_Y, int cols = 5, int rows = 5, int rectWidth = 1, int rectHeight = 1)
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
