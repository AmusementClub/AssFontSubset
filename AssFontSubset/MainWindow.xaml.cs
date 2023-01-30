using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
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

        private bool m_Continue = true;

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RemoveFontResourceEx(string lpszFilename, int fl, IntPtr pdv);

        private List<string> skipList = new List<string> ();

        private string rootdir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private void log(string info)
        {
            if ((bool) this.Debug.IsChecked) {
                using (StreamWriter logger = new StreamWriter(Path.Combine(rootdir, $"log_{DateTime.Now.ToString("yyyy-MM-dd_HHmmss")}.txt"), true, Encoding.UTF8)) {
                    logger.WriteLine(info);
                }
            }
        }

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
            public string FontName;
        }

        public MainWindow()
        {
            BindingOperations.EnableCollectionSynchronization(TaskList, m_ProcessListLock);
            this.DataContext = this;


            InitializeComponent();
            this.SourceHanEllipsis.IsChecked = Properties.Settings.Default.SourceHanEllipsis;
            this.Debug.IsChecked = Properties.Settings.Default.Debug;
            this.CloudList.IsChecked = Properties.Settings.Default.CloudList;
            this.LocalList.IsChecked = Properties.Settings.Default.LocalList;
            
            this.m_AssFiles = Environment.GetCommandLineArgs().Skip(1).ToArray();

            log($"MainWindow 1: skiplist = {string.Join(", ", this.skipList)}");
            if ((bool)this.LocalList.IsChecked) {
                this.skipList.AddRange(readLocalSkipList());
            }
            if ((bool)this.CloudList.IsChecked) {
                this.skipList.AddRange(readCloudSkipList());
            }
            log($"MainWindow 2: skiplist = {string.Join(", ", this.skipList)}");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            check_dependency("ttx.exe", "fonttools", "https://github.com/fonttools/fonttools");
            check_dependency("pyftsubset.exe", "fonttools", "https://github.com/fonttools/fonttools");


            if (this.m_AssFiles != null && this.m_AssFiles.Length > 0) {
                this.FileDrop(this.m_AssFiles);
                this.Start.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            } else {
                Task.Run(() => check_update("https://raw.githubusercontent.com/tastysugar/AssFontSubset/master/AssFontSubset/Properties/AssemblyInfo.cs"));
                Task.Run(() => check_update("https://cdn.jsdelivr.net/gh/tastysugar/AssFontSubset@master/AssFontSubset/Properties/AssemblyInfo.cs"));
            }
        }

        private static void check_update(string url)
        {
            try {
                using (var client = new WebClient()) {
                    byte[] buf = client.DownloadData(url);
                    string data = Encoding.UTF8.GetString(buf);
                    var match = Regex.Match(data, @"\[assembly: AssemblyVersion\(""([0-9\.]*?)""\)\]", RegexOptions.ECMAScript | RegexOptions.Compiled);
                    if (match.Groups.Count > 1) {
                        var onlineVer = new Version(match.Groups[1].Value);
                        var localVer = Assembly.GetEntryAssembly().GetName().Version;
                        if (onlineVer > localVer) {
                            var result = MessageBox.Show($"当前版本: {localVer.ToString()}\n发现新版本: {onlineVer.ToString()}\n请去 GitHub 主页下载", "新版", MessageBoxButton.YesNo);
                            if (result.ToString() == "Yes") {
                                System.Diagnostics.Process.Start("https://github.com/tastysugar/AssFontSubset/releases");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void check_dependency(string executable, string pkg_name, string url) {
            if (GetFullPathFromWindows(executable) == null) {
                var result = MessageBox.Show($"无法找到 \"{executable}\"，请安装 {pkg_name}，并确保其路径在 PATH 中。", "缺少依赖", MessageBoxButton.YesNo);
                if (result.ToString() == "Yes") {
                    System.Diagnostics.Process.Start(url);
                    this.Close();
                }
                if (result.ToString() == "No") {
                    this.Close();
                }
            }
        }
        private List<string> readLocalSkipList()
        {
            List<string> output = new List<string>();

            var skipListPath = Path.Combine(rootdir, "skiplist.txt");
            if (!File.Exists(skipListPath)){
                return output;
            }

            using (StreamReader sr = new StreamReader(skipListPath, Encoding.UTF8)) {
                while (!sr.EndOfStream) {
                    output.Add(sr.ReadLine().Trim());
                }
            }
            log($"readLocalSkipList: output = {string.Join(", ", output)}\n");
            return output;
        }

        private List<string> readCloudSkipList()
        {
            List<string> output = new List<string>();
            try {
                using (var client = new WebClient()) {
                    byte[] buf = client.DownloadData("https://raw.githubusercontent.com/tastysugar/AssFontSubset/master/CloudSkipList.txt");
                    string data = Encoding.UTF8.GetString(buf);
                    output = data.Split('\n').ToList();
                    for (int i = 0; i < output.Count; i++) {
                        output[i] = output[i].Trim();
                    }
                }
            } catch (Exception e) {
                log($"readCloudSkipList: exception catched \nMessage:{e.Message}\n");
            }
            log($"readCloudSkipList: output = {string.Join(", ", output)}\n");
            return output;
        }

        public static string GetFullPathFromWindows(string exeName) {
            if (exeName.Length >= MAX_PATH)
                throw new ArgumentException($"The executable name '{exeName}' must have less than {MAX_PATH} characters.",
                    nameof(exeName));

            StringBuilder sb = new StringBuilder(exeName, MAX_PATH);
            return PathFindOnPath(sb, null) ? sb.ToString() : null;
        }

        // https://docs.microsoft.com/en-us/windows/desktop/api/shlwapi/nf-shlwapi-pathfindonpathw
        // https://www.pinvoke.net/default.aspx/shlwapi.PathFindOnPath
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);

        // from MAPIWIN.h :
        private const int MAX_PATH = 260;

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
            ref List<FontFileInfo> fontFileInfo, Dictionary<string, bool> flags)
        {
            var fontFiles = Directory.EnumerateFiles(fontFolder, "*.*", SearchOption.TopDirectoryOnly);

            string[] fontExtensions = { ".fon", ".otf", ".ttc", ".ttf" };
            foreach (var file in fontFiles) {
                if (fontExtensions.Count(e => e == Path.GetExtension(file).ToLower()) == 0) {
                    continue;
                }

                int index = 0;
                var fontNames = new HashSet<Tuple<string, int>>(); // (fontname, index)
                bool isCollection = Path.GetExtension(file).ToLower() == ".ttc";

                if (!isCollection) {
                    MatchFontNames(file, fontNames, fontsInAss, index, flags);
                }
                else {
                    int ttcCount = GetTTCCount(file);
                    for (index = 0; index < ttcCount; index++) {
                        MatchFontNames(file, fontNames, fontsInAss, index, flags);
                    }
                }
                
                if (fontNames.Count == 0) {
                    continue;
                }

                foreach (var fontName in fontNames) {
                    fontFileInfo.Add(new FontFileInfo { FontNumberInCollection = fontName.Item2, FileName = file, FontName = fontName.Item1 });
                }
            }

            return true;
        }

        private void MatchFontNames(string file, HashSet<Tuple<string, int>> fontNames, Dictionary<string, List<AssFontInfo>> fontsInAss, int index, Dictionary<string, bool> flags) 
        {
            var familynames = new List<string>();
            var fullnames = new List<string>();

            string ttxContent = StartProcess("ttx", new Dictionary<string, string> { { "-o", $"-" }, { "-y", index.ToString() }, { "-t", "name" }, {"-q" , ""}, { "", file } });

            var xd = new XmlDocument();
            ttxContent = ttxContent.Replace("\0", ""); // remove null characters. it might be a bug in ttx.exe. 

            if (flags["Debug"]) {
                using (StreamWriter sw = new StreamWriter($"{file}_{index}.ttx", false, new UTF8Encoding(false))) {
                    sw.Write(ttxContent);
                }
            }

            xd.LoadXml(ttxContent);
            XmlNodeList namerecords = xd.SelectNodes(@"ttFont/name/namerecord[@platformID=3]");
            foreach (XmlNode record in namerecords) {
                string nameID = record.Attributes["nameID"].Value.Trim();
                switch (nameID) {
                    case "1":
                        familynames.Add(record.InnerText.Trim());
                        break;
                    case "4":
                        fullnames.Add(record.InnerText.Trim());
                        break;
                    default:
                        break;
                }
            }

            foreach (var fullnameResult in familynames.Where(name => fontsInAss.ContainsKey(name))) {
                fontNames.Add(new Tuple<string, int>(fullnameResult, index));
            }
            foreach (var familynameResult in familynames.Where(name => fontsInAss.ContainsKey(name))) {
                fontNames.Add(new Tuple<string, int>(familynameResult, index));
            }
        }


        private int GetTTCCount(string file) 
        {
            var fs = new FileStream(file, FileMode.Open);
            var reader = new BinaryReader(fs);
            reader.ReadInt32();
            reader.ReadInt32();
            int numOfFont = reader.ReadInt32();
            reader.Close();
            fs.Close();
            
            // convert Big Endian to Little Endian
            if (BitConverter.IsLittleEndian) 
            {
                byte[] bytes = BitConverter.GetBytes(numOfFont);
                Array.Reverse(bytes);
                numOfFont = BitConverter.ToInt32(bytes, 0);
            }

            if (numOfFont < 0) {
                MessageBox.Show($"ttc 字体数量非法：\r\n{Path.GetFileName(file)} = {numOfFont}\r\n请检查该 ttc 是否为合法文件。",
                "ttc 读取失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return numOfFont;
        }

        private bool DetectNotExistsFont(Dictionary<string, List<AssFontInfo>> fontsInAss,
            List<FontFileInfo> fontFiles)
        {
            List<string> notExists = new List<string>();
            var fontNames = new List<string>();
            foreach (var fontFileInfo in fontFiles) {
                fontNames.Add(fontFileInfo.FontName);
            }

            foreach (var kv in fontsInAss) {
                if (!fontNames.Contains(kv.Key)) {
                    notExists.Add(kv.Key);
                }
            }

            if (notExists.Count > 0) {
                MessageBox.Show($"以下字体未找到，无法继续：\r\n{string.Join("\r\n", notExists)}。提示：请确认字体名大小写是否正确。",
                    "缺少字体", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void CreateFontSubset(string fontFolder, string outputFolder, Dictionary<string, string> textsInAss,
            List<FontFileInfo> fontFiles, ref List<SubsetFontInfo> subsetFonts, ref Dictionary<string, string> rdNameLookUp, List<string> skipList, Dictionary<string, bool> flags)
        {
            var processors = new List<Dictionary<string, string>>();

            foreach (var font in fontFiles) {

                var fontName = font.FontName;

                // skip fonts in skipList
                if (skipList.Contains(fontName.ToLower()))
                {
                    File.Copy(font.FileName, Path.Combine(outputFolder, Path.GetFileName(font.FileName)), true);
                    continue;
                }

                var characters = textsInAss[fontName];

                // fix font fallback on ellipsis.
                characters = Regex.Replace(characters, @"[a-zA-Z0-9]", "", RegexOptions.Compiled);
                characters += "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

                // remove all full width numeric characters and replace them with a full set of them.
                var fullwidth_numerical = new Regex(@"[１２３４５６７８９０]");
                if (fullwidth_numerical.IsMatch(characters)) {
                    characters = fullwidth_numerical.Replace(characters, "");
                    characters += "１２３４５６７８９０";
                }

                var charactersFile = $@"{fontFolder}\{fontName}.txt";
                using (StreamWriter sw = new StreamWriter(charactersFile, false, new UTF8Encoding(false))) {
                    sw.Write(characters);
                }

                int index = font.FontNumberInCollection;
                string fontFile = font.FileName;

                string outputFile = $@"{outputFolder}\";
                if (fontFile.EndsWith(".ttc")) {
                    outputFile += $"{Path.GetFileNameWithoutExtension(fontFile)}.ttf";
                } else {
                    outputFile += $"{Path.GetFileName(fontFile)}";
                }

                // assign same randomized name for fonts with same family name
                string randomString = "";
                if (rdNameLookUp.ContainsKey(fontName)) {
                    randomString = rdNameLookUp[fontName];
                } else {
                    do {
                        randomString = this.RandomString(8);
                    } while (rdNameLookUp.ContainsValue(randomString)); // do while loop to avoid random string collision

                    rdNameLookUp.Add(fontName, randomString);
                }

                var subsetFontInfo = new SubsetFontInfo {
                    FontNameInAss = fontName,
                    OriginalFontFile = fontFile,
                    SubsetFontFile = outputFile + $".{randomString}._tmp_",
                    SubsetFontName = randomString,
                    DumpedXmlFile = $@"{outputFolder}\{Path.GetFileNameWithoutExtension(outputFile)}.{index}.{randomString}.ttx",
                    TrackIndex = index
                };
                subsetFonts.Add(subsetFontInfo);

                var args = new Dictionary<string, string> {
                    { " ", fontFile },
                    { "--text-file=", charactersFile},
                    { "--output-file=" , subsetFontInfo.SubsetFontFile},
                    { "--name-languages=", "*"},
                    { "--font-number=", index.ToString()}
                };

                processors.Add(args);
            }

            string exe = "pyftsubset.exe";
            Parallel.ForEach(processors, args => this.StartProcess(exe, args));

            if (!flags["Debug"])
            {
                foreach (var font in fontFiles)
                {
                    File.Delete($@"{fontFolder}\{font.FontName}.txt");
                }
            }
        }

        private void DumpFont(List<SubsetFontInfo> subsetFonts, Dictionary<string, bool> flags)
        {
            string exe = "ttx.exe";
            Parallel.ForEach(subsetFonts, font => this.StartProcess(exe,
                new Dictionary<string, string> { { "-f ", "" }, { "-o ", font.DumpedXmlFile }, { "", font.SubsetFontFile } }));
            if (!flags["Debug"])
                subsetFonts.ForEach(font => File.Delete(font.SubsetFontFile));
        }

        private void ChangeXmlFontName(List<SubsetFontInfo> subsetFonts, Dictionary<string, bool> flags)
        {
            Parallel.ForEach(subsetFonts, (font) => {
                if (!this.m_Continue) {
                    return;
                }

                var ttxFile = font.DumpedXmlFile;
                string ttxString = string.Empty;

                if (!File.Exists(ttxFile)) {
                    var result = MessageBox.Show($"字体生成 ttx 文件失败，请尝试使用 FontForge 重新生成字体。\r\n" +
                        $"字体名：{font.FontNameInAss}\r\n文件名：{font.OriginalFontFile}\r\n",
                        "生成ttx文件失败", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No);
                    this.m_Continue = result == MessageBoxResult.Yes;
                    return;
                }

                var ttxContent = File.ReadAllText(ttxFile, new UTF8Encoding(false));
                ttxContent = ttxContent.Replace("\0", ""); // remove null characters. it might be a bug in ttx.exe. 
                bool replaced = false;

                var specialFont = ""; // special hack for some fonts

                var xd = new XmlDocument();
                xd.LoadXml(ttxContent);

                // replace font name
                var namerecords = xd.SelectNodes(@"ttFont/name/namerecord");

                foreach (XmlNode record in namerecords) {
                    string nameID = record.Attributes["nameID"].Value.Trim();
                    switch (nameID) {
                        case "0":
                            record.InnerText = $"Processed by AssFontSubset v{Assembly.GetEntryAssembly().GetName().Version}";
                            break;
                        case "1":
                        case "3":
                        case "4":
                        case "6":
                            if (record.InnerText.Contains("Source Han")) {
                                specialFont = "Source Han";
                            }
                            record.InnerText = font.SubsetFontName;
                            replaced = true;
                            break;
                        default:
                            break;
                    }
                }

                // remove substitution for ellipsis for source han sans/serif font
                if (flags["SourceHanEllipsis"] == true && specialFont == "Source Han") {
                    SourceHanFontEllipsis(ref xd);
                }

                xd.Save(ttxFile);

                if (!replaced) {
                    var result = MessageBox.Show($"字体名称替换失败，请尝试使用 FontForge 重新生成字体。\r\n" +
                        $"字体名：{font.FontNameInAss}\r\n文件名：{font.OriginalFontFile}\r\n",
                        "字体名称替换失败", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No);
                    this.m_Continue = result == MessageBoxResult.Yes;
                    return;
                }
            });
        }

        // Special Hack for Source Han Sans & Source Han Serif ellipsis
        private void SourceHanFontEllipsis(ref XmlDocument xd) {
            // find cid for ellipsis (\u2026)
            XmlNode cmap = xd.SelectSingleNode(@"//map[@code='0x2026']");
            if (cmap != null) {
                String ellipsisCid = cmap.Attributes["name"].Value.Trim();
                XmlNodeList substitutionNodes = xd.SelectNodes($"//Substitution[@in='{ellipsisCid}']");
                // remove substitution for lower ellipsis. 
                // NOTE: Vertical ellipsis is cid5xxxxx, and we need to keep it. Hopefully Adobe won't change it.
                foreach (XmlNode sNode in substitutionNodes) {
                    if (Regex.IsMatch(sNode.Attributes["out"].Value, @"cid6")) {
                        sNode.ParentNode.RemoveChild(sNode);
                    }
                }
            }
        }

        private void CompileFont(string outputFolder, Dictionary<string, bool> flags)
        {
            string exe = "ttx.exe";
            var files = Directory.EnumerateFiles(outputFolder, "*.ttx", SearchOption.TopDirectoryOnly);
            Parallel.ForEach(files, file => this.StartProcess(exe, new Dictionary<string, string> { { "-f", "" }, { "", file } }));
            if (!flags["Debug"])
                files.ToList().ForEach(file => File.Delete(file));
        }

        private void ReplaceFontNameInAss(string[] assFiles, string outputFolder, Dictionary<string, List<AssFontInfo>> fontsInAss,
            List<SubsetFontInfo> subsetFonts, List<string> skipList)
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

                    // skip list
                    if (skipList.Contains(fontName.ToLower()))
                        continue;

                    var newFontName = subsetFonts.Find(f => f.FontNameInAss == fontName).SubsetFontName;

                    foreach (var font in assFontInfo.Value) {
                        if (font.AssFilePath != assFile) {
                            continue;
                        }
                        int line = font.LineNumber;

                        string row = assContent[line];
                        if (row.Substring(0, 6).ToLower() == "style:") {
                            assContent[line] = Regex.Replace(assContent[line], $"(Style:[^,\n]+),(@?){Regex.Escape(fontName)},", $"${{1}},${{2}}{newFontName},", RegexOptions.Compiled);
                        } else if (row.Substring(0, 9).ToLower() == "dialogue:") {
                            assContent[line] = Regex.Replace(assContent[line], $@"\\fn(@?){Regex.Escape(fontName)}", $@"\fn${{1}}{newFontName}", RegexOptions.Compiled);
                        }
                    }

                    string subsetComment = $"; Font Subset: {newFontName} - {fontName}";
                    if (!subsetComments.Contains(subsetComment)) {
                        subsetComments.Add(subsetComment);
                    }
                }

                int index = assContent.FindIndex(row => row.Length >= 13 && row.Substring(0, 13).ToLower() == "[script info]");
                assContent.Insert(index + 1, $"; Processed by AssFontSubset v{Assembly.GetEntryAssembly().GetName().Version}");
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
                var fontFiles = new List<FontFileInfo>();
                var rdNameLookUp = new Dictionary<string, string>();
                var flags = new Dictionary<string, bool> {
                    { "SourceHanEllipsis", (bool)this.SourceHanEllipsis.IsChecked },
                    { "Debug", (bool)this.Debug.IsChecked }
                };
                var skipList = this.skipList;

                for (int i=0; i < skipList.Count; i++) {
                    skipList[i] = skipList[i].ToLower();
                }

                log($"Start Click: skiplist = {string.Join(", ", skipList)} \n");

                this.Progressing.IsIndeterminate = true;
                this.m_SubsetPage.IsEnabled = false;
                this.m_ProcessListTab.IsSelected = true;
                await Task.Run(() => {
                    try {
                        this.Dispatcher.Invoke((() => this.Title = "解析字幕文本"));
                        this.ParseAssfiles(assFiles, ref fontsInAss, ref textsInAss);

                        this.Dispatcher.Invoke((() => this.Title = "读取字体文件"));
                        if (!this.FindFontFiles(fontFolder, fontsInAss, ref fontFiles, flags)) {
                            return;
                        }

                        fontFiles = fontFiles.Distinct().ToList();

                        this.Dispatcher.Invoke((() => this.Title = "检查字体文件"));
                        if (!this.DetectNotExistsFont(fontsInAss, fontFiles)) {
                            return;
                        }

                        this.Dispatcher.Invoke((() => this.Title = "创建字体子集"));
                        this.CreateFontSubset(fontFolder, outputFolder, textsInAss, fontFiles, ref subsetFonts, ref rdNameLookUp, skipList, flags);

                        this.Dispatcher.Invoke((() => this.Title = "字体拆包"));
                        this.DumpFont(subsetFonts, flags);

                        this.Dispatcher.Invoke((() => this.Title = "修改字体名称"));
                        this.ChangeXmlFontName(subsetFonts, flags);

                        this.Dispatcher.Invoke((() => this.Title = "字体组装"));
                        this.CompileFont(outputFolder, flags);

                        this.Dispatcher.Invoke((() => this.Title = "重命名字幕字体"));
                        this.ReplaceFontNameInAss(assFiles, outputFolder, fontsInAss, subsetFonts, skipList);
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

        private string StartProcess(string exe, Dictionary<string, string> args)
        {
            if (!this.m_Continue) {
                return "";
            }

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
                    CreateNoWindow = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                },
                EnableRaisingEvents = true
            };
            p.StartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";


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
                var result = MessageBox.Show($"调用 {exe} 时发生错误，请尝试使用 FontForge 重新生成字体。\r\n" +
                    "是否继续？\r\n" +
                    $"参数列表：\r\n{string.Join("\r\n", args.Select(arg => arg.Key + arg.Value))}\r\n\r\n{output}",
                    "调用外部程序时发生错误", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No);
                this.m_Continue = result == MessageBoxResult.Yes;
            }
            return output;
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
            for (int i = 0; i < validFiles.Count(); ++i) {
                validFiles[i] = Path.GetFullPath(validFiles[i]);
            }
            
            this.AssFileList.ItemsSource = validFiles;
            string dir = Path.GetDirectoryName(validFiles[0]);
            this.FontFolder.Text = dir + "\\fonts";
            this.OutputFolder.Text = dir + "\\output";

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

        private void Window_Closed(object sender, EventArgs e) {
            Properties.Settings.Default.SourceHanEllipsis = (bool)SourceHanEllipsis.IsChecked;
            Properties.Settings.Default.CloudList = (bool)CloudList.IsChecked;
            Properties.Settings.Default.LocalList = (bool)LocalList.IsChecked;
            Properties.Settings.Default.Debug = (bool)Debug.IsChecked;
            Properties.Settings.Default.Save();
        }
    }
}
