using System.IO;
using System.Text;
using HidSharp;

namespace RoBaKeymapOverlay.Services;

/// <summary>
/// Reads keyboard HID reports from roBa (layer byte + key codes).
/// Uses non-exclusive open so Windows can keep the BLE keyboard.
/// </summary>
public sealed class LayerStatusListener : IDisposable
{
    private readonly string _deviceNameFilter;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private HidDevice? _device;
    private HidStream? _stream;
    private int _currentLayer;
    private byte[] _currentKeyCodes = Array.Empty<byte>();
    private byte _currentModifiers;
    private int _reportLength = 64;
    private long _reportCount;
    private int _deviceAttemptIndex;

    public event EventHandler<int>? LayerChanged;
    public event EventHandler<IReadOnlyCollection<string>>? PressedLabelsChanged;
    public event EventHandler<string>? StatusChanged;

    public LayerStatusListener(string deviceNameFilter = "roBa")
    {
        _deviceNameFilter = deviceNameFilter;
    }

    public int CurrentLayer
    {
        get
        {
            lock (_sync)
            {
                return _currentLayer;
            }
        }
    }

    public bool IsRunning => _worker is { IsCompleted: false };

    public void Start()
    {
        if (_worker is { IsCompleted: false })
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _worker = Task.Run(() => RunLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        CloseDevice();

        try
        {
            _worker?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Ignore cancellation during shutdown.
        }

        _worker = null;
        _cts?.Dispose();
        _cts = null;
    }

    private void RunLoop(CancellationToken cancellationToken)
    {
        RaiseStatus("レイヤー同期: キーボードを待機中…");

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!TryEnsureDevice())
            {
                Thread.Sleep(2000);
                continue;
            }

            try
            {
                ReadReports(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                RaiseStatus($"レイヤー同期: 読み取りエラー ({ex.Message})");
                CloseDevice();
                Thread.Sleep(1500);
            }
        }
    }

    private void ReadReports(CancellationToken cancellationToken)
    {
        var stream = _stream ?? throw new InvalidOperationException("HID stream is not open.");
        var buffer = new byte[_reportLength];

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!stream.CanRead)
            {
                throw new IOException("HID stream is not readable.");
            }

            var read = stream.Read(buffer, 0, buffer.Length);
            if (read < 3 || !HidKeyboardReportParser.TryParse(buffer, read, out var report))
            {
                continue;
            }

            Interlocked.Increment(ref _reportCount);

            var pressedLabels = HidKeyLabelMap
                .GetPressedLabels(report.Modifiers, report.KeyCodes)
                .ToArray();

            int previousLayer;
            byte[] previousKeys;
            byte previousModifiers;

            lock (_sync)
            {
                previousLayer = _currentLayer;
                previousKeys = _currentKeyCodes;
                previousModifiers = _currentModifiers;

                _currentLayer = report.Layer;
                _currentModifiers = report.Modifiers;
                _currentKeyCodes = report.KeyCodes.ToArray();
            }

            var layerChanged = report.Layer != previousLayer;
            var keysChanged = report.Modifiers != previousModifiers
                || !report.KeyCodes.SequenceEqual(previousKeys);

            if (layerChanged)
            {
                LayerChanged?.Invoke(this, report.Layer);
            }

            if (keysChanged)
            {
                PressedLabelsChanged?.Invoke(this, pressedLabels);
            }

