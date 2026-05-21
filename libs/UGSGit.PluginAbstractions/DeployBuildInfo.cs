namespace UGSGit.PluginAbstractions;

/// <summary>
/// Information about an available editor build on the network share.
/// Parsed from zip filenames following the pattern: {ProjectName}Editor-{shortSha}.zip
/// </summary>
public record DeployBuildInfo(string ShortSha, string ZipPath, long FileSize, DateTime LastModified);