using System;
using System.Threading;
using System.Threading.Tasks;

using UGSGit.PluginAbstractions;
using UGSGit.Plugins.UnrealSync.ViewModels;

namespace UGSGit.Plugins.UnrealSync;

/// <summary>
/// Commit context menu contributor that adds a "Package" (or "Package ({DisplayName})") item
/// to package the current workspace using a specific package profile.
/// </summary>
public class PackageCommitContributor : ICommitMenuContributor
{
    private readonly FullWorkspaceViewModel _vm;
    private readonly UgsPackageProfile _profile;
    private readonly string _repoPath;
    private readonly bool _hasMultipleProfiles;

    public PackageCommitContributor(FullWorkspaceViewModel vm, UgsPackageProfile profile, string repoPath)
    {
        _vm = vm;
        _profile = profile;
        _repoPath = repoPath;
        _hasMultipleProfiles = vm.PackageProfiles.Count > 1;
    }

    public string Header => _hasMultipleProfiles ? $"Package ({_profile.DisplayName})" : "Package";

    public string? IconResourceKey => "Icons.Archive";

    public string RepoPath => _repoPath;

    public bool RequiresBuildAnnotation => false;

    public bool IsLongRunning => true;

    public bool IsVisible(CommitRef commit)
    {
        return commit.IsCurrentHead
            && _vm.PackageProfiles.Count > 0
            && _vm.PackageProfiles.Contains(_profile);
    }

    public bool IsEnabled(CommitRef commit)
    {
        return IsVisible(commit)
            && !_vm.IsBusy
            && _vm.IsPackageProfileValid(_profile);
    }

    public async Task ExecuteAsync(CommitRef commit, IProgress<string>? log, CancellationToken ct)
    {
        await _vm.PackageAsync(_profile, log, ct);
    }
}
