using System.Text.Json.Serialization;

namespace FMAC;

internal sealed class Settings
{

    public int MultiEmulatorInstanceIndex { get; set; } = 0;
    public int MultiAppInstanceIndex { get; set; } = 0;
    public float FactorCheckPointX { get; set; } = 0.588888f;
    public float FactorCheckPointY { get; set; } = 0.384375f;
    public float FactorRedPointX { get; set; } = 0.248611f;
    public float FactorRedPointY { get; set; } = 0.177343f;
    public float FactorBluePointX { get; set; } = 0.236111f;
    public float FactorBluePointY { get; set; } = 0.397656f;
    public float FactorPaddingX { get; set; } = 0.061111f;
    public float FactorPaddingY { get; set; } = 0.034375f;
    public float FactorPaddingBtnX { get; set; } = 0.194444f;
    public float FactorPaddingBtnY { get; set; } = 0.084375f;
    public float FactorBtn0X { get; set; } = 0.229166f;
    public float FactorBtn0Y { get; set; } = 0.711718f;
    public float FactorBtn1X { get; set; } = 0.120833f;
    public float FactorBtn1Y { get; set; } = 0.796093f;


}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
internal sealed partial class SettingsSerializerContext : JsonSerializerContext { }
