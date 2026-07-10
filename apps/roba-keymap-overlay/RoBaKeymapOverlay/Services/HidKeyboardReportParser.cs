namespace RoBaKeymapOverlay.Services;

public sealed class KeyboardHidReport
{
    public byte Modifiers { get; init; }
    public int Layer { get; init; }
    public byte[] KeyCodes { get; init; } = Array.Empty<byte>();
    public bool HasReportId { get; init; }
}

public static class HidKeyboardReportParser
{
    private const byte KeyboardReportId = 0x01;
    private const int DefaultKeySlotCount = 6;

    public static bool TryParse(byte[] buffer, int length, out KeyboardHidReport report)
    {
        report = new KeyboardHidReport();

        if (length < 3)
        {
            return false;
        }

        int modifierIndex;
        int layerIndex;
        int keysStart;
        var hasReportId = false;

        if (buffer[0] == KeyboardReportId)
        {
            modifierIndex = 1;
            layerIndex = 2;
            keysStart = 3;
            hasReportId = true;
        }
        else
        {
            modifierIndex = 0;
            layerIndex = 1;
            keysStart = 2;
        }

        if (length <= layerIndex)
        {
            return false;
        }

        var keyCount = Math.Min(DefaultKeySlotCount, Math.Max(0, length - keysStart));
        var keyCodes = new byte[keyCount];
        Array.Copy(buffer, keysStart, keyCodes, 0, keyCount);

        report = new KeyboardHidReport
        {
            HasReportId = hasReportId,
            Modifiers = buffer[modifierIndex],
            Layer = buffer[layerIndex],
            KeyCodes = keyCodes
        };

        return true;
    }
}
