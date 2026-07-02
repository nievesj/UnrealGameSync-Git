#nullable enable

using System;
using System.Text;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UGSGit.ViewModels;

public class CommitActionProgress : ObservableObject
{
    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public bool IsComplete
    {
        get => _isComplete;
        private set => SetProperty(ref _isComplete, value);
    }

    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public event Action? LogChanged;

    public IProgress<string> LogProgress { get; }

    public CommitActionProgress(string actionName, string shortSha)
    {
        _actionName = actionName;
        _shortSha = shortSha;
        WindowTitle = actionName;
        _cts = new CancellationTokenSource();
        _logBuilder = new StringBuilder();
        _logBuilder.AppendLine($"Starting {_actionName.ToLowerInvariant()} for {_shortSha}...");
        LogText = _logBuilder.ToString();
        IsRunning = true;

        LogProgress = new Progress<string>(msg =>
        {
            _logBuilder.AppendLine(msg);
            LogText = _logBuilder.ToString();
            LogChanged?.Invoke();
        });
    }

    public CancellationToken CancellationToken => _cts.Token;

    public void Cancel()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            AppendLog("Cancellation requested. The action will stop shortly...");
            // Note: IsRunning is set to false by MarkComplete() or MarkError() when the
            // task observes the cancellation. Setting it here would mislead the user into
            // thinking the operation has fully stopped while it may still be cleaning up.
        }
    }

    private void AppendLog(string message)
    {
        _logBuilder.AppendLine(message);
        LogText = _logBuilder.ToString();
        LogChanged?.Invoke();
    }

    public void MarkComplete()
    {
        IsRunning = false;
        IsComplete = true;
    }

    public void MarkError(string message)
    {
        IsRunning = false;
        IsError = true;
        ErrorMessage = message;
        AppendLog($"[ERROR] {_actionName} failed: {message}");
    }

    public void OnWindowClosing()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            AppendLog("Cancellation requested. The action will stop shortly...");
        }
    }

    private string _windowTitle = string.Empty;
    private readonly string _actionName = string.Empty;
    private readonly string _shortSha = string.Empty;
    private CancellationTokenSource _cts = null!;
    private readonly StringBuilder _logBuilder = new();
    private bool _isRunning;
    private bool _isComplete;
    private bool _isError;
    private string _errorMessage = string.Empty;
    private string _logText = string.Empty;
}