            if (layerChanged || keysChanged)
            {
                RaiseStatus(BuildStatus(report, pressedLabels));
            }
        }
    }

    private string BuildStatus(KeyboardHidReport report, IReadOnlyCollection<string> pressedLabels)
    {
        var product = _device?.GetProductName() ?? "HID";
        var keys = pressedLabels.Count == 0
            ? "keys=-"
            : $"keys={string.Join("+", pressedLabels)}";
        var raw = FormatRawReport(report);

        return $"同期: {product} L{report.Layer} {keys} [{raw}] #{_reportCount}";
    }

    private static string FormatRawReport(KeyboardHidReport report)
    {
        var builder = new StringBuilder();
        builder.Append(report.HasReportId ? "id " : "no-id ");
        builder.Append($"mod={report.Modifiers:X2} layer={report.Layer:X2} ");
        builder.Append(HidKeyLabelMap.FormatKeyCodes(report.KeyCodes));
        return builder.ToString().Trim();
    }

    private bool TryEnsureDevice()
    {
        if (_stream is not null)
        {
            return true;
        }

        var candidates = FindKeyboardDevices(_deviceNameFilter).ToList();
        if (candidates.Count == 0)
        {
            var available = DescribeAvailableKeyboards();
            RaiseStatus(string.IsNullOrWhiteSpace(available)
                ? "レイヤー同期: キーボード HID が見つかりません（Raw F14〜で代替可）"
                : $"レイヤー同期: '{_deviceNameFilter}' 未検出 — {available}");
            return false;
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            var index = (_deviceAttemptIndex + i) % candidates.Count;
            var device = candidates[index];
            if (TryOpenShared(device, out var stream))
            {
                _deviceAttemptIndex = index;
                _device = device;
                _stream = stream;
                _reportLength = Math.Max(8, device.GetMaxInputReportLength());
                _reportCount = 0;

                var product = device.GetProductName() ?? "Unknown";
                RaiseStatus($"レイヤー同期: {product} — HID 読み取り開始");
                return true;
            }
        }

        _deviceAttemptIndex = (_deviceAttemptIndex + 1) % Math.Max(1, candidates.Count);
        RaiseStatus("レイヤー同期: HID を開けません（Windows 占有）。Raw F14〜でレイヤー同期します");
        return false;
    }

    private static bool TryOpenShared(HidDevice device, out HidStream? stream)
    {
        stream = null;
        try
        {
            var config = new OpenConfiguration();
            config.SetOption(OpenOption.Exclusive, false);
            stream = device.Open(config);
            stream.ReadTimeout = Timeout.Infinite;
            return true;
        }
        catch
        {
            try
            {
                if (device.TryOpen(out stream) && stream is not null)
                {
                    stream.ReadTimeout = Timeout.Infinite;
                    return true;
                }
            }
            catch
            {
                // ignored
            }
        }

        stream = null;
        return false;
    }

    private static string DescribeAvailableKeyboards()
    {
        var names = DeviceList.Local
            .GetHidDevices()
            .Select(device =>
            {
                try
                {
                    return device.GetProductName();
                }
                catch
                {
                    return null;
                }
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();

        return names.Length == 0
            ? string.Empty
            : $"検出: {string.Join(", ", names)}";
    }

    private static IEnumerable<HidDevice> FindKeyboardDevices(string nameFilter)
    {
        IEnumerable<HidDevice> all;
        try
        {
            all = DeviceList.Local.GetHidDevices().Distinct();
        }
        catch
        {
            yield break;
        }

        var list = all.ToList();
        var named = list.Where(device => MatchesDeviceName(device, nameFilter)).ToList();
        var keyboards = list.Where(IsKeyboardUsage).ToList();

        foreach (var device in named.Concat(keyboards).Distinct())
        {
            yield return device;
        }
    }

    private static bool IsKeyboardUsage(HidDevice device)
    {
        try
        {
            return device.GetMaxInputReportLength() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesDeviceName(HidDevice device, string nameFilter)
    {
        if (string.IsNullOrWhiteSpace(nameFilter))
        {
            return true;
        }

        try
        {
            var product = device.GetProductName() ?? string.Empty;
            var manufacturer = device.GetManufacturer() ?? string.Empty;
            return product.Contains(nameFilter, StringComparison.OrdinalIgnoreCase)
                || manufacturer.Contains(nameFilter, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void CloseDevice()
    {
        _stream?.Dispose();
        _stream = null;
        _device = null;
    }

    private void RaiseStatus(string message) => StatusChanged?.Invoke(this, message);

    public void Dispose()
    {
        Stop();
    }
}
