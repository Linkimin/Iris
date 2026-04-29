namespace Iris.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ChatViewModel chat)
    {
        Chat = chat;
    }

    public string Greeting { get; } = "Welcome to Avalonia!";

    public ChatViewModel Chat { get; }
}
