using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Interactivity;

namespace UGSGit.Views
{
    public partial class About : ChromelessWindow
    {
        public About()
        {
            CloseOnESC = true;
            InitializeComponent();

            var assembly = Assembly.GetExecutingAssembly();
            var meta = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
            var foundFriendlyVersion = false;
            foreach (var attr in meta)
            {
                if (attr.Key.Equals("BuildDate", StringComparison.OrdinalIgnoreCase) && DateTime.TryParse(attr.Value, out var date))
                {
                    TxtReleaseDate.Text = App.Text("About.ReleaseDate", Models.DateTimeFormat.Format(date, true));
                }
                else if (attr.Key.Equals("FriendlyVersion", StringComparison.OrdinalIgnoreCase) && REG_FRIENDLY_VERSION().IsMatch(attr.Value))
                {
                    foundFriendlyVersion = true;
                    TxtVersion.Text = attr.Value;
                }
            }

            if (!foundFriendlyVersion)
            {
                var ver = assembly.GetName().Version;
                if (ver != null)
                    TxtVersion.Text = $"v{ver.Major}.{ver.Minor:D2}";
            }

            var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationVersion != null)
            {
                var infoVer = informationVersion.InformationalVersion;
                var idx = infoVer.IndexOf('+');
                if (idx > 0 && infoVer.Length > idx + 11)
                {
                    TxtGitSourceRevision.Text = infoVer.Substring(idx + 1, 10);
                    PnlGitSourceRevision.IsVisible = true;
                }
            }

            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            if (copyright != null)
                TxtCopyright.Text = copyright.Copyright;
        }

        private void OnVisitReleaseNotes(object _, RoutedEventArgs e)
        {
            var tag = TxtVersion.Text;
            // Strip dirty flag and commit-count suffix for release link
            // e.g. "v2026.14-64-54a8ae2b-dirty" -> "v2026.14"
            var baseMatch = System.Text.RegularExpressions.Regex.Match(tag, @"^(v\d{4}\.\d{1,2})");
            if (baseMatch.Success)
                tag = baseMatch.Groups[1].Value;
            Native.OS.OpenBrowser($"https://github.com/nievesj/UnrealGameSync-Git/releases/tag/{tag}");
            e.Handled = true;
        }

        private void OnVisitWebsite(object _, RoutedEventArgs e)
        {
            Native.OS.OpenBrowser("https://github.com/nievesj/UnrealGameSync-Git/");
            e.Handled = true;
        }

        private void OnVisitSourceCode(object _, RoutedEventArgs e)
        {
            Native.OS.OpenBrowser("https://github.com/nievesj/UnrealGameSync-Git");
            e.Handled = true;
        }

        [GeneratedRegex(@"^v\d{4}\.\d{1,2}(?:\-\d+\-[0-9a-f]{8})?(?:\-dirty)?$")]
        private static partial Regex REG_FRIENDLY_VERSION();
    }
}
