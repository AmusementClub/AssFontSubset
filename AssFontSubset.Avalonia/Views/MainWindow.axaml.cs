using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssFontSubset.Core;
using System;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Threading.Tasks;
using MsBox.Avalonia.Base;
using System.ComponentModel;
using AssFontSubset.Avalonia.ViewModels;
using I18nResources = AssFontSubset.Avalonia.I18n.Resources;

namespace AssFontSubset.Avalonia.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            AddHandler(DragDrop.DropEvent, Drop_Files);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            AssFileList.ItemsSource = null;
            FontFolder.Text = string.Empty;
            OutputFolder.Text = string.Empty;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            var sourceHanEllipsis = this.FindControl<CheckBox>("SourceHanEllipsis")!.IsChecked!.Value;
            var debugMode = this.FindControl<CheckBox>("Debug")!.IsChecked!.Value;
            var useHbSubset = this.FindControl<CheckBox>("UseHbSubset")!.IsChecked!.Value;

            if (AssFileList.Items.Count == 0)
            {
                await ShowMessageBox("Error", I18nResources.ErrorNoAssFile);
                return;
            }
            var path = new FileInfo[AssFileList.Items.Count];
            for (int i = 0; i < path.Length; i++)
            {
                path[i] = new FileInfo((string)AssFileList.Items.GetAt(i)!);
            }
            var fontPath = new DirectoryInfo(FontFolder.Text!);
            var outputPath = new DirectoryInfo(OutputFolder.Text!);
            DirectoryInfo? binPath = null;

            var subsetConfig = new SubsetConfig
            {
                SourceHanEllipsis = sourceHanEllipsis,
                DebugMode = debugMode,
                Backend = useHbSubset ? SubsetBackend.HarfBuzzSubset : SubsetBackend.PyFontTools,
            };
            
            await AssFontSubsetByPyFT(path, fontPath, outputPath, binPath, subsetConfig);
        }

        private async Task AssFontSubsetByPyFT(FileInfo[] path, DirectoryInfo? fontPath, DirectoryInfo? outputPath, DirectoryInfo? binPath, SubsetConfig subsetConfig)
        {
            try
            {
                Progressing.IsIndeterminate = true;
                var ssFt = new SubsetCore();
                await ssFt.SubsetAsync(path, fontPath, outputPath, binPath, subsetConfig);
                Progressing.IsIndeterminate = false;
                await ShowMessageBox("Success", I18nResources.SuccessSubset);
            }
            catch (Exception ex)
            {
                Progressing.IsIndeterminate = false;
                await ShowMessageBox("Error", ex.Message);
            }
        }

        private async Task ShowMessageBox(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.None, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this);
        }
        
        private void Drop_Files(object? sender, DragEventArgs e)
        {
            var dragData = (IEnumerable<IStorageItem>?)e.Data.Get(DataFormats.Files);
            if (dragData == null) return;
            var files = dragData.ToArray();
            
            var validFiles = files.Where(f => Path.GetExtension(f.Name) == ".ass").ToArray();
            if (validFiles.Length == 0) return;

            AssFileList.ItemsSource = validFiles.Select(f => f.Path.LocalPath).Order().ToList();
            var dir = validFiles[0].GetParentAsync().Result;
            FontFolder.Text = Path.Combine(dir!.Path.LocalPath, "fonts");
            OutputFolder.Text = Path.Combine(dir!.Path.LocalPath, "output");
        }
    }
}