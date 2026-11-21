using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Models.Overlay;
using SS14.Launcher.Models.Themes;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public class OptionsTabViewModel : MainWindowTabViewModel
{
    public DataManager Cfg { get; }
    private readonly IEngineManager _engineManager;
    private readonly ContentManager _contentManager;

    public LanguageSelectorViewModel Language { get; } = new();

    private readonly ThemeManager _themeManager;
    public ThemeOptionViewModel[] Themes { get; }

    public OptionsTabViewModel()
    {
        Cfg = Locator.Current.GetRequiredService<DataManager>();
        _engineManager = Locator.Current.GetRequiredService<IEngineManager>();
        _contentManager = Locator.Current.GetRequiredService<ContentManager>();

        _themeManager = Locator.Current.GetRequiredService<ThemeManager>();
        Themes = ThemePalettes.All
            .Select(t => new ThemeOptionViewModel(_themeManager, t,
                () => OnPropertyChanged(nameof(IsCustomThemeSelected))))
            .ToArray();

        DisableIncompatibleMacOS = OperatingSystem.IsMacOS();
    }

    public bool IsCustomThemeSelected => _themeManager.Current.Id == ThemePalettes.Custom;

    private void UpdateCustom(Func<ThemePalettes.CustomParams, ThemePalettes.CustomParams> change, string prop)
    {
        _themeManager.SetCustom(change(_themeManager.GetCustom()));
        OnPropertyChanged(prop);
    }

    public double CustomBaseHue
    {
        get => _themeManager.GetCustom().BaseHue;
        set => UpdateCustom(c => c with { BaseHue = (int)value }, nameof(CustomBaseHue));
    }

    public double CustomBaseSat
    {
        get => _themeManager.GetCustom().BaseSat;
        set => UpdateCustom(c => c with { BaseSat = (int)value }, nameof(CustomBaseSat));
    }

    public double CustomGradientHue
    {
        get => _themeManager.GetCustom().GradientHue;
        set => UpdateCustom(c => c with { GradientHue = (int)value }, nameof(CustomGradientHue));
    }

    public double CustomGradientSat
    {
        get => _themeManager.GetCustom().GradientSat;
        set => UpdateCustom(c => c with { GradientSat = (int)value }, nameof(CustomGradientSat));
    }

    public double CustomParticleHue
    {
        get => _themeManager.GetCustom().ParticleHue;
        set => UpdateCustom(c => c with { ParticleHue = (int)value }, nameof(CustomParticleHue));
    }

    public double CustomParticleSat
    {
        get => _themeManager.GetCustom().ParticleSat;
        set => UpdateCustom(c => c with { ParticleSat = (int)value }, nameof(CustomParticleSat));
    }

    public void ResetCustomTheme()
    {
        _themeManager.SetCustom(ThemePalettes.CustomParams.Default);
        OnPropertyChanged(nameof(CustomBaseHue));
        OnPropertyChanged(nameof(CustomBaseSat));
        OnPropertyChanged(nameof(CustomGradientHue));
        OnPropertyChanged(nameof(CustomGradientSat));
        OnPropertyChanged(nameof(CustomParticleHue));
        OnPropertyChanged(nameof(CustomParticleSat));
    }

    public bool ThemeParticles
    {
        get => _themeManager.ParticlesEnabled;
        set
        {
            _themeManager.SetParticles(value);
            OnPropertyChanged();
        }
    }
    public bool DisableIncompatibleMacOS { get; }

    public override string Name => LocalizationManager.Instance.GetString("tab-options-title");

    public bool CompatMode
    {
        get => Cfg.GetCVar(CVars.CompatMode);
        set
        {
            Cfg.SetCVar(CVars.CompatMode, value);
            Cfg.CommitConfig();
        }
    }

    public bool LogLauncherVerbose
    {
        get => Cfg.GetCVar(CVars.LogLauncherVerbose);
        set
        {
            Cfg.SetCVar(CVars.LogLauncherVerbose, value);
            Cfg.CommitConfig();
        }
    }

    public bool PrefetchFavoriteServers
    {
        get => Cfg.GetCVar(CVars.PrefetchFavoriteServers);
        set
        {
            Cfg.SetCVar(CVars.PrefetchFavoriteServers, value);
            Cfg.CommitConfig();
            OnPropertyChanged();
        }
    }

    public bool OverrideAssets
    {
        get => Cfg.GetCVar(CVars.OverrideAssets);
        set
        {
            Cfg.SetCVar(CVars.OverrideAssets, value);
            Cfg.CommitConfig();
        }
    }

    public bool StatsOverlaySupported => GameStatsOverlayManager.IsSupported;

    public bool StatsOverlay
    {
        get => Cfg.GetCVar(CVars.StatsOverlay);
        set
        {
            Cfg.SetCVar(CVars.StatsOverlay, value);
            Cfg.CommitConfig();
        }
    }

    private bool _capturingKey;

    public bool IsCapturingKey => _capturingKey;

    /// <summary>Text of the key button: the current key, or a prompt while waiting for a key press.</summary>
    public string StatsOverlayKeyText => _capturingKey
        ? LocalizationManager.Instance.GetString("tab-options-stats-overlay-key-press")
        : OverlayHotkey.Format(Cfg.GetCVar(CVars.StatsOverlayVk), Cfg.GetCVar(CVars.StatsOverlayMods));

    public void BeginKeyCapture()
    {
        _capturingKey = true;
        OnPropertyChanged(nameof(StatsOverlayKeyText));
    }

    public void CancelKeyCapture()
    {
        if (!_capturingKey)
            return;

        _capturingKey = false;
        OnPropertyChanged(nameof(StatsOverlayKeyText));
    }

    /// <summary>Handles a key press while capturing. Returns true if the key press was used.</summary>
    public bool TryCaptureKey(Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifiers)
    {
        if (!_capturingKey)
            return false;

        // Wait for the actual key: pressing only Shift/Ctrl/Alt is the start of a combination.
        if (OverlayHotkey.IsModifierKey(key))
            return true;

        if (key == Avalonia.Input.Key.Escape)
        {
            CancelKeyCapture();
            return true;
        }

        if (OverlayHotkey.TryMapKey(key, out var vk))
        {
            Cfg.SetCVar(CVars.StatsOverlayVk, vk);
            Cfg.SetCVar(CVars.StatsOverlayMods, OverlayHotkey.ModsFromAvalonia(modifiers));
            Cfg.CommitConfig();
            _capturingKey = false;
            OnPropertyChanged(nameof(StatsOverlayKeyText));
        }

        // Unsupported keys are ignored; the prompt stays so the user can press another one.
        return true;
    }

    public void ResetKey()
    {
        Cfg.SetCVar(CVars.StatsOverlayVk, OverlayHotkey.DefaultVk);
        Cfg.SetCVar(CVars.StatsOverlayMods, OverlayHotkey.DefaultMods);
        Cfg.CommitConfig();
        _capturingKey = false;
        OnPropertyChanged(nameof(StatsOverlayKeyText));
    }

    private bool _diagnosticsBusy;
    private string _diagnosticsStatus = "";

    public bool DiagnosticsBusy
    {
        get => _diagnosticsBusy;
        private set => SetProperty(ref _diagnosticsBusy, value);
    }

    public string DiagnosticsStatus
    {
        get => _diagnosticsStatus;
        private set => SetProperty(ref _diagnosticsStatus, value);
    }

    /// <summary>Packs logs, system info and a network check into a zip on the desktop and shows it.</summary>
    public async void CreateDiagnostics()
    {
        if (DiagnosticsBusy)
            return;

        var loc = LocalizationManager.Instance;
        DiagnosticsBusy = true;
        DiagnosticsStatus = loc.GetString("tab-options-diagnostics-working");
        try
        {
            var path = await DiagnosticsExporter.CreateAsync();
            DiagnosticsStatus = loc.GetString("tab-options-diagnostics-done", ("path", path));
            DiagnosticsExporter.Reveal(path);
        }
        catch (Exception e)
        {
            Serilog.Log.Error(e, "Failed to create diagnostics archive");
            DiagnosticsStatus = loc.GetString("tab-options-diagnostics-failed", ("error", e.Message));
        }
        finally
        {
            DiagnosticsBusy = false;
        }
    }

    public void ClearEngines()
    {
        _engineManager.ClearAllEngines();
    }

    public async Task<bool> ClearServerContent()
    {
        return await _contentManager.ClearAll();
    }

    public void OpenLogDirectory()
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = LauncherPaths.DirLogs
        });
    }

    public void OpenAccountSettings()
    {
        Helpers.OpenUri(ConfigConstants.AccountManagementUrl);
    }
}

public sealed class ThemeOptionViewModel : ObservableObject
{
    private readonly ThemeManager _manager;
    private readonly ThemeDefinition _theme;

    private readonly Action _onSelected;

    public ThemeOptionViewModel(ThemeManager manager, ThemeDefinition theme, Action onSelected)
    {
        _manager = manager;
        _theme = theme;
        _onSelected = onSelected;
    }

    public string Name => LocalizationManager.Instance.GetString(_theme.NameKey);

    public bool IsChecked
    {
        get => _manager.Current.Id == _theme.Id;
        set
        {
            // RadioButton also writes false to the previously selected item; only react to selection.
            if (!value || IsChecked)
                return;

            _manager.SetTheme(_theme.Id);
            OnPropertyChanged();
            _onSelected();
        }
    }
}
