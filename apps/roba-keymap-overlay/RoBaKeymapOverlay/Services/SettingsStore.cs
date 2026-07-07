using System.IO;
using System.Text.Json;
using System.Windows;
using RoBaKeymapOverlay.Models;

namespace RoBaKeymapOverlay.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;
    private System.Timers.Timer? _debounceTimer;

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "RoBaKeymapOverlay");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveDebounced(AppSettings settings)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();

        _debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
        _debounceTimer.Elapsed += (_, _) =>
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // Ignore persistence errors during shutdown.
            }
        };
        _debounceTimer.Start();
    }

    public void SaveImmediate(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    public static void ClampToWorkingArea(Window window)
    {
        var rect = new Rect(window.Left, window.Top, window.Width, window.Height);
        var workArea = SystemParameters.WorkArea;

        if (workArea.Contains(rect))
        {
            return;
        }

        window.Left = Math.Max(workArea.Left, Math.Min(window.Left, workArea.Right - window.Width));
        window.Top = Math.Max(workArea.Top, Math.Min(window.Top, workArea.Bottom - window.Height));
    }
}
