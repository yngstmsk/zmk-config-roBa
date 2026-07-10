namespace RoBaKeymapOverlay.Services;

public sealed class KeyboardPressTracker
{
    private readonly object _sync = new();
    private HashSet<string> _hidLabels = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _rawLabels = new(StringComparer.OrdinalIgnoreCase);
    private string _hidStatus = "HID: 未接続";
    private string _rawStatus = "Raw: 待機";

    public event EventHandler<KeyboardPressState>? StateChanged;

    public void SetHidLabels(IReadOnlyCollection<string> labels, string? status = null)
    {
        lock (_sync)
        {
            _hidLabels = labels.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (status is not null)
            {
                _hidStatus = status;
            }
        }

        Publish();
    }

    public void SetHidStatus(string status)
    {
        lock (_sync)
        {
            _hidStatus = status;
        }

        Publish();
    }

    public void SetRawLabels(IReadOnlyCollection<string> labels, string status)
    {
        lock (_sync)
        {
            _rawLabels = labels.ToHashSet(StringComparer.OrdinalIgnoreCase);
            _rawStatus = status;
        }

        Publish();
    }

    private void Publish()
    {
        KeyboardPressState snapshot;
        lock (_sync)
        {
            snapshot = new KeyboardPressState(
                _hidLabels.Union(_rawLabels, StringComparer.OrdinalIgnoreCase).ToArray(),
                $"{_rawStatus} | {_hidStatus}");
        }

        StateChanged?.Invoke(this, snapshot);
    }
}

public sealed record KeyboardPressState(IReadOnlyCollection<string> PressedLabels, string Status);
