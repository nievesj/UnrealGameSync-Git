using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels.Tabs.UnrealSync;

public class NotUeProjectViewModel : ObservableObject
{
    public string Title => "Not a Unreal Engine Project";
    public string Message => "This repository does not contain a Unreal Engine project.";
    public string Description =>
        "UnrealSync looks for a .uproject file in the repository root to enable " +
        "the full sync, build, and launch workflow. These files are created by the " +
        "Unreal Editor when you create a new project.";
}
