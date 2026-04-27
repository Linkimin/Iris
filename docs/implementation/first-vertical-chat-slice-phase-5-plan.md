# First Vertical Chat Slice Phase 5 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire `Iris.Desktop` to the existing Application, Persistence, and ModelGateway layers so the user can send one chat message from Avalonia UI and see a persisted local-model response.

**Architecture:** Phase 5 is host and presentation wiring. `Iris.Desktop` composes dependencies, owns Avalonia views/viewmodels, calls Application through `IrisApplicationFacade`, and displays readable UI state. It must not build prompts, call Ollama directly, use `IrisDbContext` directly, or execute future tools/perception/voice flows.

**Tech Stack:** .NET 10, C#, Avalonia 12, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Configuration.Json, EF Core SQLite, Ollama through `IChatModelClient`, xUnit.

---

## Existing Spec / Design Check

`docs/implementation/first-vertical-chat-slice.md` defines Desktop wiring at a high level:

- `ChatView`;
- `ChatViewModel`;
- `IrisApplicationFacade`;
- `ChatMessageViewModelItem`;
- input box;
- send action;
- loading state;
- readable error;
- no direct UI-to-Ollama or UI-to-DbContext shortcut.

That is enough as a slice-level direction, but not enough for implementation. Phase 5 must close these missing details:

- Desktop host composition and configuration.
- Application DI registration, because `Iris.Application/DependencyInjection.cs` is currently empty.
- SQLite schema initialization for the first Desktop run without letting UI code touch `IrisDbContext`.
- Exact `IrisApplicationFacade` boundary.
- ViewModel state model and duplicate-send behavior.
- How UI errors are mapped without leaking raw exception details.
- Whether user messages are optimistic or only appended after successful persistence.
- How conversation id is retained after the first send.
- How the database and model provider options reach adapters without code hardcoding.
- What testing is realistic before dedicated Desktop test infrastructure exists.
- What the manual smoke path proves.

This document is the executable Phase 5 plan.

## Phase 5 Scope

In scope:

- `Iris.Application` DI registration for the existing send-message use case.
- `Iris.Desktop` project references to host-composed adapters.
- Desktop configuration loading from `appsettings.json` and optional local override.
- Desktop composition root through `Iris.Desktop.DependencyInjection`.
- Persistence-owned database initializer invoked from Desktop startup.
- `IrisApplicationFacade` and `IIrisApplicationFacade`.
- `ChatViewModel` send flow with loading/error/input state.
- `ChatMessageViewModelItem` UI message model.
- `ChatView` and `ChatMessageBubble` minimal usable Avalonia UI.
- `MainWindow` showing the chat surface as the first screen.
- Focused tests for Application DI and ViewModel behavior where practical.
- Build, focused tests, full tests, and manual Desktop smoke instructions.

Out of scope:

- Streaming responses.
- Cancel button.
- Conversation list/sidebar.
- Selecting previous conversations.
- Settings UI.
- Avatar.
- Memory UI.
- Tools.
- Voice.
- Perception.
- API/Worker wiring.
- Model discovery.
- Production database migrations.
- Advanced logging/diagnostics UI.
- Full visual design polish.

## Design Closure For First Vertical Slice

### Desktop Is A Host

`Iris.Desktop` may reference Application and adapters because it is a composition root.

Allowed:

```text
Iris.Desktop -> Iris.Application
Iris.Desktop -> Iris.Persistence
Iris.Desktop -> Iris.ModelGateway
Iris.Desktop -> Iris.Shared
Iris.Desktop -> Iris.Domain
```

Forbidden:

```text
ChatViewModel -> OllamaChatModelClient
ChatViewModel -> IrisDbContext
IrisApplicationFacade -> OllamaChatModelClient
IrisApplicationFacade -> IrisDbContext
ChatView -> SendMessageHandler
ChatView -> repositories
Desktop -> Tools/Voice/Perception/SI runtime for this phase
```

### Runtime Flow

Phase 5 runtime flow:

```text
ChatView
-> ChatViewModel
-> IIrisApplicationFacade
-> IrisApplicationFacade
-> SendMessageHandler
-> PromptBuilder
-> IChatModelClient
-> OllamaChatModelClient
-> IConversationRepository / IMessageRepository / IUnitOfWork
-> SQLite
-> SendMessageResult
-> ChatViewModel
-> ChatView
```

### Message Display Policy

Phase 5 uses non-optimistic UI updates:

- While sending, the input is disabled and loading state is visible.
- On success, append the persisted user message and assistant message returned by Application.
- On model/database failure, do not append a user bubble because Phase 1-4 intentionally do not persist failed message pairs.

This matches the current Application policy: model failure returns a controlled error and saves no message pair.

### Conversation State Policy

`ChatViewModel` holds the active `ConversationId?`.

- Initially null.
- After first successful send, set from `SendMessageResult.ConversationId`.
- Subsequent sends use the same id.
- If Application returns `chat.conversation_not_found`, clear the id and show a readable error. Do not silently create a new conversation in that failure path.

### Configuration Policy

Desktop loads configuration from:

