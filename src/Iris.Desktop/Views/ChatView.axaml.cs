using Avalonia.Controls;
using Avalonia.Input;

using CommunityToolkit.Mvvm.Input;

using Iris.Desktop.ViewModels;

namespace Iris.Desktop.Views
{
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

            e.Handled = true;

            if (DataContext is not ChatViewModel viewModel ||
                viewModel.SendMessageCommand is not IAsyncRelayCommand command ||
                !command.CanExecute(null))
            {
                return;
            }

            await command.ExecuteAsync(null);
        }
    }
}
