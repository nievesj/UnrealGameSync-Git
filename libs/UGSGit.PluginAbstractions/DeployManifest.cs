namespace UGSGit.PluginAbstractions;

/// <summary>
/// Manifest tracking files deployed from an editor binary zip.
/// Stored at &lt;repo&gt;/.unrealsync/Editor.zipmanifest for cleanup before next deploy.
/// </summary>
public record DeployManifest(int Version, string CommitSha, List<DeployManifestFile> Files);

/// <summary>
/// Single file entry in a <see cref="DeployManifest"/>.
/// Used for safe deletion: only delete if size and timestamp match.
/// </summary>
public record DeployManifestFile(string RelativePath, long Length, DateTime LastWriteTimeUtc);