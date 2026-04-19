using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace StringifyDesktop.Services;

public sealed class FilePickerService
{
    private Window? window;

    public void Attach(Window window)
    {
        this.window = window;
    }

    public async Task<string?> PickFolderAsync(string startDirectory)
    {
        if (window is null)
        {
            return null;
        }

        IStorageFolder? start = null;
        if (Directory.Exists(startDirectory))
        {
            start = await window.StorageProvider.TryGetFolderFromPathAsync(startDirectory);
        }

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = start,
            Title = "Choose replay folder"
        });

        return folders.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<IReadOnlyList<string>> PickReplayFilesAsync(string startDirectory)
    {
        if (window is null)
        {
            return [];
        }

        IStorageFolder? start = null;
        if (Directory.Exists(startDirectory))
        {
            start = await window.StorageProvider.TryGetFolderFromPathAsync(startDirectory);
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose replay files",
            AllowMultiple = true,
            SuggestedStartLocation = start,
            FileTypeFilter =
            [
                new FilePickerFileType("Replay files")
                {
                    Patterns = ["*.replay"]
                }
            ]
        });

        return files.Select(static file => file.Path.LocalPath).ToArray();
    }
}
