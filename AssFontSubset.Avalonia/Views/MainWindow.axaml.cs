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

namespace AssFontSubset.Avalonia.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            AddHandler(DragDrop.DropEvent, Window_Drop);
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

            if (AssFileList.Items.Count == 0)
            {
                await ShowMessageBox("Error", "没有 ASS 文件可供处理，请检查");
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

            await AssFontSubsetByPyFT(path, fontPath, outputPath, binPath, sourceHanEllipsis, debugMode);
        }

        private async Task AssFontSubsetByPyFT(FileInfo[] path, DirectoryInfo? fontPath, DirectoryInfo? outputPath, DirectoryInfo? binPath, bool sourceHanEllipsis, bool debug)
        {
            try
            {
                var subsetConfig = new SubsetConfig
                {
                    SourceHanEllipsis = sourceHanEllipsis,
                    DebugMode = debug,
                };
                Progressing.IsIndeterminate = true;
                var ssFt = new SubsetByPyFT();
                await ssFt.SubsetAsync(path, fontPath, outputPath, binPath, subsetConfig);
                Progressing.IsIndeterminate = false;
                await ShowMessageBox("Sucess", "子集化完成，请检查 output 文件夹");
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

        private void FileDrop(IStorageItem[] files)
        {
            var validFiles = files.Where(f => Path.GetExtension(f.Name) == ".ass").ToArray();
            if (validFiles.Length == 0)
            {
                return;
            }

            AssFileList.ItemsSource = validFiles.Select(f => f.Path.LocalPath).Order().ToList();
            var dir = validFiles[0].GetParentAsync().Result;
            FontFolder.Text = Path.Combine(dir!.Path.LocalPath, "fonts");
            OutputFolder.Text = Path.Combine(dir!.Path.LocalPath, "output");
        }

        private void Window_Drop(object? sender, DragEventArgs e)
        {
            var dragData = (IEnumerable<IStorageItem>?)e.Data.Get(DataFormats.Files);
            if (dragData != null)
            {
                var files = dragData.ToArray();
                FileDrop(files);
            }
        }
    }
}