namespace Iris.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ChatViewModel chat, AvatarViewModel avatar)
    {
        Chat = chat;
        Avatar = avatar;
    }

    public ChatViewModel Chat { get; }

    public AvatarViewModel Avatar { get; }
}
