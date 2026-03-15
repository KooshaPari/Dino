using CommunityToolkit.Mvvm.ComponentModel;

namespace DINOForge.DesktopCompanion.ViewModels
{
    /// <summary>
    /// Root view-model owning the NavigationView selection state.
    /// </summary>
    public sealed partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _selectedPageTag = "Dashboard";

        /// <summary>Display title shown in the window title bar.</summary>
        public string WindowTitle => "DINOForge Desktop Companion";
    }
}
