using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Path = System.IO.Path;

namespace AssFontSubset
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] m_AssFiles = null;
        private Random m_Random = new Random();

        private ObservableCollection<ProcessInfo> m_ProcessList = new ObservableCollection<ProcessInfo>();
        private object m_ProcessListLock = new object();
        public ObservableCollection<ProcessInfo> TaskList { get { return m_ProcessList; } set { m_ProcessList = value; } }

        private struct SubsetFontInfo
        {
            public string OriginalFontFile;
            public string SubsetFontFile;
            public string FontNameInAss;
            public string SubsetFontName;
            public string DumpedXmlFile;
            public int TrackIndex;
        }

        private struct AssFontInfo
        {
            public string AssFilePath;
            public int LineNumber;
        }

        private struct FontFileInfo
        {
            public int FontNumberInCollection;
            public string FileName;
        }

        public MainWindow()
        {
            BindingOperations.EnableCollectionSynchronization(TaskList, m_ProcessListLock);
            this.DataContext = this;

            InitializeComponent();

            this.m_AssFiles = Environment.GetCommandLineArgs().Skip(1).ToArray();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.m_AssFiles != null && this.m_AssFiles.Length > 0) {
                this.FileDrop(this.m_AssFiles);
                this.Start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            } else {
                    Task.Run(() => {
                    try {
                        using (var client = new WebClient()) {
                            byte[] buf = client.DownloadData("https://raw.githubusercontent.com/youlun/AssFontSubset/master/AssFontSubset/Properties/AssemblyInfo.cs");
                            string data = Encoding.UTF8.GetString(buf);
                            var match = Regex.Match(data, @"\[assembly: AssemblyVersion\(""([0-9\.]*?)""\)\]", RegexOptions.ECMAScript | RegexOptions.Compiled);
                            if (match.Groups.Count > 1) {
                                var onlineVer = new Version(match.Groups[1].Value);
                                var localVer = Assembly.GetEntryAssembly().GetName().Version;
                                if (onlineVer > localVer) {
                                    MessageBox.Show("发现新版本，请去 GitHub 主页下载", "新版", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            }
                        } catch { }
                    });
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            this.AssFileList.ItemsSource = null;
            this.FontFolder.Text = "";
            this.OutputFolder.Text = "";
        }

        private void ParseAssfiles(string[] assFiles, ref Dictionary<string, List<AssFontInfo>> fontsInAss,
    ref Dictionary<string, string> textsInAss)
        {
            foreach (string assFile in assFiles) {
                AssParser parser = new AssParser();
                var result = parser.Parse(assFile);
                var _fonts = result.Item1;
                var _texts = result.Item2;

                foreach (var font in _fonts) {
                    string fontName = font.Key;
                    if (!fontsInAss.ContainsKey(fontName)) {
                        fontsInAss[fontName] = new List<AssFontInfo>();
                    }
                    foreach (int line in font.Value) {
                        fontsInAss[fontName].Add(new AssFontInfo {
                            AssFilePath = assFile,
                            LineNumber = line
                        });
                    }
                }

                foreach (var text in _texts) {
                    string fontName = text.Key;
                    if (!textsInAss.ContainsKey(fontName)) {
                        textsInAss[fontName] = "";
                    }
                    textsInAss[fontName] += text.Value;
                }
            }
            var keys = new List<string>(textsInAss.Keys);
            foreach (var key in keys) {
                textsInAss[key] = new string(textsInAss[key].Distinct().ToArray());
            }
        }

        private bool FindFontFiles(string fontFolder, Dictionary<string, List<AssFontInfo>> fontsInAss,
            ref Dictionary<string, FontFileInfo> fontFileInfo)
        {
            var duplicateFontFiles = new Dictionary<string, List<string>>();

            var fontFiles = Directory.EnumerateFiles(fontFolder, "*.*", SearchOption.TopDirectoryOnly);
            string[] fontExtensions = { ".fon", ".otf", ".ttc", ".ttf" };
            foreach (var file in fontFiles) {
                if (fontExtensions.Count(e => e == Path.GetExtension(file).ToLower()) == 0) {
                    continue;
                }

                int index = -1;
                string fontName = string.Empty;
                var fontFamilies = System.Windows.Media.Fonts.GetFontFamilies(file).ToList();
                for (index = 0; index < fontFamilies.Count; index++) {
                    var result = fontFamilies[index].FamilyNames.Values.Where(name => fontsInAss.ContainsKey(name));
                    if (result.Count() < 1) {
                        continue;
                    }
                    fontName = result.First();
                    break;
                }

                if (string.IsNullOrEmpty(fontName)) {
                    PrivateFontCollection collection = new PrivateFontCollection();
                    collection.AddFontFile(file);
                    if (collection.Families.Length > 0) {
                        fontName = collection.Families[0].Name;
                    }
                }

                if (string.IsNullOrEmpty(fontName)) {
                    continue;
                }

                if (fontFileInfo.ContainsKey(fontName)) {
                    if (!duplicateFontFiles.ContainsKey(fontName)) {
                        duplicateFontFiles[fontName] = new List<string> { fontFileInfo[fontName].FileName };
                    }
                    duplicateFontFiles[fontName].Add(file);
                }

                fontFileInfo[fontName] = new FontFileInfo { FontNumberInCollection = index, FileName = file };
            }

            if (duplicateFontFiles.Count > 0) {
                string text = "找到以下重复字体文件：\r\n";
                foreach (var font in duplicateFontFiles) {
                    text += $"    {font.Key}：\r\n";
                    font.Value.Sort();
                    font.Value.ForEach(file => text += $"        {Path.GetFileName(file)}\r\n");
                }
                text += "\r\n";
                text += "将从重复字体文件中随机选取字体，是否继续？";

                var result = MessageBox.Show(text, "找到重复字体文件", MessageBoxButton.YesNo,
                    MessageBoxImage.Question,  MessageBoxResult.No);
                return result == MessageBoxResult.Yes;
            }

            return true;
        }

        private bool DetectNotExistsFont(Dictionary<string, List<AssFontInfo>> fontsInAss,
            Dictionary<string, FontFileInfo> fontFiles)
        {
            List<string> notExists = new List<string>();
            foreach (var kv in fontsInAss) {
                if (!fontFiles.ContainsKey(kv.Key)) {
                    notExists.Add(kv.Key);
                }
            }
            if (notExists.Count > 0) {
                MessageBox.Show($"以下字体未找到，无法继续：\r\n{string.Join("\r\n", notExists)}",
                    "缺少字体", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void CreateFontSubset(string fontFolder, string outputFolder, Dictionary<string, string> textsInAss,
            Dictionary<string, FontFileInfo> fontFiles, ref List<SubsetFontInfo> subsetFonts)
        {
            var processors = new List<Dictionary<string, string>>();

            foreach (var text in textsInAss) {
                var fontName = text.Key;
                var characters = text.Value;

                var charactersFile = $@"{fontFolder}\{fontName}.txt";
                using (StreamWriter sw = new StreamWriter(charactersFile, false, new UTF8Encoding(false))) {
                    sw.Write(characters);
                }

                int index = fontFiles[fontName].FontNumberInCollection;
                string fontFile = fontFiles[fontName].FileName;

                string outputFile = $@"{outputFolder}\";
                if (fontFile.EndsWith(".ttc")) {
                    outputFile += $"{Path.GetFileNameWithoutExtension(fontFile)}.ttf";
                } else {
                    outputFile += $"{Path.GetFileName(fontFile)}";
                }

                var subsetFontInfo = new SubsetFontInfo {
                    FontNameInAss = fontName,
                    OriginalFontFile = fontFile,
                    SubsetFontFile = outputFile + "._tmp_",
                    SubsetFontName = this.RandomString(8),
                    DumpedXmlFile = outputFolder + "\\" + Path.GetFileNameWithoutExtension(outputFile) + ".ttx",
                    TrackIndex = index
                };
                subsetFonts.Add(subsetFontInfo);

                var args = new Dictionary<string, string> {
                    { " ", fontFile },
                    { "--text-file=", charactersFile},
                    { "--output-file=" , subsetFontInfo.SubsetFontFile},
                    { "--name-languages=", "0x0409"}
                };
                if (index > -1) {
                    args.Add("--font-number=", index.ToString());
                }

                processors.Add(args);
            }

            string exe = "pyftsubset.exe";
            Parallel.ForEach(processors, args => this.StartProcess(exe, args));
        }

        private void DumpFont(List<SubsetFontInfo> subsetFonts)
        {
            string exe = "ttx.exe";
            Parallel.ForEach(subsetFonts, font => this.StartProcess(exe,
                new Dictionary<string, string> { { "-f ", "" }, { "-o ", font.DumpedXmlFile }, { "", font.SubsetFontFile } }));
            subsetFonts.ForEach(font => File.Delete(font.SubsetFontFile));
        }

        private void ChangeXmlFontName(List<SubsetFontInfo> subsetFonts)
        {
            Parallel.ForEach(subsetFonts, (font) => {
                var ttxFile = font.DumpedXmlFile;
                string ttxString = string.Empty;

                if (!File.Exists(ttxFile)) {
                    MessageBox.Show($"字体生成 ttx 文件失败，请尝试使用 FontForge 重新生成字体。\r\n" +
                        $"字体名：{font.FontNameInAss}\r\n文件名：{font.OriginalFontFile}\r\n",
                        "生成ttx文件失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var ttxContent = File.ReadAllText(ttxFile, new UTF8Encoding(false));
                ttxContent = Regex.Replace(ttxContent, @"<map code=""0x3000"" name=""(.*?)""/>",
                    @"<map code=""0x3000"" name=""$1""/><map code=""0xa0"" name=""$1""/>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                bool replaced = false;
                var xd = new XmlDocument();
                xd.LoadXml(ttxContent);
                var namerecordList = xd.SelectNodes("ttFont/name/namerecord");
                foreach (XmlNode record in namerecordList) {
                    string nameID = record.Attributes["nameID"].Value.Trim();
                    if (record.Attributes["langID"].Value != "0x409") {
                        continue;
                    }

                    switch (nameID) {
                        case "1":
                        case "3":
                        case "4":
                        case "6":
                            record.InnerText = font.SubsetFontName;
                            replaced = true;
                            break;
                        default:
                            break;
                    }
                }
                xd.Save(ttxFile);

                if (!replaced) {
                    MessageBox.Show($"字体名称替换失败，请尝试使用 FontForge 重新生成字体。\r\n" +
                        $"字体名：{font.FontNameInAss}\r\n文件名：{font.OriginalFontFile}\r\n",
                        "字体名称替换失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            });
        }

        private void CompileFont(string outputFolder)
        {
            string exe = "ttx.exe";
            var files = Directory.EnumerateFiles(outputFolder, "*.ttx", SearchOption.TopDirectoryOnly);
            Parallel.ForEach(files, file => this.StartProcess(exe, new Dictionary<string, string> { { "-f", "" }, { "", file } }));
            files.ToList().ForEach(file => File.Delete(file));
        }

        private void ReplaceFontNameInAss(string[] assFiles, string outputFolder, Dictionary<string, List<AssFontInfo>> fontsInAss,
            List<SubsetFontInfo> subsetFonts)
        {
            foreach (var assFile in assFiles) {
                var assContent = new List<string>();
                var subsetComments = new List<string>();

                using (StreamReader sr = new StreamReader(assFile, true)) {
                    while (!sr.EndOfStream) {
                        assContent.Add(sr.ReadLine());
                    }
                }

                foreach (var assFontInfo in fontsInAss) {
                    string fontName = assFontInfo.Key;
                    var newFontName = subsetFonts.Find(f => f.FontNameInAss == fontName).SubsetFontName;

                    foreach (var font in assFontInfo.Value) {
                        if (font.AssFilePath != assFile) {
                            continue;
                        }
                        int line = font.LineNumber;

                        string row = assContent[line];
                        if (row.Substring(0, 6).ToLower() == "style:") {
                            assContent[line] = row.Replace(fontName, newFontName);
                        } else if (row.Substring(0, 9).ToLower() == "dialogue:") {
                            if (row.Contains($@"\fn{fontName}")) {
                                assContent[line] = row.Replace($@"\fn{fontName}", $@"\fn{newFontName}");
                            } else if (row.Contains($@"\fn@{fontName}")) {
                                assContent[line] = row.Replace($@"\fn@{fontName}", $@"\fn@{newFontName}");
                            }
                        }
                    }

                    string subsetComment = $"; Font Subset: {newFontName} - {fontName}";
                    if (!subsetComments.Contains(subsetComment)) {
                        subsetComments.Add(subsetComment);
                    }
                }

                int index = assContent.FindIndex(row => row.Length >= 13 && row.Substring(0, 13).ToLower() == "[script info]");
                assContent.Insert(index + 1, string.Join("\r\n", subsetComments));

                string newAssContent = string.Join("\r\n", assContent);
                using (StreamWriter sw = new StreamWriter(outputFolder + "\\" + Path.GetFileName(assFile), false, Encoding.UTF8)) {
                    sw.Write(newAssContent);
                }
            }
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            try {
                this.AssFileList.Focus();

                string[] assFiles = this.AssFileList.Items.Cast<string>().ToArray();
                string fontFolder = this.FontFolder.Text;
                string outputFolder = this.OutputFolder.Text;

                if (assFiles.Length == 0) {
                    MessageBox.Show("没有设置字幕文件");
                    return;
                }
                if (!Directory.Exists(fontFolder)) {
                    MessageBox.Show("字体目录不存在");
                    return;
                }
                if (Directory.Exists(outputFolder)) {
                    Directory.Delete(outputFolder, true);
                }
                Directory.CreateDirectory(outputFolder);

                var fontsInAss = new Dictionary<string, List<AssFontInfo>>();
                var textsInAss = new Dictionary<string, string>();
                var subsetFonts = new List<SubsetFontInfo>();
                var fontFiles = new Dictionary<string, FontFileInfo>();

                this.Progressing.IsIndeterminate = true;
                this.m_SubsetPage.IsEnabled = false;
                this.m_ProcessListTab.IsSelected = true;
                await Task.Run(() => {
                    try {
                        this.Dispatcher.Invoke((() => this.Title = "解析字幕文本"));
                        this.ParseAssfiles(assFiles, ref fontsInAss, ref textsInAss);

                        this.Dispatcher.Invoke((() => this.Title = "读取字体文件"));
                        if (!this.FindFontFiles(fontFolder, fontsInAss, ref fontFiles)) {
                            return;
                        }

                        this.Dispatcher.Invoke((() => this.Title = "检查字体文件"));
                        if (!this.DetectNotExistsFont(fontsInAss, fontFiles)) {
                            return;
                        }

                        this.Dispatcher.Invoke((() => this.Title = "创建字体子集"));
                        this.CreateFontSubset(fontFolder, outputFolder, textsInAss, fontFiles, ref subsetFonts);

                        this.Dispatcher.Invoke((() => this.Title = "字体拆包"));
                        this.DumpFont(subsetFonts);

                        this.Dispatcher.Invoke((() => this.Title = "修改字体名称"));
                        this.ChangeXmlFontName(subsetFonts);

                        this.Dispatcher.Invoke((() => this.Title = "字体组装"));
                        this.CompileFont(outputFolder);

                        this.Dispatcher.Invoke((() => this.Title = "重命名字幕字体"));
                        this.ReplaceFontNameInAss(assFiles, outputFolder, fontsInAss, subsetFonts);
                    } catch (Exception ex) {
                        MessageBox.Show(ex.ToString(), "发生异常", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
                this.Title = "完成";
                GC.Collect();
                this.m_SubsetPage.IsEnabled = true;
                this.Progressing.IsIndeterminate = false;
                this.m_SubsetTab.IsSelected = true;

                if (this.m_AssFiles != null && this.m_AssFiles.Length > 0) {
                    Application.Current.Shutdown();
                }

            } catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "发生异常", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartProcess(string exe, Dictionary<string, string> args)
        {
            string output = "";
            bool success = true;

            Process p = new Process {
                StartInfo = new ProcessStartInfo() {
                    FileName = exe,
                    Arguments = string.Join(" ", args.Select(arg => arg.Key + (!string.IsNullOrEmpty(arg.Value) ? $"\"{arg.Value}\"" : ""))),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var taskId = Guid.NewGuid();

            DataReceivedEventHandler dataReceived = (object sender, DataReceivedEventArgs e) => {
                output += e.Data + "\r\n";
                string lowerCase = output.ToLower();
                if (lowerCase.Contains("traceback (most recent call last)")) {
                    success = false;
                    try {
                        p.Kill();
                    } catch { }
                } else if (lowerCase.Contains("hit any key to exit")) {
                    success = false;
                    try {
                        p.Kill();
                    } catch { }
                }

                lock (this.m_ProcessList) {
                    var info = this.m_ProcessList.Where(proc => proc.TaskId == taskId).First();
                    if (info != null) {
                        info.Output = output;
                    }
                }
            };

            p.OutputDataReceived += dataReceived;
            p.ErrorDataReceived += dataReceived;

            lock (this.m_ProcessList) {
                this.m_ProcessList.Add(new ProcessInfo { TaskId = taskId, Argument = $"{exe} {p.StartInfo.Arguments}", Output = "" });
            }

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            lock (this.m_ProcessList) {
                this.m_ProcessList.Remove(this.m_ProcessList.Where(proc => proc.TaskId == taskId).First());
            }

            if (!success) {
                MessageBox.Show($"调用 {exe} 时发生错误，请尝试使用 FontForge 重新生成字体。" +
                    $"参数列表：\r\n{string.Join("\r\n", args.Select(arg => arg.Key + arg.Value))}\r\n\r\n{output}",
                    "调用外部程序时发生错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[this.m_Random.Next(s.Length)]).ToArray());
        }

        private void FileDrop(string[] files)
        {
            if (files == null || files.Length == 0) {
                return;
            }
            var validFiles = new List<string>();
            validFiles.AddRange(files.Where(f => Path.GetExtension(f) == ".ass"));
            if (validFiles.Count == 0) {
                return;
            }

            this.AssFileList.ItemsSource = validFiles;
            this.FontFolder.Text = Path.GetDirectoryName(validFiles[0]) + "\\fonts";
            this.OutputFolder.Text = Path.GetDirectoryName(validFiles[0]) + "\\output";

            this.FontFolder.Select(this.FontFolder.Text.Length - 1, 0);
            this.OutputFolder.Select(this.OutputFolder.Text.Length - 1, 0);
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.FileDrop(files);
        }

        private void ProcessList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2) {
                var info = (this.ProcessList.SelectedItem as ProcessInfo);
                if (info != null) {
                    MessageBox.Show(info.Output, info.Argument, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
