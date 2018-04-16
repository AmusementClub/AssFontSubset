using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AssFontSubset
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        struct SubsetFontInfo
        {
            public string OriginalFontFile;
            public string SubsetFontFile;
            public string FontNameInAss;
            public string SubsetFontName;
            public int TrackIndex;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string[] assFiles = this.listBox1.Items.Cast<string>().ToArray();
            string fontFolder = this.textBox2.Text;
            string outputFolder = this.textBox3.Text;

            if (assFiles.Length == 0) {
                MessageBox.Show("没有设置字幕文件");
                return;
            }
            if (!Directory.Exists(fontFolder)) {
                MessageBox.Show("字体目录不存在");
                return;
            }
            if (!Directory.Exists(outputFolder)) {
                Directory.CreateDirectory(outputFolder);
            }


            // Dictionary<字体名, List<Tuple<ASS文件名, 行数>>>
            Dictionary<string, List<Tuple<string, int>>> fonts = new Dictionary<string, List<Tuple<string, int>>>();

            //Dictionary<字体名, 字符>
            Dictionary<string, string> texts = new Dictionary<string, string>();

            foreach (string assFile in assFiles) {
                AssParser parser = new AssParser();
                var result = parser.Parse(assFile);
                var _fonts = result.Item1;
                var _texts = result.Item2;

                foreach (var font in _fonts) {
                    string fontName = font.Key;
                    if (!fonts.ContainsKey(fontName)) {
                        fonts[fontName] = new List<Tuple<string, int>>();
                    }
                    foreach (int line in font.Value) {
                        fonts[fontName].Add(Tuple.Create(assFile, line));
                    }
                }

                foreach (var text in _texts) {
                    string fontName = text.Key;
                    if (!texts.ContainsKey(fontName)) {
                        texts[fontName] = "";
                    }
                    texts[fontName] += text.Value;
                }
            }
            var keys = new List<string>(texts.Keys);
            foreach (var key in keys) {
                texts[key] = new string(texts[key].Distinct().ToArray());
            }


            var subsetFonts = new List<SubsetFontInfo>();


            // Dictionary<字体名, List<Tuple<TTC字体轨道号, 文件名>>>
            var fontInfo = new Dictionary<string, List<Tuple<int, string>>>();

            var fontFiles = Directory.EnumerateFiles(fontFolder, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in fontFiles) {
                if (Path.GetExtension(file) == ".txt") {
                    continue;
                }

                int index = -1;
                string fontName = string.Empty;
                var fontFamilies = System.Windows.Media.Fonts.GetFontFamilies(file).ToList();
                for (index = 0; index < fontFamilies.Count; index++) {
                    var result = fontFamilies[index].FamilyNames.Values.Where(name => fonts.ContainsKey(name));
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

                if (!fontInfo.ContainsKey(fontName)) {
                    fontInfo[fontName] = new List<Tuple<int, string>>();
                }
                fontInfo[fontName].Add(Tuple.Create(index, file));
            }

            foreach (var text in texts) {
                var fontName = text.Key;
                var characters = text.Value;

                var tmpfile = $@"{fontFolder}\{fontName}.txt";
                using (StreamWriter sw = new StreamWriter(tmpfile, false, new UTF8Encoding(false))) {
                    sw.Write(characters);
                }

                if (!fontInfo.ContainsKey(fontName)) {
                    throw new Exception("字体未找到！" + fontName);
                }

                string randomName = this.RandomString(8);

                foreach (var tuple in fontInfo[fontName]) {
                    int index = tuple.Item1;
                    string fontFile = tuple.Item2;

                    string outputFile = string.Empty;
                    if (fontFile.EndsWith(".ttc")) {
                        outputFile = $"{outputFolder}\\{Path.GetFileNameWithoutExtension(fontFile)}.ttf";
                    } else {
                        outputFile = $"{outputFolder}\\{Path.GetFileName(fontFile)}";
                    }
                    string exe = "pyftsubset.exe";
                    string args =
                        $"\"{fontFile}\"" +
                        $" --text-file=\"{tmpfile}\"" +
                        $" --output-file=\"{outputFile}\"" +
                        $" --name-languages=0x0409";

                    if (index > -1) {
                        // ttc / otc, multi track
                        args += $" --font-number={index}";
                    }

                    subsetFonts.Add(new SubsetFontInfo {
                        FontNameInAss = fontName,
                        OriginalFontFile = fontFile,
                        SubsetFontFile = outputFile,
                        SubsetFontName = randomName,
                        TrackIndex = index
                    });

                    this.StartProcess(exe, args);
                }
            }

            foreach (var font in subsetFonts) {
                string exe = "ttx.exe";
                string args = $"-f \"{font.SubsetFontFile}\"";

                this.StartProcess(exe, args);
            }

            foreach (var font in subsetFonts) {
                var ttxFile = Path.GetDirectoryName(font.SubsetFontFile) + "\\" + Path.GetFileNameWithoutExtension(font.SubsetFontFile) + ".ttx";
                string ttxString = string.Empty;
                using (StreamReader sr = new StreamReader(ttxFile, new UTF8Encoding(false))) {
                    ttxString = sr.ReadToEnd();
                }

                XmlDocument xd = new XmlDocument();
                xd.LoadXml(ttxString);
                XmlNodeList namerecordList = xd.SelectNodes("ttFont/name/namerecord");

                string fontFamilyName = string.Empty;
                string uniqueFontIdentifier = string.Empty;
                string fullFontName = string.Empty;
                string postScriptName = string.Empty;

                foreach (XmlNode record in namerecordList) {
                    string nameID = record.Attributes["nameID"].Value;
                    string langID = record.Attributes["langID"].Value;
                    if (langID != "0x409") {
                        continue;
                    }

                    switch (nameID) {
                        case "1":
                        case "3":
                        case "4":
                        case "6":
                            record.InnerText = font.SubsetFontName;
                            break;
                        default:
                            break;
                    }
                }

                xd.Save(ttxFile);

                string exe = "ttx.exe";
                string args = $"-f \"{ttxFile}\"";
                this.StartProcess(exe, args);

                File.Delete(ttxFile);
            }

            foreach (string assFile in assFiles) {
                List<string> assContent = new List<string>();
                using (StreamReader sr = new StreamReader(assFile, true)) {
                    while (!sr.EndOfStream) {
                        assContent.Add(sr.ReadLine());
                    }
                }

                List<string> subsetComments = new List<string>();

                foreach (var kv in fonts) {
                    string fontName = kv.Key;
                    var subsetFontInfo = subsetFonts.Find(f => f.FontNameInAss == fontName);

                    foreach (var _ in kv.Value) {
                        if (_.Item1 != assFile) {
                            continue;
                        }
                        int line = _.Item2;

                        string row = assContent[line];
                        if (row.Substring(0, 6).ToLower() == "style:") {
                            assContent[line] = row.Replace(fontName, subsetFontInfo.SubsetFontName);
                        } else if (row.Substring(0, 9).ToLower() == "dialogue:") {
                            if (row.Contains($@"\fn{fontName}")) {
                                assContent[line] = row.Replace($@"\fn{fontName}", $@"\fn{subsetFontInfo.SubsetFontName}");
                            } else if (row.Contains($@"\fn@{fontName}")) {
                                assContent[line] = row.Replace($@"\fn@{fontName}", $@"\fn@{subsetFontInfo.SubsetFontName}");
                            }
                        }
                    }

                    string subsetComment = $"; Font Subset: {subsetFontInfo.SubsetFontName} - {fontName}";
                    if (!subsetComments.Contains(subsetComment)) {
                        subsetComments.Add(subsetComment);
                    }
                }

                int index = assContent.FindIndex(row => row.Length >= 13 && row.Substring(0, 13).ToLower() == "[script info]");
                assContent.Insert(index + 1, String.Join("\r\n", subsetComments));


                string newAssContent = string.Join("\r\n", assContent);
                using (StreamWriter sw = new StreamWriter(outputFolder + "\\" + Path.GetFileName(assFile), false, Encoding.UTF8)) {
                    sw.Write(newAssContent);
                }
            }



        }

        private void StartProcess(string exe, string args)
        {
            Console.WriteLine($"{exe} {args}");

            Process p = new Process {
                StartInfo = new ProcessStartInfo() {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false
                }
            };
            p.Start();
            p.WaitForExit();

            Console.WriteLine();
            Console.WriteLine();
        }

        public Random random = new Random();
        public string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effect = DragDropEffects.Copy;
            } else {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] assFile = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.listBox1.Items.AddRange(assFile);
            this.textBox2.Text = Path.GetDirectoryName(assFile[0]) + "\\fonts";
            this.textBox3.Text = Path.GetDirectoryName(assFile[0]) + "\\output";

            this.textBox2.Select(this.textBox2.Text.Length - 1, 0);
            this.textBox3.Select(this.textBox3.Text.Length - 1, 0);
        }
    }

}
