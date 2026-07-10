using System.Runtime.InteropServices;

namespace RoBaKeymapOverlay.Services;

/// <summary>
/// Receives global keyboard input via WM_INPUT.
/// Also derives active layer from F14–F18 held by the roBa firmware fallback.
/// </summary>
public sealed class RawKeyboardInputListener
{
    private const uint RidInput = 0x10000003;
    private const uint RimTypeKeyboard = 1;
    private const ushort RiKeyBreak = 0x01;
    private const uint RidevInputSink = 0x00000100;

    // VK_F14 .. VK_F18 → layer 1 .. 5 (firmware holds F(13+layer))
    private const int VkF14 = 0x7D;
    private const int VkF18 = 0x81;

    private readonly HashSet<string> _pressedLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> _layerIndicatorVks = new();
    private readonly object _sync = new();
    private int _hintLayer;

    public event EventHandler<IReadOnlyCollection<string>>? PressedLabelsChanged;
    public event EventHandler<int>? LayerHintChanged;

    public bool Register(IntPtr hwnd)
    {
        var devices = new[]
        {
            new RawInputDevice(0x01, 0x06, RidevInputSink, hwnd)
        };

        return RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>());
    }

    public bool ProcessInputMessage(IntPtr lParam)
    {
        if (!TryParseKeyboard(lParam, out var virtualKey, out var isKeyUp))
        {
            return false;
        }

        if (IsLayerIndicatorVk(virtualKey))
        {
            UpdateLayerHint(virtualKey, isKeyUp);
            return true;
        }

        var label = VkKeyLabelMap.GetLabel(virtualKey);
        if (label is null)
        {
            return false;
        }

        IReadOnlyCollection<string> snapshot;
        lock (_sync)
        {
            if (isKeyUp)
            {
                _pressedLabels.Remove(label);
            }
            else
            {
                _pressedLabels.Add(label);
            }

            snapshot = MergeWithLayerHoldLabels(_pressedLabels, _hintLayer);
        }

        PressedLabelsChanged?.Invoke(this, snapshot);
        return true;
    }

    private void UpdateLayerHint(int virtualKey, bool isKeyUp)
    {
        int layer;
        IReadOnlyCollection<string> snapshot;

        lock (_sync)
        {
            if (isKeyUp)
            {
                _layerIndicatorVks.Remove(virtualKey);
            }
            else
            {
                _layerIndicatorVks.Add(virtualKey);
            }

            layer = _layerIndicatorVks.Count == 0
                ? 0
                : _layerIndicatorVks.Max(vk => vk - VkF14 + 1);

            layer = Math.Clamp(layer, 0, 5);
            var layerChanged = layer != _hintLayer;
            _hintLayer = layer;
            snapshot = MergeWithLayerHoldLabels(_pressedLabels, _hintLayer);

            if (layerChanged)
            {
                LayerHintChanged?.Invoke(this, layer);
            }
        }

        PressedLabelsChanged?.Invoke(this, snapshot);
    }

    private static bool IsLayerIndicatorVk(int virtualKey) =>
        virtualKey >= VkF14 && virtualKey <= VkF18;

    public static IReadOnlyCollection<string> MergeWithLayerHoldLabels(
        IEnumerable<string> pressed,
        int layer)
    {
        var set = pressed.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var label in GetLayerHoldLabels(layer))
        {
            set.Add(label);
        }

        return set.ToArray();
    }

    public static IReadOnlyList<string> GetLayerHoldLabels(int layer) => layer switch
    {
        1 => ["MO1"],
        2 => ["MO2"],
        3 => ["MO1", "MO2", "MO3"],
        4 => ["FN"],
        5 => [],
        _ => []
    };

    private static bool TryParseKeyboard(IntPtr hRawInput, out int virtualKey, out bool isKeyUp)
    {
        virtualKey = 0;
        isKeyUp = false;

        uint size = 0;
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        GetRawInputData(hRawInput, RidInput, IntPtr.Zero, ref size, headerSize);
        if (size == 0)
        {
            return false;
        }

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetRawInputData(hRawInput, RidInput, buffer, ref size, headerSize) == unchecked((uint)-1))
            {
                return false;
            }

            var type = Marshal.ReadInt32(buffer);
            if (type != RimTypeKeyboard)
            {
                return false;
            }

            var keyboardOffset = (int)headerSize;
            var flags = (ushort)Marshal.ReadInt16(buffer, keyboardOffset + 2);
            virtualKey = Marshal.ReadInt16(buffer, keyboardOffset + 6);
            isKeyUp = (flags & RiKeyBreak) != 0;
            return virtualKey != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;

        public RawInputDevice(ushort usagePage, ushort usage, uint flags, IntPtr target)
        {
            UsagePage = usagePage;
            Usage = usage;
            Flags = flags;
            Target = target;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(
        RawInputDevice[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize,
        uint cbSizeHeader);
}