```text
src/Iris.Desktop/appsettings.json
src/Iris.Desktop/appsettings.local.json
```

`appsettings.local.json` is optional and ignored by git.

Configuration values are not read inside ViewModels.

Minimum `appsettings.json`:

```json
{
  "Application": {
    "Chat": {
      "MaxMessageLength": 8000
    }
  },
  "Database": {
    "ConnectionString": "Data Source=iris.db"
  },
  "ModelGateway": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "ChatModel": "llama3.1",
      "TimeoutSeconds": 120
    }
  }
}
```

The model name is configuration, not Application or ViewModel code. Users can override it in `appsettings.local.json`.

### Database Initialization Policy

Phase 5 must support a clean first run.

`Iris.Persistence` owns the EF Core schema operation through a narrow initializer:

```text
Iris.Persistence.Database.IIrisDatabaseInitializer
Iris.Persistence.Database.IrisDatabaseInitializer
```

The initializer uses `IrisDbContext.Database.EnsureCreatedAsync()` for the MVP because Phase 3 explicitly deferred production EF migrations. This keeps EF details inside Persistence while allowing the Desktop host to prepare the adapter before the first message is sent.

Allowed:

```text
App.axaml.cs -> IIrisDatabaseInitializer
IIrisDatabaseInitializer -> IrisDbContext
```

Forbidden:

```text
ChatViewModel -> IIrisDatabaseInitializer
ChatViewModel -> IrisDbContext
IrisApplicationFacade -> IrisDbContext
SendMessageHandler -> IrisDbContext
```

When migrations are introduced later, `IrisDatabaseInitializer` is the replacement point: change its implementation from `EnsureCreatedAsync()` to the approved migration flow without touching Desktop ViewModels or Application handlers.

## Invariants

### Architecture Invariants

- `Iris.Application` remains free of Desktop, Persistence, and ModelGateway references.
- `Iris.Desktop` composes adapters but ViewModels do not know adapter concrete types.
- Desktop startup may invoke the Persistence database initializer; no ViewModel/facade may use `IrisDbContext` or repositories directly.
- Prompt building remains inside Application.
- Persistence remains inside `Iris.Persistence`.
- Ollama HTTP remains inside `Iris.ModelGateway`.
- No Tools, Voice, Perception, API, Worker, or SI runtime code participates in Phase 5 chat flow.

### UI State Invariants

- `Messages` is updated only on the UI thread by `ChatViewModel`.
- `IsSending` is true only while a send operation is in progress.
- Send command cannot execute while `IsSending` is true.
- Send command cannot execute for null/empty/whitespace input.
- Input is cleared only after successful send.
- User-visible errors are controlled strings.
- Raw exception type, stack trace, prompt text, raw provider response, and connection string are not displayed.

### Configuration Invariants

- Database connection string comes from configuration.
- Ollama base URL/model/timeout come from configuration.
- Max message length comes from configuration.
- Missing required configuration fails startup with a clear host composition error.
- `appsettings.local.json` is optional and not committed.

### Persistence Invariants

- Desktop never references `IrisDbContext`.
- Desktop startup references only `IIrisDatabaseInitializer` for schema preparation.
- Desktop never queries repositories directly.
- Message persistence is verified by Application/Persistence through the already implemented handler path.
- History reload after restart is not Phase 5 acceptance because the Application layer does not yet expose a load-history/read-model use case. Phase 5 persists newly sent messages; Phase 6 must either add an Application read path or explicitly keep reload deferred.

## Failure Pattern Matrix

| Failure | Detection | UI behavior | Privacy rule |
| --- | --- | --- | --- |
| Empty input | ViewModel command guard and Application validator | Send disabled; if invoked, show "Type a message first." | No content logging |
| Duplicate send | `IsSending` command guard | Second send blocked | No duplicate message |
| Ollama unavailable | `IChatModelClient` result | Show "I could not reach Ollama. Check that Ollama is running." | Do not show exception text |
| Ollama timeout | `model_gateway.provider_timeout` | Show "The local model took too long to respond." | Do not show prompt |
| Ollama invalid config | ModelGateway option/result error | Show model configuration message | Do not echo config value |
| SQLite schema missing on clean run | Persistence initializer | Create schema before first window send flow | EF details remain in Persistence |
| SQLite initialization failed | Desktop startup initializer | Fail startup with controlled host error; record in log notes during implementation | Do not show connection string |
| Database unavailable/locked | Application persistence error | Show "I could not save the conversation." | Do not show connection string |
| Commit failed | `chat.commit_failed` | Show readable save failure | Do not append message pair |
| Conversation not found | `chat.conversation_not_found` | Show message and clear active id | Do not auto-recreate silently |
| XAML binding error | build/manual smoke | Fix binding before completion | No runtime workaround |
| Missing appsettings | startup composition | fail startup with controlled host error | No secrets |
| Window closes during send | future cancellation hook absent | no cancel button in Phase 5; app shutdown owns process | record cancel UI as deferred if needed |

## File Responsibility Map

Modify:

- `src/Iris.Application/Iris.Application.csproj`  
  Add DI abstractions package if required for `AddIrisApplication`.

