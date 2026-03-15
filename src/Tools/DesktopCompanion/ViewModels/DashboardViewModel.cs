using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DINOForge.DesktopCompanion.Data;

namespace DINOForge.DesktopCompanion.ViewModels
{
    /// <summary>
    /// View-model for the Dashboard page — shows a summary of loaded packs and errors.
    /// </summary>
    public sealed partial class DashboardViewModel : ObservableObject
    {
        private readonly IPackDataService _packDataService;
        private readonly AppConfigService _configService;

        [ObservableProperty]
        private int _loadedCount;

        [ObservableProperty]
        private int _errorCount;

        [ObservableProperty]
        private string _statusMessage = "Not loaded";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasErrors;

        [ObservableProperty]
        private string _packsDirectory = "";

        /// <summary>Initializes a new instance of <see cref="DashboardViewModel"/>.</summary>
        public DashboardViewModel(IPackDataService packDataService, AppConfigService configService)
        {
            _packDataService = packDataService ?? throw new ArgumentNullException(nameof(packDataService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        /// <summary>Refreshes pack data from the configured packs directory.</summary>
        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsLoading = true;
            StatusMessage = "Scanning packs…";

            try
            {
                AppConfig config = await _configService.LoadAsync().ConfigureAwait(true);
                PacksDirectory = config.PacksDirectory;

                if (string.IsNullOrEmpty(PacksDirectory))
                {
                    StatusMessage = "Packs directory not configured — go to Settings.";
                    LoadedCount = 0;
                    ErrorCount = 0;
                    HasErrors = false;
                    return;
                }

                LoadResultViewModel result = await _packDataService
                    .LoadPacksAsync(PacksDirectory)
                    .ConfigureAwait(true);

                LoadedCount = result.LoadedCount;
                ErrorCount = result.ErrorCount;
                HasErrors = result.ErrorCount > 0;

                StatusMessage = result.IsSuccess
                    ? $"All {result.LoadedCount} pack(s) loaded OK"
                    : $"{result.LoadedCount} loaded, {result.ErrorCount} error(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                HasErrors = true;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
