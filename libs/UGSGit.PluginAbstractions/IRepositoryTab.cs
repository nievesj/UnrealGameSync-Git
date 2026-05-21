using System;

namespace UGSGit.PluginAbstractions
{
    /// <summary>
    /// Contract for a tab contributed by a plugin to the repository tab bar.
    /// Each plugin manifest may create one or more tabs via <see cref="IPluginManifest.CreateTabs"/>.
    /// </summary>
    public interface IRepositoryTab : IDisposable
    {
        /// <summary>Tab title displayed in the tab bar.</summary>
        string Title { get; }

        /// <summary>Tab icon (typically an Avalonia resource key or geometry).</summary>
        object Icon { get; }

        /// <summary>Content displayed in the status/toolbar area below the tab bar.</summary>
        object ToolbarContent { get; }

        /// <summary>Main content area for the tab.</summary>
        object BodyContent { get; }

        /// <summary>Whether the user can close this tab.</summary>
        bool IsClosable { get; }

        /// <summary>Unique identifier for this tab instance, used for state persistence.</summary>
        string TabId { get; }

        /// <summary>Sort position relative to other tabs (0 = first, 100 = after Repository tab).</summary>
        int SortOrder { get; }

        /// <summary>Called when the tab becomes the active tab.</summary>
        void OnActivated();

        /// <summary>Called when the tab is no longer the active tab.</summary>
        void OnDeactivated();
    }
}