- `src/Iris.Application/DependencyInjection.cs`  
  Register `SendMessageOptions`, `SendMessageValidator`, `PromptBuilder`, `SendMessageHandler`, and `IClock`.

- `src/Iris.Persistence/DependencyInjection.cs`  
  Register the Persistence database initializer.

- `src/Iris.Desktop/Iris.Desktop.csproj`  
  Add references to `Iris.Persistence`, `Iris.ModelGateway`, and packages for DI/configuration. Include `appsettings.json` as copied content.

- `.gitignore`  
  Ignore `src/Iris.Desktop/appsettings.local.json`.

- `src/Iris.Desktop/appsettings.json`  
  Add safe local-first default configuration.

- `src/Iris.Desktop/DependencyInjection.cs`  
  Implement Desktop composition root.

- `src/Iris.Desktop/App.axaml.cs`  
  Build configuration and service provider, then create `MainWindow` with its resolved viewmodel.

- `src/Iris.Desktop/Views/MainWindow.axaml`  
  Make Chat the first screen.

- `src/Iris.Desktop/Views/MainWindow.axaml.cs`  
  Accept `MainWindowViewModel` or set `DataContext` through property initialization.

- `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs`  
  Expose `ChatViewModel Chat`.

- `src/Iris.Desktop/ViewModels/ChatViewModel.cs`  
  Implement message collection, input, loading state, error state, and send command.

- `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs`  
  Implement UI message model.

- `src/Iris.Desktop/Services/IrisApplicationFacade.cs`  
  Implement facade over `SendMessageHandler`.

- `src/Iris.Desktop/Views/ChatView.axaml`  
  Implement minimal chat layout.

- `src/Iris.Desktop/Views/ChatView.axaml.cs`  
  Handle Enter vs Shift+Enter keyboard behavior if XAML command binding cannot preserve multiline input.

- `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml`  
  Implement minimal message bubble visual.

Create:

- `src/Iris.Persistence/Database/IIrisDatabaseInitializer.cs`  
  New narrow startup lifecycle contract for database preparation.

- `src/Iris.Persistence/Database/IrisDatabaseInitializer.cs`  
  Own EF Core `EnsureCreatedAsync()` for the MVP.

- `src/Iris.Desktop/Services/IIrisApplicationFacade.cs`  
  Testable Desktop-owned facade contract.

- `src/Iris.Desktop/Services/DesktopErrorMessageMapper.cs`  
  Maps Application/adapter error codes to readable UI text.

- `tests/Iris.Application.Tests/DependencyInjectionTests.cs`  
  Verifies Application DI can resolve `SendMessageHandler` when its adapter abstractions are provided.

- `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs`  
  Verifies ViewModel send success, failure, loading, input clear, and duplicate-send guard with a fake facade.

- `tests/Iris.IntegrationTests/Persistence/IrisDatabaseInitializerTests.cs`  
  Verifies clean SQLite schema creation through the Persistence initializer.

Do not create `Iris.Desktop.Tests` in Phase 5. Record dedicated Desktop test project ownership as technical debt because current test structure does not include it.

## Production Code Shape

### Application DI

`src/Iris.Application/DependencyInjection.cs`:

```csharp
using Iris.Application.Chat.Prompting;
using Iris.Application.Chat.SendMessage;
using Iris.Shared.Time;
using Iris.Shared.Time.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIrisApplication(
        this IServiceCollection services,
        SendMessageOptions sendMessageOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sendMessageOptions);

        if (sendMessageOptions.MaxMessageLength <= 0)
        {
            throw new InvalidOperationException("Chat max message length must be greater than zero.");
        }

        services.AddSingleton(sendMessageOptions);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<SendMessageValidator>();
        services.AddSingleton<PromptBuilder>();
services.AddScoped<SendMessageHandler>();

        return services;
    }
}
```

### Persistence Database Initializer

`src/Iris.Persistence/Database/IIrisDatabaseInitializer.cs`:

```csharp
namespace Iris.Persistence.Database;

public interface IIrisDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
```

`src/Iris.Persistence/Database/IrisDatabaseInitializer.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Database;

public sealed class IrisDatabaseInitializer : IIrisDatabaseInitializer
{
    private readonly IrisDbContext _dbContext;

    public IrisDatabaseInitializer(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
```

Add to `src/Iris.Persistence/DependencyInjection.cs`:

```csharp
services.AddScoped<IIrisDatabaseInitializer, IrisDatabaseInitializer>();
```

### Desktop Facade

`src/Iris.Desktop/Services/IIrisApplicationFacade.cs`:

```csharp
using Iris.Application.Chat.SendMessage;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Desktop.Services;

public interface IIrisApplicationFacade
{
    Task<Result<SendMessageResult>> SendMessageAsync(
        ConversationId? conversationId,
        string message,
        CancellationToken cancellationToken);
}
```

`src/Iris.Desktop/Services/IrisApplicationFacade.cs`:

