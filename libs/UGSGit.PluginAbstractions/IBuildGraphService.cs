using System;
using System.Threading;
using System.Threading.Tasks;

namespace UGSGit.PluginAbstractions;

/// <summary>
/// Abstraction for BuildGraph-based staging and packaging operations in
/// an Unreal Engine workspace.
/// Host implementation delegates to UGSGit.Services.BuildGraphService, which
/// drives AutomationTool (UAT) BuildGraph scripts.
/// </summary>
public interface IBuildGraphService
{
    /// <summary>
    /// Stage build outputs by running a BuildGraph node, producing a
    /// staged directory ready for packaging.
    /// </summary>
    /// <param name="editorTarget">The editor target name (e.g., "UnrealEditor", "UE5Editor").</param>
    /// <param name="platform">Target platform for staging (e.g., "Win64", "Android", "IOS").</param>
    /// <param name="configuration">Build configuration (e.g., "Development", "Shipping", "DebugGame").</param>
    /// <param name="includePdb">Whether to include program database (.pdb) symbol files in the staged output.</param>
    /// <param name="log">Progress reporter for streaming BuildGraph log output.</param>
    /// <param name="ct">Cancellation token to abort staging early.</param>
    /// <param name="timeout">Optional maximum duration for the staging operation; null means no timeout.</param>
    /// <param name="buildGraphScript">Custom BuildGraph script path relative to engine root; null/empty falls back to "Engine/Build/Graph/Examples/BuildEditorAndTools.xml".</param>
    /// <param name="buildGraphTarget">Custom BuildGraph target name; null/empty falls back to "Copy to Staging Directory".</param>
    /// <param name="setArgsTemplate">Template for -set: arguments with {UbtTarget}, {ProjectPath}, {ShortSha}, {ProjectName} expansion.</param>
    /// <returns>A <see cref="StageResult"/> describing the staging outcome and the path to staged output on success.</returns>
    Task<StageResult> StageAsync(
        string editorTarget,
        string platform,
        string configuration,
        bool includePdb,
        IProgress<string> log,
        CancellationToken ct = default,
        TimeSpan? timeout = null,
        string? buildGraphScript = null,
        string? buildGraphTarget = null,
        string? setArgsTemplate = null);

    /// <summary>
    /// Create a compressed archive (.zip) from a previously staged output directory.
    /// </summary>
    /// <param name="stagingDir">Absolute path to the staged directory produced by <see cref="StageAsync"/>.</param>
    /// <param name="outputPath">Desired path for the resulting .zip file.</param>
    /// <param name="excludePdb">Whether to exclude program database (.pdb) files from the archive.</param>
    /// <param name="log">Progress reporter for streaming compression progress.</param>
    /// <param name="ct">Cancellation token to abort archiving early.</param>
    /// <returns>The absolute path to the created .zip file.</returns>
    Task<string> CreateZipAsync(
        string stagingDir,
        string outputPath,
        bool excludePdb,
        IProgress<string> log,
        CancellationToken ct = default);
}
