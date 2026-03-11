using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DINOForge.Installer.Services;

namespace DINOForge.Installer.ViewModels;

/// <summary>
/// Represents the wizard page currently displayed.
/// </summary>
public enum WizardPage
{
    /// <summary>Welcome / mode selection screen.</summary>
    Welcome = 0,
    /// <summary>Game path detection screen.</summary>
    GamePath = 1,
    /// <summary>Installation options screen.</summary>
    Options = 2,
    /// <summary>Progress / completion screen.</summary>
    Progress = 3
}

/// <summary>
/// Root ViewModel for the installer main window.
/// Acts as a wizard state machine, hosting the four page ViewModels and
/// managing navigation between them.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly WelcomePageViewModel _welcomeVm;
    private readonly GamePathPageViewModel _gamePathVm;
    private readonly OptionsPageViewModel _optionsVm;
    private readonly ProgressPageViewModel _progressVm;

    [ObservableProperty]
    private WizardPage _currentPage = WizardPage.Welcome;

    [ObservableProperty]
    private ObservableObject _currentPageViewModel;

    /// <summary>
    /// Initializes child ViewModels and wires navigation events.
    /// </summary>
    public MainWindowViewModel()
    {
        _welcomeVm = new WelcomePageViewModel();
        _gamePathVm = new GamePathPageViewModel();
        _optionsVm = new OptionsPageViewModel();
        _progressVm = new ProgressPageViewModel();

        _currentPageViewModel = _welcomeVm;

        // Wire up welcome page navigation
        _welcomeVm.PlayerModeSelected += () =>
        {
            _optionsVm.IsDevMode = false;
            NavigateTo(WizardPage.GamePath);
        };

        _welcomeVm.DevModeSelected += () =>
        {
            _optionsVm.IsDevMode = true;
            NavigateTo(WizardPage.GamePath);
        };

        // Auto-detect on first visit to game path page
        _gamePathVm.AutoDetect();
    }

    /// <summary>
    /// Exposes the Welcome page ViewModel (used by WelcomePage view).
    /// </summary>
    public WelcomePageViewModel WelcomeVm => _welcomeVm;

    /// <summary>
    /// Exposes the GamePath page ViewModel.
    /// </summary>
    public GamePathPageViewModel GamePathVm => _gamePathVm;

    /// <summary>
    /// Exposes the Options page ViewModel.
    /// </summary>
    public OptionsPageViewModel OptionsVm => _optionsVm;

    /// <summary>
    /// Exposes the Progress page ViewModel.
    /// </summary>
    public ProgressPageViewModel ProgressVm => _progressVm;

    /// <summary>
    /// Whether the Back navigation button should be visible.
    /// </summary>
    public bool CanGoBack => CurrentPage > WizardPage.Welcome && CurrentPage != WizardPage.Progress;

    /// <summary>
    /// Whether the Next / Install navigation button should be visible.
    /// </summary>
    public bool CanGoNext => CurrentPage < WizardPage.Progress;

    /// <summary>
    /// Label for the primary action button (changes to "Install" on options page).
    /// </summary>
    public string NextButtonLabel => CurrentPage == WizardPage.Options ? "Install" : "Next";

    /// <summary>
    /// Navigates to the previous wizard page.
    /// </summary>
    [RelayCommand]
    public void GoBack()
    {
        if (CurrentPage > WizardPage.Welcome)
            NavigateTo(CurrentPage - 1);
    }

    /// <summary>
    /// Navigates to the next wizard page, or triggers install on the options page.
    /// </summary>
    [RelayCommand]
    public async System.Threading.Tasks.Task GoNextAsync()
    {
        if (CurrentPage == WizardPage.Options)
        {
            // Kick off install
            NavigateTo(WizardPage.Progress);
            InstallOptions opts = BuildInstallOptions();
            await _progressVm.RunInstallAsync(opts).ConfigureAwait(false);
        }
        else if (CurrentPage < WizardPage.Progress)
        {
            NavigateTo(CurrentPage + 1);
        }
    }

    /// <summary>
    /// Navigates directly to a specific wizard page.
    /// </summary>
    private void NavigateTo(WizardPage page)
    {
        CurrentPage = page;
        CurrentPageViewModel = page switch
        {
            WizardPage.Welcome => _welcomeVm,
            WizardPage.GamePath => _gamePathVm,
            WizardPage.Options => _optionsVm,
            WizardPage.Progress => _progressVm,
            _ => _welcomeVm
        };

        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(NextButtonLabel));
    }

    /// <summary>
    /// Builds <see cref="InstallOptions"/> from the current ViewModel state.
    /// </summary>
    private InstallOptions BuildInstallOptions()
    {
        return new InstallOptions
        {
            GamePath = _gamePathVm.GamePath,
            IsDevMode = _optionsVm.IsDevMode,
            InstallExamplePacks = _optionsVm.InstallExamplePacks,
            CreateDesktopShortcut = _optionsVm.CreateDesktopShortcut,
            InstallSdkHeaders = _optionsVm.InstallSdkHeaders,
            InstallPackCompiler = _optionsVm.InstallPackCompiler,
            InstallSchemas = _optionsVm.InstallSchemas,
            InstallDebugTools = _optionsVm.InstallDebugTools
        };
    }
}
