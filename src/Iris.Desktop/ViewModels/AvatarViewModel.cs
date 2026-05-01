using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using Iris.Desktop.Models;

namespace Iris.Desktop.ViewModels;

public sealed partial class AvatarViewModel : ViewModelBase, IDisposable
{
    private readonly ChatViewModel _chatViewModel;
    private readonly AvatarOptions _options;
    private AvatarState _state;
    private CancellationTokenSource? _successCts;
    private bool _disposed;

    public AvatarViewModel(ChatViewModel chatViewModel, AvatarOptions options)
    {
        _chatViewModel = chatViewModel ?? throw new ArgumentNullException(nameof(chatViewModel));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _state = _options.Enabled ? AvatarState.Idle : AvatarState.Hidden;

        _chatViewModel.PropertyChanged += OnChatPropertyChanged;
        _chatViewModel.Messages.CollectionChanged += OnMessagesChanged;
    }

    public AvatarState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public AvatarSize Size => _options.Size;

    public AvatarPosition Position => _options.Position;

    /// <summary>
    /// Test-only hook that exposes whether a Success-timer cancellation token
    /// is currently live (assigned and not cancelled). Used by integration tests
    /// to assert FR-015 (timer cancellation invariant). Not for production use.
    /// </summary>
    internal bool HasActiveSuccessTimer => _successCts is { IsCancellationRequested: false };

    private void OnChatPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (e.PropertyName == nameof(ChatViewModel.IsSending)
            || e.PropertyName == nameof(ChatViewModel.HasError))
        {
            ComputeState();
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is null)
        {
            return;
        }

        foreach (var item in e.NewItems)
        {
            if (item is ChatMessageViewModelItem message
                && message.IsAssistant
                && !_chatViewModel.HasError)
            {
                State = AvatarState.Success;
                StartSuccessTimer();
                break;
            }
        }
    }

    private void StartSuccessTimer()
    {
        CancelSuccessTimer();
        var delayMs = (int)(_options.SuccessDisplayDurationSeconds * 1000);

        var cts = new CancellationTokenSource();
        _successCts = cts;
        CancellationToken token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_disposed || token.IsCancellationRequested)
            {
                return;
            }

            if (_state != AvatarState.Success)
            {
                return;
            }

            // Marshal Idle assignment back to the UI thread when an Avalonia
            // application is running. In headless test mode (no live Avalonia
            // app, no dispatcher pump) fall back to a direct assignment — the
            // _disposed and token guards above keep this safe against stale
            // continuations.
            if (Avalonia.Application.Current is not null)
            {
                try
                {
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            if (_disposed || token.IsCancellationRequested)
                            {
                                return;
                            }

                            if (_state == AvatarState.Success)
                            {
                                _state = AvatarState.Idle;
                                OnPropertyChanged(nameof(State));
                            }
                        },
                        DispatcherPriority.Background);
                    return;
                }
                catch (InvalidOperationException)
                {
                    // Dispatcher unavailable despite Application.Current being
                    // present (e.g. early shutdown). Fall through to direct path.
                }
            }

            if (_state == AvatarState.Success)
            {
                _state = AvatarState.Idle;
                OnPropertyChanged(nameof(State));
            }
        });
    }

    private void ComputeState()
    {
        if (!_options.Enabled)
        {
            CancelSuccessTimer();
            State = AvatarState.Hidden;
            return;
        }

        if (_chatViewModel.IsSending)
        {
            CancelSuccessTimer();
            State = AvatarState.Thinking;
            return;
        }

        if (!_chatViewModel.IsSending && _chatViewModel.HasError)
        {
            CancelSuccessTimer();
            State = AvatarState.Error;
            return;
        }

        // Otherwise leave the current state unchanged
        // (Idle or Success during active timer)
    }

    private void CancelSuccessTimer()
    {
        _successCts?.Cancel();
        _successCts?.Dispose();
        _successCts = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _chatViewModel.PropertyChanged -= OnChatPropertyChanged;
        _chatViewModel.Messages.CollectionChanged -= OnMessagesChanged;

        CancelSuccessTimer();
    }
}
