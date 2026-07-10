namespace RoBaKeymapOverlay.Services;

public static class HidKeyLabelMap
{
    private static readonly Dictionary<byte, string> UsageToLabel = new()
    {
        [0x04] = "A",
        [0x05] = "B",
        [0x06] = "C",
        [0x07] = "D",
        [0x08] = "E",
        [0x09] = "F",
        [0x0A] = "G",
        [0x0B] = "H",
        [0x0C] = "I",
        [0x0D] = "J",
        [0x0E] = "K",
        [0x0F] = "L",
        [0x10] = "M",
        [0x11] = "N",
        [0x12] = "O",
        [0x13] = "P",
        [0x14] = "Q",
        [0x15] = "R",
        [0x16] = "S",
        [0x17] = "T",
        [0x18] = "U",
        [0x19] = "V",
        [0x1A] = "W",
        [0x1B] = "X",
        [0x1C] = "Y",
        [0x1D] = "Z",
        [0x1E] = "1",
        [0x1F] = "2",
        [0x20] = "3",
        [0x21] = "4",
        [0x22] = "5",
        [0x23] = "6",
        [0x24] = "7",
        [0x25] = "8",
        [0x26] = "9",
        [0x27] = "0",
        [0x28] = "Enter",
        [0x29] = "ESC",
        [0x2A] = "BSpc",
        [0x2B] = "TAB",
        [0x2C] = "Space",
        [0x2D] = "-",
        [0x2E] = "=",
        [0x2F] = "[",
        [0x30] = "]",
        [0x31] = "\\",
        [0x33] = ";",
        [0x34] = "'",
        [0x35] = "半/全",
        [0x36] = ",",
        [0x37] = ".",
        [0x38] = "/",
        [0x39] = "Caps",
        [0x4C] = "DEL",
        [0x4F] = "→",
        [0x50] = "←",
        [0x51] = "↓",
        [0x52] = "↑",
    };

    private static readonly (byte Mask, string Label)[] ModifierBits =
    [
        (0x01, "Ctrl"),
        (0x02, "Shift"),
        (0x04, "Alt"),
        (0x08, "Ctrl"),
        (0x10, "Shift"),
        (0x20, "Alt"),
        (0x40, "Ctrl"),
        (0x80, "Shift"),
    ];

    public static IEnumerable<string> GetPressedLabels(byte modifiers, IEnumerable<byte> keyCodes)
    {
        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (mask, label) in ModifierBits)
        {
            if ((modifiers & mask) != 0)
            {
                labels.Add(label);
            }
        }

        foreach (var code in keyCodes)
        {
            if (code == 0)
            {
                continue;
            }

            if (UsageToLabel.TryGetValue(code, out var label))
            {
                labels.Add(label);
            }
        }

        return labels;
    }

    public static string FormatKeyCodes(IEnumerable<byte> keyCodes)
    {
        var codes = keyCodes.Where(code => code != 0).Select(code => $"{code:X2}").ToArray();
        return codes.Length == 0 ? "-" : string.Join(",", codes);
    }
}