```csharp
using Iris.Application.Chat.SendMessage;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Desktop.Services;

public sealed class IrisApplicationFacade : IIrisApplicationFacade
{
    private readonly SendMessageHandler _sendMessageHandler;

    public IrisApplicationFacade(SendMessageHandler sendMessageHandler)
    {
        _sendMessageHandler = sendMessageHandler;
    }

    public Task<Result<SendMessageResult>> SendMessageAsync(
        ConversationId? conversationId,
        string message,
        CancellationToken cancellationToken)
    {
        return _sendMessageHandler.HandleAsync(
            new SendMessageCommand(conversationId, message),
            cancellationToken);
    }
}
```

### Error Mapper

`src/Iris.Desktop/Services/DesktopErrorMessageMapper.cs`:

```csharp
using Iris.Shared.Results;

namespace Iris.Desktop.Services;

internal static class DesktopErrorMessageMapper
{
    public static string ToUserMessage(Error error)
    {
        return error.Code switch
        {
            "chat.message_empty" => "Type a message first.",
            "chat.message_too_long" => "This message is too long.",
            "chat.history_load_failed" => "I could not load the conversation history.",
            "chat.conversation_load_failed" => "I could not load this conversation.",
            "chat.conversation_not_found" => "This conversation could not be found.",
            "chat.message_save_failed" or "chat.commit_failed" => "I could not save the conversation.",
            "model.empty_response" or "model_gateway.provider_empty_response" => "The local model returned an empty response.",
            "model_gateway.provider_unavailable" => "I could not reach Ollama. Check that Ollama is running.",
            "model_gateway.provider_timeout" => "The local model took too long to respond.",
            "model_gateway.provider_not_found" => "The configured Ollama model or endpoint was not found.",
            "model_gateway.provider_failure" or "model_gateway.provider_http_error" => "The local model returned an error.",
            "model_gateway.provider_invalid_response" => "The local model returned a response I could not read.",
            "model_gateway.ollama.base_url_required" or
            "model_gateway.ollama.base_url_invalid" or
            "model_gateway.ollama.base_url_scheme_invalid" or
            "model_gateway.ollama.model_required" or
            "model_gateway.ollama.timeout_invalid" => "The local model configuration is incomplete.",
            _ => "I could not send the message."
        };
    }
}
```

### Chat Message UI Model

`src/Iris.Desktop/Models/ChatMessageViewModelItem.cs`:

```csharp
using Iris.Application.Chat.Contracts;
using Iris.Domain.Conversations;

namespace Iris.Desktop.Models;

public sealed record ChatMessageViewModelItem(
    string Id,
    MessageRole Role,
    string Author,
    string Content,
    DateTimeOffset CreatedAt)
{
    public bool IsUser => Role == MessageRole.User;

    public bool IsAssistant => Role == MessageRole.Assistant;

    public static ChatMessageViewModelItem FromDto(ChatMessageDto message)
    {
        return new ChatMessageViewModelItem(
            message.Id.Value.ToString(),
            message.Role,
            message.Role == MessageRole.User ? "You" : "Iris",
            message.Content,
            message.CreatedAt);
    }
}
```

### Chat ViewModel

`src/Iris.Desktop/ViewModels/ChatViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Iris.Desktop.Models;
using Iris.Desktop.Services;
using Iris.Domain.Conversations;

namespace Iris.Desktop.ViewModels;

public sealed partial class ChatViewModel : ViewModelBase
{
    private readonly IIrisApplicationFacade _applicationFacade;
    private ConversationId? _conversationId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string inputText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private bool isSending;

    [ObservableProperty]
    private string? errorMessage;

    public ChatViewModel(IIrisApplicationFacade applicationFacade)
    {
        _applicationFacade = applicationFacade;
    }

    public ObservableCollection<ChatMessageViewModelItem> Messages { get; } = [];

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    private bool CanSendMessage()
    {
        return !IsSending && !string.IsNullOrWhiteSpace(InputText);
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        var message = InputText.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            ErrorMessage = "Type a message first.";
            return;
        }

        IsSending = true;
        ErrorMessage = null;

        try
        {
            var result = await _applicationFacade.SendMessageAsync(
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
        catch (OperationCanceledException)
        {
            ErrorMessage = "Sending was cancelled.";
        }
        finally
        {
            IsSending = false;
        }
    }
}
```

### Desktop Composition

`src/Iris.Desktop/DependencyInjection.cs`:

```csharp
using Iris.Application;
using Iris.Application.Chat.SendMessage;
using Iris.Desktop.Services;
using Iris.Desktop.ViewModels;
using Iris.ModelGateway;
using Iris.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Desktop;

internal static class DependencyInjection
{
    public static IServiceCollection AddIrisDesktop(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var maxMessageLength = configuration.GetValue<int>("Application:Chat:MaxMessageLength");
        if (maxMessageLength <= 0)
        {
            throw new InvalidOperationException("Application:Chat:MaxMessageLength must be greater than zero.");
        }

        services.AddIrisApplication(new SendMessageOptions(maxMessageLength));

        services.AddIrisPersistence(options =>
        {
            options.ConnectionString = GetRequiredString(configuration, "Database:ConnectionString");
        });

        services.AddIrisModelGateway(options =>
        {
            var timeoutSeconds = configuration.GetValue<int>("ModelGateway:Ollama:TimeoutSeconds");
            if (timeoutSeconds <= 0)
            {
                throw new InvalidOperationException("ModelGateway:Ollama:TimeoutSeconds must be greater than zero.");
            }

            options.BaseUrl = GetRequiredString(configuration, "ModelGateway:Ollama:BaseUrl");
            options.ChatModel = GetRequiredString(configuration, "ModelGateway:Ollama:ChatModel");
            options.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        services.AddSingleton<IIrisApplicationFacade, IrisApplicationFacade>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<MainWindowViewModel>();

        return services;
    }

    private static string GetRequiredString(IConfiguration configuration, string key)
    {
        var value = configuration.GetValue<string>(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required.");
        }

        return value;
    }
}
```

