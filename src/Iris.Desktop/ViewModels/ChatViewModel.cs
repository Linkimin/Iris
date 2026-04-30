using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Iris.Application.Chat.SendMessage;
using Iris.Desktop.Models;
using Iris.Desktop.Services;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Desktop.ViewModels;

public sealed class ChatViewModel : ViewModelBase
{
    private readonly IIrisApplicationFacade _facade;
    private ConversationId? _conversationId;
    private string _inputText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isSending;

    public ChatViewModel(IIrisApplicationFacade facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSendMessage);
    }

    public ObservableCollection<ChatMessageViewModelItem> Messages { get; } = new();

    public IAsyncRelayCommand SendMessageCommand { get; }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value ?? string.Empty))
            {
                SendMessageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsSending
    {
        get => _isSending;
        private set
        {
            if (SetProperty(ref _isSending, value))
            {
                OnPropertyChanged(nameof(CanEditInput));
                SendMessageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool CanEditInput => !IsSending;

    private bool CanSendMessage()
    {
        return !IsSending && !string.IsNullOrWhiteSpace(InputText);
    }

    private async Task SendMessageAsync()
    {
        if (!CanSendMessage())
        {
            return;
        }

        var message = InputText;
        ErrorMessage = string.Empty;
        IsSending = true;

        try
        {
            Result<SendMessageResult> result = await _facade.SendMessageAsync(
                _conversationId,
                message,
                CancellationToken.None);

            if (result.IsFailure)
            {
                if (result.Error.Code == "chat.conversation_not_found")
                {
                    _conversationId = null;
                }

                ErrorMessage = DesktopErrorMessageMapper.ToUserMessage(result.Error);
                return;
            }

            _conversationId = result.Value.ConversationId;
            Messages.Add(ChatMessageViewModelItem.FromDto(result.Value.UserMessage));
            Messages.Add(ChatMessageViewModelItem.FromDto(result.Value.AssistantMessage));
            InputText = string.Empty;
        }
        finally
        {
            IsSending = false;
        }
    }
}
