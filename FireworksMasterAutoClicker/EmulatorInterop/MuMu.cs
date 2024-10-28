using System.Runtime.InteropServices;

namespace FMAC.EmulatorInterop;

internal static partial class MuMu
{

    private const string MuMuExtraLibrary = "external_renderer_ipc";

    /// <summary>
    /// connect to emulator.
    /// </summary>
    /// <param name="path">emulator install path.</param>
    /// <param name="index">multi-instance index num.</param>
    /// <returns>0 when connect success, 0 when fail.</returns>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_connect", SetLastError = false)]
    public static partial int Connect([MarshalAs(UnmanagedType.LPWStr)] string path, int index);

    /// <summary>
    /// disconnect handle
    /// </summary>
    /// <param name="handle">handle to disconnect</param>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_disconnect", SetLastError = false)]
    public static partial void Disconnect(int handle);

    /// <summary>
    /// get pkg display id when 'keep-alive' is on. when 'keep-alive' is off, always return 0 no matter what <paramref name="pkg"/> is.
    /// when pkg close and start again, you should call this function again to get a newer display id.
    /// call this function after the <paramref name="pkg"/> start up.
    /// </summary>
    /// <param name="handle">value returned from <see cref="Connect(string, int)"/></param>
    /// <param name="pkg">pkg name</param>
    /// <param name="appIndex">if <paramref name="pkg"/> is a cloned pkg, <paramref name="appIndex"/> means cloned index, the main clone is 0, the first clone is 1, and so on.</param>
    /// <returns>
    /// lesser than 0 means fail, check if the pkg is started or pkg name is correct.
    /// greater than or equal to 0 means valid display id.
    /// </returns>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_get_display_id", SetLastError = false)]
    public static partial int GetDisplayId(int handle, [MarshalAs(UnmanagedType.LPStr)] string pkg, int appIndex);

    /// <summary>
    /// call this function twice to get valid pixels data.
    /// first you set <paramref name="bufferSize"/> to 0, function will return valid width and height to <paramref name="width"/> and <paramref name="height"/>.
    /// then you set <code>4 * width * height</code> to <paramref name="bufferSize"/>, and call this function again, <paramref name="pixels"/> will contain valid data when function success.
    /// </summary>
    /// <param name="handle">value returned from <see cref="Connect(string, int)"/></param>
    /// <param name="displayId">display id, return value from <see cref="GetDisplayId(int, string, int)"/></param>
    /// <param name="bufferSize">size of <paramref name="pixels"/> data.</param>
    /// <param name="width">valid width.</param>
    /// <param name="height">valid height.</param>
    /// <param name="pixels">valid pixels data.</param>
    /// <returns>equal to 0 when success, greater than 0 when fail.</returns>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_capture_display", SetLastError = false)]
    public static partial int CaptureDisplay(int handle, int displayId, int bufferSize, ref int width, ref int height, ref byte pixels);

    /// <summary>
    /// raise touch down event at pos.
    /// </summary>
    /// <param name="handle">value returned from <see cref="Connect(string, int)"/></param>
    /// <param name="displayId">display id, return value from <see cref="GetDisplayId(int, string, int)"/></param>
    /// <param name="x">x pos.</param>
    /// <param name="y">y pos.</param>
    /// <returns><inheritdoc cref="CaptureDisplay(int, uint, int, ref int, ref int, ref byte)"/></returns>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_input_event_touch_down", SetLastError = false)]
    public static partial int InputEventTouchDown(int handle, int displayId, int x, int y);

    /// <summary>
    /// raise touch up event at pos.
    /// </summary>
    /// <param name="handle">value returned from <see cref="Connect(string, int)"/></param>
    /// <param name="displayId">display id, return value from <see cref="GetDisplayId(int, string, int)"/></param>
    /// <returns><inheritdoc cref="CaptureDisplay(int, uint, int, ref int, ref int, ref byte)"/></returns>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_input_event_touch_up", SetLastError = false)]
    public static partial int InputEventTouchUp(int handle, int displayId);

    /// <summary>
    /// raise touch down event with multi-finger at pos.
    /// </summary>
    /// <param name="handle">value returned from <see cref="Connect(string, int)"/></param>
    /// <param name="displayId">display id, return value from <see cref="GetDisplayId(int, string, int)"/></param>
    /// <param name="fingerId">which finger you press down, range is [1, 10].</param>
    /// <param name="x">x pos.</param>
    /// <param name="y">y pos.</param>
    /// <returns><inheritdoc cref="CaptureDisplay(int, uint, int, ref int, ref int, ref byte)"/></returns>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_input_event_finger_touch_down", SetLastError = false)]
    public static partial int InputEventFingerTouchDown(int handle, int displayId, int fingerId, int x, int y);

    /// <summary>
    /// raise touch up event with multi-finger at pos.
    /// </summary>
    /// <param name="handle">value returned from <see cref="Connect(string, int)"/></param>
    /// <param name="displayId">display id, return value from <see cref="GetDisplayId(int, string, int)"/></param>
    /// <param name="fingerId">which finger you press up, range is [1, 10].</param>
    /// <returns><inheritdoc cref="CaptureDisplay(int, uint, int, ref int, ref int, ref byte)"/></returns>
    [LibraryImport(MuMuExtraLibrary, EntryPoint = "nemu_input_event_finger_touch_up", SetLastError = false)]
    public static partial int InputEventFingerTouchUp(int handle, int displayId, int fingerId);

}
