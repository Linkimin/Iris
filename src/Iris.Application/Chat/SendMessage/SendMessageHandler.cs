using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Abstractions.Models.Interfaces;
using Iris.Application.Abstractions.Persistence;
using Iris.Application.Chat.Contracts;
using Iris.Application.Chat.Prompting;
using Iris.Domain.Conversations;
using Iris.Shared.Results;
using Iris.Shared.Time.Interfaces;

namespace Iris.Application.Chat.SendMessage;

public sealed class SendMessageHandler
{
    private const int RecentMessageLimit = 20;

    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatModelClient _chatModelClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly SendMessageValidator _validator;
    private readonly IClock _clock;

    public SendMessageHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IChatModelClient chatModelClient,
        PromptBuilder promptBuilder,
        SendMessageValidator validator,
        IClock clock)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _chatModelClient = chatModelClient;
        _promptBuilder = promptBuilder;
        _validator = validator;
        _clock = clock;
    }

    public async Task<Result<SendMessageResult>> HandleAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        Result validation = _validator.Validate(command);

        if (validation.IsFailure)
        {
            return Result<SendMessageResult>.Failure(validation.Error);
        }

        DateTimeOffset now = _clock.UtcNow;
        Result<ConversationLoadResult> conversationResult = await LoadOrCreateConversationAsync(
            command.ConversationId,
            now,
            cancellationToken);

        if (conversationResult.IsFailure)
        {
            return Result<SendMessageResult>.Failure(conversationResult.Error);
        }

        Conversation conversation = conversationResult.Value.Conversation;
        var isNewConversation = conversationResult.Value.IsNewConversation;
        var userContent = MessageContent.Create(command.Message);
        var userMessage = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.User,
            userContent,
            MessageMetadata.Empty,
            now);

        IReadOnlyList<Message> history;

        try
        {
            history = await _messageRepository.ListRecentAsync(
                conversation.Id,
                RecentMessageLimit,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<SendMessageResult>.Failure(Error.Failure(
                "chat.history_load_failed",
                "Conversation history could not be loaded."));
        }

        var chronologicalHistory = history
            .OrderBy(message => message.CreatedAt)
            .ToList();

        Result<PromptBuildResult> promptResult = _promptBuilder.Build(new PromptBuildRequest(chronologicalHistory, userContent));

        if (promptResult.IsFailure)
        {
            return Result<SendMessageResult>.Failure(promptResult.Error);
        }

        Result<ChatModelResponse> modelResponse = await _chatModelClient.SendAsync(
            promptResult.Value.ModelRequest,
            cancellationToken);

        if (modelResponse.IsFailure)
        {
            return Result<SendMessageResult>.Failure(modelResponse.Error);
        }

        if (string.IsNullOrWhiteSpace(modelResponse.Value.Content))
        {
            return Result<SendMessageResult>.Failure(Error.Failure(
                "model.empty_response",
                "The model returned an empty response."));
        }

        DateTimeOffset assistantMessageCreatedAt = _clock.UtcNow;
        var assistantMessage = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.Assistant,
            MessageContent.Create(modelResponse.Value.Content),
            MessageMetadata.Empty,
            assistantMessageCreatedAt);

        conversation.Touch(_clock.UtcNow);

        try
        {
            if (isNewConversation)
            {
                await _conversationRepository.AddAsync(conversation, cancellationToken);
            }
            else
            {
                await _conversationRepository.UpdateAsync(conversation, cancellationToken);
            }

            await _messageRepository.AddAsync(userMessage, cancellationToken);
            await _messageRepository.AddAsync(assistantMessage, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<SendMessageResult>.Failure(Error.Failure(
                "chat.message_save_failed",
                "Conversation messages could not be saved."));
        }

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<SendMessageResult>.Failure(Error.Failure(
                "chat.commit_failed",
                "Conversation changes could not be committed."));
        }

        return Result<SendMessageResult>.Success(new SendMessageResult(
            conversation.Id,
            ToDto(userMessage),
            ToDto(assistantMessage)));
    }

    private async Task<Result<ConversationLoadResult>> LoadOrCreateConversationAsync(
        ConversationId? conversationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (conversationId is null)
        {
            var conversation = Conversation.Create(
                ConversationId.New(),
                title: null,
                ConversationMode.Default,
                now);

            return Result<ConversationLoadResult>.Success(new ConversationLoadResult(conversation, true));
        }

        Conversation? existing;

        try
        {
            existing = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<ConversationLoadResult>.Failure(Error.Failure(
                "chat.conversation_load_failed",
                "Conversation could not be loaded."));
        }

        if (existing is null)
        {
            return Result<ConversationLoadResult>.Failure(Error.Failure(
                "chat.conversation_not_found",
                "Conversation was not found."));
        }

        return Result<ConversationLoadResult>.Success(new ConversationLoadResult(existing, false));
    }

    private static ChatMessageDto ToDto(Message message)
    {
        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.Role,
            message.Content.Value,
            message.CreatedAt);
    }

    private sealed record ConversationLoadResult(Conversation Conversation, bool IsNewConversation);
}
