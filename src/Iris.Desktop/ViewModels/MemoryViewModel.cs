using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Iris.Application.Memory.Contracts;
using Iris.Desktop.Models;
using Iris.Desktop.Services;
using Iris.Domain.Memories;
using Iris.Shared.Results;

namespace Iris.Desktop.ViewModels;

public sealed class MemoryViewModel : ViewModelBase
{
    private readonly IIrisApplicationFacade _facade;
    private string _newMemoryContent = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public MemoryViewModel(IIrisApplicationFacade facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        RememberCommand = new AsyncRelayCommand(RememberAsync, () => !string.IsNullOrWhiteSpace(NewMemoryContent));
        ForgetCommand = new AsyncRelayCommand<MemoryId>(id => id is not null ? ForgetAsync(id, default) : Task.CompletedTask);
        _ = LoadMemoriesAsync(CancellationToken.None);
    }

    public ObservableCollection<MemoryViewModelItem> Memories { get; } = new();

    public string NewMemoryContent
    {
        get => _newMemoryContent;
        set
        {
            if (SetProperty(ref _newMemoryContent, value))
            {
                RememberCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public IAsyncRelayCommand RememberCommand { get; }

    public IAsyncRelayCommand<MemoryId> ForgetCommand { get; }

    public async Task LoadMemoriesAsync(CancellationToken cancellationToken)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            Result<System.Collections.Generic.IReadOnlyList<MemoryDto>> result =
                await _facade.ListActiveMemoriesAsync(limit: null, cancellationToken);

            if (result.IsSuccess)
            {
                Memories.Clear();

                foreach (MemoryDto dto in result.Value)
                {
                    Memories.Add(new MemoryViewModelItem(
                        dto.Id,
                        dto.Content,
                        MapKindLabel(dto.Kind),
                        MapImportanceLabel(dto.Importance),
                        dto.CreatedAt,
                        dto.UpdatedAt));
                }
            }
            else
            {
                ErrorMessage = result.Error.Message;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ForgetAsync(MemoryId id, CancellationToken cancellationToken)
    {
        try
        {
            Result result = await _facade.ForgetAsync(id, cancellationToken);

            if (result.IsSuccess)
            {
                await LoadMemoriesAsync(cancellationToken);
            }
            else
            {
                ErrorMessage = result.Error.Message;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private async Task RememberAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewMemoryContent))
        {
            return;
        }

        var content = NewMemoryContent;
        NewMemoryContent = string.Empty;
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            Result<Application.Memory.Commands.RememberMemoryResult> result =
                await _facade.RememberAsync(content, kind: null, importance: null, cancellationToken);

            if (result.IsSuccess)
            {
                await LoadMemoriesAsync(cancellationToken);
            }
            else
            {
                ErrorMessage = result.Error.Message;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string MapKindLabel(MemoryKind kind)
    {
        return kind switch
        {
            MemoryKind.Fact => "Факт",
            MemoryKind.Preference => "Предпочтение",
            MemoryKind.Note => "Заметка",
            _ => kind.ToString()
        };
    }

    private static string MapImportanceLabel(MemoryImportance importance)
    {
        return importance switch
        {
            MemoryImportance.Low => "Низкая",
            MemoryImportance.Normal => "Обычная",
            MemoryImportance.High => "Высокая",
            _ => importance.ToString()
        };
    }
}
