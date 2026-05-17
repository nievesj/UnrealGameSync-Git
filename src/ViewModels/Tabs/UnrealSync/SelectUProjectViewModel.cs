#nullable enable

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

using UGSGit.Models;
using UGSGit.Services;

namespace UGSGit.ViewModels.Tabs.UnrealSync;

/// <summary>
/// ViewModel shown when no .uproject is found or multiple are found.
/// Lets the user browse for a .uproject file or pick from a list of discovered ones.
/// </summary>
public partial class SelectUProjectViewModel : ObservableObject
{
    private readonly string _repoPath;
    private readonly Action<string> _onSelected;

    [ObservableProperty]
    private ObservableCollection<string> _discoveredProjects = new();

    [ObservableProperty]
    private string? _selectedProject;

    [ObservableProperty]
    private string _message = "";

    [ObservableProperty]
    private bool _hasDiscoveredProjects;

    public SelectUProjectViewModel(string repoPath, string[] discoveredFiles, Action<string> onSelected)
    {
        _repoPath = repoPath;
        _onSelected = onSelected;

        foreach (var f in discoveredFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            DiscoveredProjects.Add(f);

        HasDiscoveredProjects = discoveredFiles.Length > 0;
        Message = discoveredFiles.Length == 0
            ? "No .uproject file found in this repository.\nBrowse to select a .uproject file, or ensure a UE project exists."
            : "Multiple .uproject files found. Select one from the list or browse:";
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        // Use Avalonia's top-level storage API for file picker
        var topLevel = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var storage = topLevel.StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select .uproject File",
            AllowMultiple = false,
            SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(_repoPath),
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Unreal Project Files")
                {
                    Patterns = new[] { "*.uproject" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase))
            {
                _onSelected(path);
            }
        }
    }

    [RelayCommand]
    private void UseSelected()
    {
        if (!string.IsNullOrEmpty(SelectedProject))
            _onSelected(SelectedProject);
    }
}
