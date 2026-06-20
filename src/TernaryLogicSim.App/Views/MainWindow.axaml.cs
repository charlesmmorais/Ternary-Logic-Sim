using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TernaryLogicSim.App.Editor;

namespace TernaryLogicSim.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent(); // gerado pelo compilador XAML do Avalonia
            EditorSurface.OnLibraryChanged = RefreshCustomPalette;
            EditorSurface.LoadDemoHalfAdder();
            RefreshCustomPalette();
        }

        // ---- Paleta de primitivos ----
        private void OnAddChip(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string tag &&
                Enum.TryParse<EditorChipKind>(tag, out var kind))
            {
                EditorSurface.AddChip(kind);
            }
        }

        // ---- Paleta "MEUS CHIPS" (compostos) ----
        private void OnAddComposite(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string name)
                EditorSurface.AddComposite(name);
        }

        private void OnLoadDemo(object sender, RoutedEventArgs e) => EditorSurface.LoadDemoHalfAdder();
        private void OnClear(object sender, RoutedEventArgs e) => EditorSurface.ClearAll();

        // ---- Criar chip composto ----
        private async void OnCreateChip(object sender, RoutedEventArgs e)
        {
            var name = await NameDialog.Show(this, "Nome do novo chip");
            if (string.IsNullOrWhiteSpace(name)) return;

            if (EditorSurface.CreateCompositeFromCurrent(name, out var err))
                StatusText.Text = $"Chip \"{name.Trim()}\" criado! Veja em MEUS CHIPS (à esquerda).";
            else
                StatusText.Text = "Não foi possível criar: " + err;
        }

        // ---- Salvar ----
        private async void OnSave(object sender, RoutedEventArgs e)
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;

            var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Salvar circuito",
                SuggestedFileName = "circuito.tlsim.json",
                DefaultExtension = "json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Ternary-Logic-Sim") { Patterns = new[] { "*.json" } }
                }
            });
            if (file is null) return;

            try
            {
                await using var stream = await file.OpenWriteAsync();
                using var w = new StreamWriter(stream);
                await w.WriteAsync(EditorSurface.ToJson());
                StatusText.Text = $"Salvo: {file.Name}";
            }
            catch (Exception ex) { StatusText.Text = "Erro ao salvar: " + ex.Message; }
        }

        // ---- Abrir ----
        private async void OnOpen(object sender, RoutedEventArgs e)
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;

            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Abrir circuito",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Ternary-Logic-Sim") { Patterns = new[] { "*.json" } }
                }
            });
            if (files.Count == 0) return;

            try
            {
                await using var stream = await files[0].OpenReadAsync();
                using var r = new StreamReader(stream);
                var text = await r.ReadToEndAsync();
                EditorSurface.LoadJson(text);
                StatusText.Text = $"Aberto: {files[0].Name}";
            }
            catch (Exception ex) { StatusText.Text = "Erro ao abrir: " + ex.Message; }
        }

        // ---- Reconstrói a lista de chips do usuário ----
        private void RefreshCustomPalette()
        {
            CustomPalette.Children.Clear();
            int count = 0;
            foreach (var name in EditorSurface.Library.Names)
            {
                var btn = new Button { Content = "🧩 " + name, Tag = name };
                btn.Click += OnAddComposite;
                CustomPalette.Children.Add(btn);
                count++;
            }
            NoCustomHint.IsVisible = count == 0;
        }
    }
}
