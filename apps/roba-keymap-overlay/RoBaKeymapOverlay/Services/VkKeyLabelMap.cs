namespace RoBaKeymapOverlay.Services;

public static class VkKeyLabelMap
{
    private static readonly Dictionary<int, string> VkToLabel = new()
    {
        [0x41] = "A",
        [0x42] = "B",
        [0x43] = "C",
        [0x44] = "D",
        [0x45] = "E",
        [0x46] = "F",
        [0x47] = "G",
        [0x48] = "H",
        [0x49] = "I",
        [0x4A] = "J",
        [0x4B] = "K",
        [0x4C] = "L",
        [0x4D] = "M",
        [0x4E] = "N",
        [0x4F] = "O",
        [0x50] = "P",
        [0x51] = "Q",
        [0x52] = "R",
        [0x53] = "S",
        [0x54] = "T",
        [0x55] = "U",
        [0x56] = "V",
        [0x57] = "W",
        [0x58] = "X",
        [0x59] = "Y",
        [0x5A] = "Z",
        [0x30] = "0",
        [0x31] = "1",
        [0x32] = "2",
        [0x33] = "3",
        [0x34] = "4",
        [0x35] = "5",
        [0x36] = "6",
        [0x37] = "7",
        [0x38] = "8",
        [0x39] = "9",
        [0x1B] = "ESC",
        [0x09] = "TAB",
        [0x08] = "BSpc",
        [0x0D] = "Enter",
        [0x20] = "Space",
        [0x2E] = "DEL",
        [0x14] = "Caps",
        [0x25] = "←",
        [0x26] = "↑",
        [0x27] = "→",
        [0x28] = "↓",
        [0xBA] = ";",
        [0xBB] = "=",
        [0xBC] = ",",
        [0xBD] = "-",
        [0xBE] = ".",
        [0xBF] = "/",
        [0xC0] = "半/全",
        [0xDB] = "[",
        [0xDC] = "\\",
        [0xDD] = "]",
        [0xDE] = "'",
        [0x6B] = "+",
        [0x6D] = "-",
        [0x6A] = "*",
        [0x6F] = "/",
    };

    private static readonly Dictionary<int, string> ModifierVkToLabel = new()
    {
        [0x10] = "Shift",
        [0xA0] = "Shift",
        [0xA1] = "Shift",
        [0x11] = "Ctrl",
        [0xA2] = "Ctrl",
        [0xA3] = "Ctrl",
        [0x12] = "Alt",
        [0xA4] = "Alt",
        [0xA5] = "Alt",
        [0x5B] = "WIN",
        [0x5C] = "WIN",
    };

    public static string? GetLabel(int virtualKey)
    {
        if (VkToLabel.TryGetValue(virtualKey, out var label))
        {
            return label;
        }

        if (ModifierVkToLabel.TryGetValue(virtualKey, out label))
        {
            return label;
        }

        return null;
    }
}
