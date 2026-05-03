namespace Iris.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ChatViewModel chat, AvatarViewModel avatar, MemoryViewModel memory)
    {
        Chat = chat;
        Avatar = avatar;
        Memory = memory;
    }

    public ChatViewModel Chat { get; }

    public AvatarViewModel Avatar { get; }

    public MemoryViewModel Memory { get; }
}