### App Startup

`src/Iris.Desktop/App.axaml.cs` should build configuration and service provider:

```csharp
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Iris.Desktop.ViewModels;
using Iris.Desktop.Views;
using Iris.Persistence.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AvaloniaApplication = Avalonia.Application;

namespace Iris.Desktop;

internal sealed class App : AvaloniaApplication
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .Build();

            var services = new ServiceCollection();
            services.AddIrisDesktop(configuration);
            _serviceProvider = services.BuildServiceProvider();

            InitializePersistence(_serviceProvider);

            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void InitializePersistence(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IIrisDatabaseInitializer>();
        initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
```

### Main Window

`src/Iris.Desktop/ViewModels/MainWindowViewModel.cs`:

```csharp
namespace Iris.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ChatViewModel chat)
    {
        Chat = chat;
    }

    public ChatViewModel Chat { get; }
}
```

`src/Iris.Desktop/Views/MainWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Iris.Desktop.ViewModels"
        xmlns:views="using:Iris.Desktop.Views"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DesignWidth="1000"
        d:DesignHeight="700"
        x:Class="Iris.Desktop.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Icon="/Assets/avalonia-logo.ico"
        Title="Iris">
  <views:ChatView DataContext="{Binding Chat}" />
</Window>
```

### Chat UI

`src/Iris.Desktop/Views/ChatView.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Iris.Desktop.ViewModels"
             xmlns:models="using:Iris.Desktop.Models"
             xmlns:chat="using:Iris.Desktop.Controls"
             x:Class="Iris.Desktop.Views.ChatView"
             x:DataType="vm:ChatViewModel">
  <Grid RowDefinitions="Auto,*,Auto" Margin="20" RowSpacing="16">
    <TextBlock Grid.Row="0"
               Text="Iris"
               FontSize="24"
               FontWeight="SemiBold" />

    <Border Grid.Row="1"
            BorderThickness="1"
            CornerRadius="8"
            Padding="12">
      <ScrollViewer VerticalScrollBarVisibility="Auto">
        <ItemsControl ItemsSource="{Binding Messages}">
          <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="models:ChatMessageViewModelItem">
              <chat:ChatMessageBubble Margin="0,0,0,10" />
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </ScrollViewer>
    </Border>

    <Grid Grid.Row="2" RowDefinitions="Auto,Auto,Auto" RowSpacing="8">
      <TextBlock Grid.Row="0"
                 Text="{Binding ErrorMessage}"
                 IsVisible="{Binding HasError}"
                 Foreground="#D96C6C"
                 TextWrapping="Wrap" />

      <TextBlock Grid.Row="1"
                 Text="Thinking..."
                 IsVisible="{Binding IsSending}" />

      <Grid Grid.Row="2" ColumnDefinitions="*,Auto" ColumnSpacing="8">
        <TextBox x:Name="InputTextBox"
                 Grid.Column="0"
                 Text="{Binding InputText, Mode=TwoWay}"
                 AcceptsReturn="True"
                 TextWrapping="Wrap"
                 MinHeight="48"
                 MaxHeight="160"
                 IsEnabled="{Binding !IsSending}"
                 Watermark="Message Iris" />

        <Button Grid.Column="1"
                MinWidth="96"
                Command="{Binding SendMessageCommand}"
                IsEnabled="{Binding !IsSending}">
          Send
        </Button>
      </Grid>
    </Grid>
  </Grid>
</UserControl>
```

If `IsEnabled="{Binding !IsSending}"` does not compile under Avalonia compiled bindings, replace it with viewmodel properties:

```csharp
public bool CanEditInput => !IsSending;

partial void OnIsSendingChanged(bool value)
{
    OnPropertyChanged(nameof(CanEditInput));
}
```

Then bind `IsEnabled="{Binding CanEditInput}"`.

`src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:models="using:Iris.Desktop.Models"
             x:Class="Iris.Desktop.Controls.ChatMessageBubble"
             x:DataType="models:ChatMessageViewModelItem">
  <Border Padding="12"
          CornerRadius="8"
          BorderThickness="1">
    <StackPanel Spacing="4">
      <TextBlock Text="{Binding Author}"
                 FontWeight="SemiBold" />
      <TextBlock Text="{Binding Content}"
                 TextWrapping="Wrap" />
    </StackPanel>
  </Border>
</UserControl>
```

