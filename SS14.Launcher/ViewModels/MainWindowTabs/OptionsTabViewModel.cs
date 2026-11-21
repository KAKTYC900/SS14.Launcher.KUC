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
            .Select(t => new ThemeOptionViewModel(_themeManager, t))
            .ToArray();

        DisableIncompatibleMacOS = OperatingSystem.IsMacOS();
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

    public ThemeOptionViewModel(ThemeManager manager, ThemeDefinition theme)
    {
        _manager = manager;
        _theme = theme;
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
        }
    }
}
