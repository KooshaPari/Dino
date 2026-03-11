using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DINOForge.Installer.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace DINOForge.Installer.Views;

/// <summary>
/// Game path detection page — wires the folder picker dialog to the ViewModel.
/// </summary>
public partial class GamePathPage : UserControl
{
    public GamePathPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is GamePathPageViewModel vm)
        {
            vm.BrowseDialogOpener = OpenFolderDialogAsync;
        }
    }

    private async Task<string?> OpenFolderDialogAsync()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        System.Collections.Generic.IReadOnlyList<IStorageFolder> folders =
            await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Diplomacy is Not an Option install folder",
                AllowMultiple = false
            }).ConfigureAwait(false);

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
