using System.Text.Json.Serialization;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Reference to a commit (SHA only — annotators that need file data should
/// query it via host services themselves).
/// </summary>
/// <param name="ShortSha">9-character git commit abbreviation.</param>
/// <param name="IsCurrentHead">True when this commit is the current HEAD of the repository.</param>
public record CommitRef(string ShortSha, [property: JsonIgnore] bool IsCurrentHead = false);
