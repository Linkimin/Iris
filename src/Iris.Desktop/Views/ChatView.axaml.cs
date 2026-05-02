using Avalonia.Controls;

namespace Iris.Desktop.Views
{
    internal partial class ChatView : UserControl
    {
        // Enter-to-send is wired declaratively via TextBox.KeyBindings in
        // ChatView.axaml. Shift+Enter still inserts a newline because the
        // KeyBinding gesture is bare "Enter" (no modifiers). KeyBinding fires
        // before the TextBox processes Enter as a newline, which is the
        // reliable behavior in Avalonia 12 (a code-behind KeyDown handler
        // attached via += subscribes only to the bubbling phase and is
        // bypassed when the TextBox handles the key first).
        public ChatView()
        {
            InitializeComponent();
        }
    }
}
