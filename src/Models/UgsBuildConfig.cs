using System.Text.Json.Serialization;

namespace SourceGit.Models;

/// <summary>
/// Build configuration enum — mirrors UGS's build config dropdown.
/// </summary>
public enum UgsBuildConfig
{
    Debug,
    DebugGame,
    Development,
    Test,
    Shipping
}
