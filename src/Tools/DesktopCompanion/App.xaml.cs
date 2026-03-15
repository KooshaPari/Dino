using System;
using DINOForge.DesktopCompanion.Data;
using DINOForge.DesktopCompanion.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace DINOForge.DesktopCompanion
{
    /// <summary>
    /// WinUI 3 application entry point.
    /// Bootstraps the DI container and launches <see cref="MainWindow"/>.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Application-wide DI service provider.</summary>
        public static IServiceProvider Services { get; private set; } = null!;

        private MainWindow? _mainWindow;

        /// <summary>Initialises a new instance of <see cref="App"/>.</summary>
        public App()
        {
            InitializeComponent();
            Services = BuildServiceProvider();
        }

        /// <inheritdoc />
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
        }

        private static IServiceProvider BuildServiceProvider()
        {
            ServiceCollection services = new ServiceCollection();

            // Data layer
            services.AddSingleton<AppConfigService>();
            services.AddSingleton<DisabledPacksService>();
            services.AddSingleton<IPackDataService, FileSystemPackDataService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<PackListViewModel>();
            services.AddTransient<DebugPanelViewModel>();
            services.AddTransient<SettingsViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