### Enter Key Behavior

Use `ChatView.axaml.cs` for UI-only keyboard handling:

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Iris.Desktop.ViewModels;

namespace Iris.Desktop.Views;

internal partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        InputTextBox.KeyDown += OnInputTextBoxKeyDown;
    }

    private async void OnInputTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        if (DataContext is not ChatViewModel viewModel ||
            viewModel.SendMessageCommand is not IAsyncRelayCommand command ||
            !command.CanExecute(null))
        {
            return;
        }

        e.Handled = true;
        await command.ExecuteAsync(null);
    }
}
```

This code-behind is acceptable because it owns keyboard gesture translation only. It must not call Application, repositories, or model clients.

## Test Plan

### Application DI Tests

Add `tests/Iris.Application.Tests/DependencyInjectionTests.cs`.

Test registration with fakes for external abstractions:

```csharp
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddIrisApplication_RegistersSendMessageHandler()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConversationRepository, FakeConversationRepository>();
        services.AddSingleton<IMessageRepository, FakeMessageRepository>();
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IChatModelClient, FakeChatModelClient>();
        services.AddIrisApplication(new SendMessageOptions(8000));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<SendMessageHandler>());
    }
}
```

Reuse or copy the focused fakes already present in `tests/Iris.Application.Tests/Chat/SendMessage/SendMessageHandlerTests.cs`.

### Persistence Initializer Test

Add `tests/Iris.IntegrationTests/Persistence/IrisDatabaseInitializerTests.cs`.

Required test:

```csharp
[Fact]
public async Task InitializeAsync_CreatesSqliteSchema_ForCleanDatabase()
{
    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
    var connectionString = $"Data Source={databasePath}";

    try
    {
        var services = new ServiceCollection();
        services.AddIrisPersistence(options => options.ConnectionString = connectionString);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var initializer = scope.ServiceProvider.GetRequiredService<IIrisDatabaseInitializer>();
        await initializer.InitializeAsync(CancellationToken.None);

        var dbContext = scope.ServiceProvider.GetRequiredService<IrisDbContext>();
        Assert.True(await dbContext.Database.CanConnectAsync());
        Assert.True(File.Exists(databasePath));
    }
    finally
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
```

This test is allowed to resolve `IrisDbContext` because it lives in Integration tests and verifies Persistence adapter behavior. Production Desktop code must resolve only `IIrisDatabaseInitializer`.

### Desktop ViewModel Tests

Add `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs`.

Required tests:

- `SendMessageCommand_IsDisabled_WhenInputIsEmpty`.
- `SendMessageCommand_AppendsMessagesAndClearsInput_WhenFacadeSucceeds`.
- `SendMessageCommand_ShowsReadableError_WhenFacadeFails`.
- `SendMessageCommand_BlocksDuplicateSend_WhileSending`.
- `SendMessageCommand_ClearsConversationId_WhenConversationNotFound`.

Fake facade shape:

```csharp
private sealed class FakeIrisApplicationFacade : IIrisApplicationFacade
{
    public TaskCompletionSource<Result<SendMessageResult>>? PendingResult { get; set; }

    public int Calls { get; private set; }

    public ConversationId? LastConversationId { get; private set; }

    public Task<Result<SendMessageResult>> SendMessageAsync(
        ConversationId? conversationId,
        string message,
        CancellationToken cancellationToken)
    {
        Calls++;
        LastConversationId = conversationId;

        if (PendingResult is not null)
        {
            return PendingResult.Task;
        }

        return Task.FromResult(CreateSuccessfulResult(conversationId));
    }
}
```

If `Iris.Integration.Tests` cannot reference `Iris.Desktop` cleanly because it is a WinExe Avalonia project, stop implementation and record the need for `Iris.Desktop.Tests` in `.agent/debt_tech_backlog.md`.

## Implementation Tasks

### Task 1: Application DI

**Files:**
- Modify: `src/Iris.Application/Iris.Application.csproj`
- Modify: `src/Iris.Application/DependencyInjection.cs`
- Create: `tests/Iris.Application.Tests/DependencyInjectionTests.cs`

- [ ] Add `Microsoft.Extensions.DependencyInjection.Abstractions` to `Iris.Application`.
- [ ] Replace empty `DependencyInjection.cs` with `AddIrisApplication`.
- [ ] Add Application DI test using fakes.
- [ ] Run `dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj`.
- [ ] Commit with message `feat(application): register chat services`.

### Task 2: Desktop Configuration And Composition

**Files:**
- Modify: `.gitignore`
- Create: `src/Iris.Persistence/Database/IIrisDatabaseInitializer.cs`
- Create: `src/Iris.Persistence/Database/IrisDatabaseInitializer.cs`
- Modify: `src/Iris.Persistence/DependencyInjection.cs`
- Modify: `src/Iris.Desktop/Iris.Desktop.csproj`
- Create: `src/Iris.Desktop/appsettings.json`
- Modify: `src/Iris.Desktop/DependencyInjection.cs`
- Modify: `src/Iris.Desktop/App.axaml.cs`
- Create: `tests/Iris.IntegrationTests/Persistence/IrisDatabaseInitializerTests.cs`

- [ ] Ignore `src/Iris.Desktop/appsettings.local.json`.
- [ ] Add Persistence database initializer contract and implementation.
- [ ] Register `IIrisDatabaseInitializer` in `AddIrisPersistence`.
- [ ] Add Desktop references to `Iris.Persistence` and `Iris.ModelGateway`.
- [ ] Add Desktop package references for `Microsoft.Extensions.Configuration.Json` and `Microsoft.Extensions.DependencyInjection`.
- [ ] Add `appsettings.json` as copied output content.
- [ ] Implement `AddIrisDesktop`.
- [ ] Build service provider in `App.axaml.cs` and invoke the Persistence initializer before showing the main window.
- [ ] Add initializer integration test.
- [ ] Run `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj --filter IrisDatabaseInitializerTests`.
- [ ] Run `dotnet build .\Iris.slnx`.
- [ ] Commit with message `feat(desktop): compose chat dependencies`.

### Task 3: Facade And Error Mapping

**Files:**
- Create: `src/Iris.Desktop/Services/IIrisApplicationFacade.cs`
- Modify: `src/Iris.Desktop/Services/IrisApplicationFacade.cs`
- Create: `src/Iris.Desktop/Services/DesktopErrorMessageMapper.cs`

- [ ] Add `IIrisApplicationFacade`.
- [ ] Implement `IrisApplicationFacade` over `SendMessageHandler`.
- [ ] Add `DesktopErrorMessageMapper`.
- [ ] Run `dotnet build .\Iris.slnx`.
- [ ] Commit with message `feat(desktop): add application facade`.

### Task 4: Chat ViewModel

**Files:**
- Modify: `src/Iris.Desktop/Models/ChatMessageViewModelItem.cs`
- Modify: `src/Iris.Desktop/ViewModels/ChatViewModel.cs`
- Modify: `src/Iris.Desktop/ViewModels/MainWindowViewModel.cs`
- Create: `tests/Iris.IntegrationTests/Desktop/ChatViewModelTests.cs`

- [ ] Implement `ChatMessageViewModelItem`.
- [ ] Implement `ChatViewModel`.
- [ ] Update `MainWindowViewModel`.
- [ ] Add ViewModel tests with fake facade.
- [ ] Run `dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj`.
- [ ] Commit with message `feat(desktop): wire chat viewmodel`.

### Task 5: Avalonia Chat UI

**Files:**
- Modify: `src/Iris.Desktop/Views/MainWindow.axaml`
- Modify: `src/Iris.Desktop/Views/MainWindow.axaml.cs`
- Modify: `src/Iris.Desktop/Views/ChatView.axaml`
- Modify: `src/Iris.Desktop/Views/ChatView.axaml.cs`
- Modify: `src/Iris.Desktop/Controls/Chat/ChatMessageBubble.axaml`

- [ ] Make `MainWindow` render `ChatView`.
- [ ] Implement `ChatView` layout.
- [ ] Implement `ChatMessageBubble`.
- [ ] Add Enter-to-send and Shift+Enter multiline behavior.
- [ ] Run `dotnet build .\Iris.slnx`.
- [ ] Commit with message `feat(desktop): add chat UI`.

### Task 6: Validation And Manual Smoke

**Files:**
- Modify: `.agent/PROJECT_LOG.md`
- Modify: `.agent/overview.md`
- Modify: `.agent/log_notes.md` if any validation issue appears.
- Modify: `.agent/debt_tech_backlog.md` for missing dedicated Desktop test project.

- [ ] Run `dotnet build .\Iris.slnx`.
- [ ] Run `dotnet test .\Iris.slnx`.
- [ ] Run `dotnet list .\src\Iris.Application\Iris.Application.csproj reference`.
- [ ] Run `dotnet list .\src\Iris.Desktop\Iris.Desktop.csproj reference`.
- [ ] Start Ollama locally or intentionally leave it stopped for error smoke.
- [ ] Run `dotnet run --project .\src\Iris.Desktop\Iris.Desktop.csproj`.
- [ ] Manual smoke with Ollama running: send one message and verify user + assistant bubbles appear.
- [ ] Manual smoke with Ollama stopped: verify readable provider error appears.
- [ ] Verify no raw exception text appears in normal UI.
- [ ] Update metadata.

## Validation Commands

Build:

```powershell
dotnet build .\Iris.slnx
```

Expected:

```text
Build succeeded.
```

Focused tests:

```powershell
dotnet test .\tests\Iris.Application.Tests\Iris.Application.Tests.csproj
dotnet test .\tests\Iris.IntegrationTests\Iris.Integration.Tests.csproj
```

Expected:

```text
Failed: 0
```

Full test:

```powershell
dotnet test .\Iris.slnx
```

Expected:

```text
Failed: 0
```

Run Desktop:

```powershell
dotnet run --project .\src\Iris.Desktop\Iris.Desktop.csproj
```

Manual expected:

```text
Desktop window opens.
Chat surface is first screen.
Typing a message and sending does not freeze UI.
Loading state appears.
Successful Ollama response appears as assistant message.
If Ollama is unavailable, readable error appears.
```

## Architecture Checkpoint

Before marking Phase 5 complete, verify:

- [ ] `Iris.Application` references only Domain and Shared.
- [ ] `Iris.Desktop` does not reference `Iris.Api` or `Iris.Worker`.
- [ ] `ChatViewModel` references `IIrisApplicationFacade`, not adapters.
- [ ] `IrisApplicationFacade` references `SendMessageHandler`, not adapters.
- [ ] `App.axaml.cs` references `IIrisDatabaseInitializer`, not `IrisDbContext`.
- [ ] `ChatView` code-behind only handles UI gestures.
- [ ] `ChatView` and `ChatViewModel` do not build prompts.
- [ ] No Tools/Voice/Perception/SI runtime wiring was added.
- [ ] No raw exception text is shown in UI.

## Acceptance Criteria

- `Iris.Desktop` starts.
- Clean first run creates the SQLite schema through the Persistence initializer.
- Chat view is the first screen.
- User can type a message.
- Send button calls `ChatViewModel.SendMessageCommand`.
- Enter sends message; Shift+Enter inserts newline.
- Send is blocked while loading.
- Empty input cannot send.
- UI does not freeze during model call.
- Successful response appends user and assistant messages.
- Active conversation id is retained after first successful send.
- Ollama unavailable displays readable error.
- Database/model/config errors display readable error.
- Desktop does not call Ollama or `IrisDbContext` directly.
- Build and tests pass.
- Manual smoke results are recorded in `.agent/PROJECT_LOG.md`.

## Known Risks

- Compiled binding failures from mismatched `x:DataType`.
- `MessageId` value API mismatch in `ChatMessageViewModelItem.FromDto`.
- `IsEnabled="{Binding !IsSending}"` may not compile; use `CanEditInput` if needed.
- `appsettings.json` may not copy to output unless Desktop csproj explicitly includes it.
- Persistence initializer may fail on locked/unwritable database path; treat this as a startup/configuration failure, not as a ViewModel concern.
- SQLite file location may surprise users because Phase 5 uses configured relative path.
- Ollama model name may not exist locally; UI must show readable provider error.
- Manual history reload after restart is not fully supported unless a load-history Application use case exists.

## Deferred Items

These are not Phase 5 work:

- Conversation list and reload UI.
- Dedicated `Iris.Desktop.Tests` project.
- Cancel send button.
- Streaming response.
- Settings UI for model/database.
- App data directory provider.
- Avatar.
- Memory UI.
- Tools/permissions UI.
- Voice/perception controls.

## Metadata Updates Required After Implementation

Append `.agent/PROJECT_LOG.md`:

```md
## 2026-04-27 — Phase 5 Desktop chat wiring

### Changed
- Wired Desktop chat UI through `IrisApplicationFacade`.
- Added Desktop configuration and host composition.
- Added Persistence-owned SQLite initialization for clean Desktop first run.
- Added loading/error/input state for chat.
- Added focused Application DI and Desktop ViewModel tests.

### Files
- src/Iris.Application/DependencyInjection.cs
- src/Iris.Desktop/...
- tests/Iris.Application.Tests/...
- tests/Iris.IntegrationTests/Desktop/...

### Validation
- `dotnet build .\Iris.slnx`: ...
- `dotnet test .\Iris.slnx`: ...
- Manual Desktop smoke: ...

### Next
- Phase 6 end-to-end stabilization.
```

Update `.agent/overview.md`:

```md
Current phase: Phase 5 complete.
Current implementation target: First vertical chat slice stabilization.
Current working status: Desktop can send one chat message through Application to Ollama and SQLite.
Next immediate step: Phase 6 end-to-end stabilization.
Known blockers: ...
```

Append `.agent/debt_tech_backlog.md`:

```md
## Debt: Missing Iris.Desktop.Tests project

### Area
Tests / Iris.Desktop

### Problem
Phase 5 ViewModel tests are planned under `Iris.Integration.Tests` because the repository does not currently have a dedicated Desktop test project.

### Risk
Desktop presentation logic coverage will be mixed with integration tests as Desktop grows.

### Proposed fix
Create `tests/Iris.Desktop.Tests` when Desktop UI/ViewModel behavior expands beyond the first chat screen. Move `ChatViewModelTests` and future Desktop-only ViewModel tests there.

### Priority
Medium
```

Append `.agent/log_notes.md` for any build, XAML, DI, configuration, or manual smoke failure discovered during implementation.

## Implementation Mode

Recommended execution mode:

```text
Subagent-driven development
```

Suggested split:

- Worker 1: Application DI and Desktop composition/configuration.
- Worker 2: Facade, ViewModel, UI message model, and ViewModel tests.
- Worker 3: Avalonia XAML and manual UI smoke checklist.
- Main agent: integration, build/test, dependency audit, metadata, and final review.

Workers must not edit the same files concurrently. UI worker owns XAML files; ViewModel worker owns ViewModels/Models/Services; composition worker owns csproj/appsettings/DI/startup.
