// =============================================================================
//  NameDialog.cs  —  Pequeno diálogo modal para digitar um nome
// =============================================================================
//  Construído em código (sem XAML) para ficar autocontido. Usado ao criar um
//  chip composto ("Criar chip…"). Devolve o texto digitado, ou null se cancelar.
// =============================================================================

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace TernaryLogicSim.App.Views
{
    public sealed class NameDialog : Window
    {
        private readonly TextBox _box;

        public NameDialog(string title)
        {
            Title = title;
            Width = 380;
            Height = 150;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _box = new TextBox { Watermark = "ex.: MeioSomador", Margin = new Thickness(12, 12, 12, 6) };

            var ok = new Button { Content = "OK", IsDefault = true, Width = 90 };
            var cancel = new Button { Content = "Cancelar", IsCancel = true, Width = 90 };
            ok.Click += (_, __) => Close(_box.Text);
            cancel.Click += (_, __) => Close(null);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(12, 0, 12, 12)
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var root = new DockPanel();
            DockPanel.SetDock(buttons, Dock.Bottom);
            root.Children.Add(buttons);
            root.Children.Add(_box);
            Content = root;
        }

        public static Task<string> Show(Window owner, string title) =>
            new NameDialog(title).ShowDialog<string>(owner);
    }
}
