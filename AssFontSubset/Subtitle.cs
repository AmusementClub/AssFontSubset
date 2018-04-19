using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace AssFontSubset
{
    enum Section
    {
        None,
        Style,
        Event
    }

    struct Font
    {
        public string StyleName;
        public string FontName;
        public int Line;
    }

    struct CustomFontTextInfo
    {
        public string Text;
        public int Line;
        public CustomFontTextInfo(string text, int line)
        {
            this.Text = text;
            this.Line = line;
        }
    }

    class AssParser
    {
        public Dictionary<string, Font> Fonts;
        public Dictionary<Font, List<string>> SubtitlesByFont;

        public Dictionary<string, int> StyleFormat;
        public Dictionary<string, int> EventFormat;
        public Dictionary<string, List<CustomFontTextInfo>> CustomFontText;

        public int CurrentLine;
        public string AssFile;

        public AssParser()
        {
            this.Fonts = new Dictionary<string, Font>();
            this.SubtitlesByFont = new Dictionary<Font, List<string>>();
            this.StyleFormat = new Dictionary<string, int>();
            this.EventFormat = new Dictionary<string, int>();
            this.CustomFontText = new Dictionary<string, List<CustomFontTextInfo>>();
            this.CurrentLine = -1;
        }

        public void ParseStyleFormat(string value)
        {
            value = value.Trim().ToLower();
            string[] array = value.Split(',');
            for (int i = 0; i < array.Length; i++) {
                this.StyleFormat[array[i].Trim().ToLower()] = i;
            }
        }

        public void ParseEventFormat(string value)
        {
            value = value.Trim().ToLower();
            string[] array = value.Split(',');
            for (int i = 0; i < array.Length; i++) {
                this.EventFormat[array[i].Trim().ToLower()] = i;
            }

            if (this.EventFormat.Last().Key != "text") {
                this.RaiseException("[Events] Format 中 Text 不在最后面，处理不了");
            }
        }

        public void ParseStyle(string value)
        {
            if (this.StyleFormat.Count == 0) {
                this.RaiseException("ASS 无法处理, [Styles] Format 不存在");
            } else if (!this.StyleFormat.ContainsKey("name")) {
                this.RaiseException("ASS 无法处理, [Styles] Format 中不存在 Name 字段");
            } else if (!this.StyleFormat.ContainsKey("fontname")) {
                this.RaiseException("ASS 无法处理, [Styles] Format 中不存在 Fontname 字段");
            }

            int styleNameIndex = this.StyleFormat["name"];
            int fontNameIndex = this.StyleFormat["fontname"];

            string[] array = value.Split(',');
            if (array.Length <= styleNameIndex || array.Length <= fontNameIndex) {
                this.RaiseException("ASS 无法处理");
            }

            string fontName = array[fontNameIndex].Trim();
            string styleName = array[styleNameIndex].Trim().ToLower();

            if (fontName[0] == '@') {
                fontName = fontName.Substring(1);
            }
            if (string.IsNullOrEmpty(fontName)) {
                this.RaiseException("ASS 无法处理, Style 中字体名为空");
            }

            Font font = new Font {
                FontName = fontName,
                StyleName = styleName,
                Line = this.CurrentLine
            };
            if (this.Fonts.ContainsKey(styleName)) {
                this.RaiseException("Style 二次定义");
            }
            this.Fonts.Add(styleName, font);
        }

        public void ParseDialogue(string value)
        {
            if (this.EventFormat.Count == 0) {
                this.RaiseException("ASS 无法处理, [Events] Format 不存在");
            } else if (!this.EventFormat.ContainsKey("style")) {
                this.RaiseException("ASS 无法处理, [Events] Format 中不存在 Style 字段");
            } else if (!this.EventFormat.ContainsKey("text")) {
                this.RaiseException("ASS 无法处理, [Events] Format 中不存在 Text 字段");
            }

            int styleIndex = this.EventFormat["style"];
            int textIndex = this.EventFormat["text"];

            string[] array = value.Split(',');
            if (array.Length <= styleIndex || array.Length <= textIndex) {
                this.RaiseException("ASS 无法处理");
            }

            string styleName = array[styleIndex].ToLower();

            if (!this.Fonts.ContainsKey(styleName)) {
                if (!this.Fonts.ContainsKey("default")) {
                    this.RaiseException($"ASS 无法处理, Style {array[styleIndex]} 不存在，Default Style 也不存在");
                }
                styleName = "default";
            }

            Font font = this.Fonts[styleName];
            string text = string.Join("", array.Skip(textIndex));

            string parsedText = this.ParseText(text);
            if (!string.IsNullOrEmpty(parsedText)) {
                if (!this.SubtitlesByFont.ContainsKey(font)) {
                    this.SubtitlesByFont[font] = new List<string>();
                }
                this.SubtitlesByFont[font].Add(parsedText);
            }
        }

        public string ParseText(string text)
        {
            text = text.Replace(@"\n", "");
            text = text.Replace(@"\N", "");

            if (text.Contains(@"\fn")) {
                this.ParseTextWithCustomFont(text);
            }

            text = Regex.Replace(text, @"\{\\[nNbiusfcakqrtmpo1234].*?\}", "", RegexOptions.Compiled);

            return text;
        }

        public string ParseTextWithCustomFont(string text)
        {
            bool overrideBegin = false;
            bool overrideEnd = false;
            bool overrideFontBegin = false;
            bool overrideFontEnd = false;
            bool fontDetected = false;
            string currentFontName = "";
            string textByFont = "";

            for (int i = 0; i < text.Length; i++) {
                char ch = text[i];
                if (ch == '{') {
                    overrideBegin = true;
                    overrideEnd = false;
                    continue;
                }

                if (ch == '}' && overrideBegin) {
                    overrideBegin = false;
                    overrideEnd = true;

                    if (!fontDetected) {
                        overrideFontBegin = false;
                        overrideFontEnd = true;
                        i += 1;
                        continue;
                    }

                    continue;
                }

                if (overrideBegin) {
                    if (ch == '\\' && text[i + 1] == 'f' && text[i + 2] == 'n') {
                        overrideFontBegin = true;
                        overrideFontEnd = false;
                        fontDetected = false;

                        this.AddCustomFontText(currentFontName, textByFont);

                        currentFontName = "";
                        textByFont = "";

                        i += 2;
                        continue;
                    }

                    if (overrideFontBegin && ch == '\\' && text[i + 1] == 'r') {
                        overrideFontBegin = false;
                        overrideFontEnd = true;
                        fontDetected = false;

                        this.AddCustomFontText(currentFontName, textByFont);

                        currentFontName = "";
                        textByFont = "";
                        i += 1;
                        continue;
                    }
                }

                if (overrideFontBegin && !overrideFontEnd) {
                    if (!fontDetected) {
                        int step = -1;
                        for (int j = i; j < text.Length; j++, step++) {
                            if (text[j] == '}' || text[j] == '\\') {
                                break;
                            }

                            currentFontName += text[j];
                        }
                        fontDetected = true;
                        i += step;
                        continue;
                    }
                }

                if (overrideEnd && fontDetected) {
                    textByFont += text[i];
                }
            }

            this.AddCustomFontText(currentFontName, textByFont);

            return null;
        }

        public void AddCustomFontText(string currentFontName, string textByFont)
        {
            if (!string.IsNullOrEmpty(currentFontName) && currentFontName[0] == '@') {
                currentFontName = currentFontName.Substring(1);
            }
            if (!string.IsNullOrEmpty(currentFontName) && !string.IsNullOrEmpty(textByFont)) {
                if (!this.CustomFontText.ContainsKey(currentFontName)) {
                    this.CustomFontText[currentFontName] = new List<CustomFontTextInfo>();
                }
                this.CustomFontText[currentFontName].Add(new CustomFontTextInfo(textByFont, this.CurrentLine));
            }
        }

        public Tuple<Dictionary<string, List<int>>, Dictionary<string, string>> Parse(string file)
        {
            List<Tuple<int, string>> dialogues = new List<Tuple<int, string>>();

            List<string> assContent = new List<string>();
            using (StreamReader sr = new StreamReader(file, true)) {
                while (!sr.EndOfStream) {
                    assContent.Add(sr.ReadLine());
                }
            }

            this.AssFile = file;

            Section section = Section.None;

            foreach (string content in assContent) {
                this.CurrentLine++;
                string row = content.Trim();

                if (string.IsNullOrEmpty(row)) {
                    continue;
                }
                if (row[0] == ';') {
                    continue;
                }

                if (row[0] == '[') {
                    switch (row.ToLower()) {
                        case "[v4+ styles]":
                        case "[v4 styles+]":
                        case "[v4+styles]":
                        case "[v4styles+]":
                            section = Section.Style;
                            continue;
                        case "[events]":
                            section = Section.Event;
                            continue;
                        default:
                            section = Section.None;
                            break;
                    }
                    continue;
                }

                string type = row.Substring(0, row.IndexOf(':')).Trim().ToLower();
                string value = row.Substring(row.IndexOf(':') + 1);

                switch (section) {
                    case Section.Style:
                        switch (type) {
                            case "format":
                                this.ParseStyleFormat(value);
                                continue;
                            case "style":
                                this.ParseStyle(value);
                                continue;
                            default:
                                continue;
                        }
                    case Section.Event:
                        switch (type) {
                            case "format":
                                this.ParseEventFormat(value);
                                continue;
                            case "dialogue":
                                dialogues.Add(Tuple.Create(this.CurrentLine, value));
                                continue;
                            default:
                                continue;
                        }
                    default:
                        continue;
                }
            }

            foreach (var item in dialogues) {
                this.CurrentLine = item.Item1;
                this.ParseDialogue(item.Item2);
            }

            Dictionary<string, List<int>> fonts = new Dictionary<string, List<int>>();
            Dictionary<string, string> texts = new Dictionary<string, string>();

            foreach (var kv in this.SubtitlesByFont) {
                if (!fonts.ContainsKey(kv.Key.FontName)) {
                    fonts[kv.Key.FontName] = new List<int>();
                }
                fonts[kv.Key.FontName].Add(kv.Key.Line);
            }
            foreach (var item in this.CustomFontText) {
                if (!fonts.ContainsKey(item.Key)) {
                    fonts[item.Key] = new List<int>();
                }
                foreach (var text in item.Value) {
                    fonts[item.Key].Add(text.Line);
                }
            }


            foreach (var kv in this.SubtitlesByFont) {
                Font font = kv.Key;
                if (!texts.ContainsKey(font.FontName)) {
                    texts[font.FontName] = "";
                }
                texts[font.FontName] += string.Join("", kv.Value);
            }
            foreach (var item in this.CustomFontText) {
                if (!texts.ContainsKey(item.Key)) {
                    texts[item.Key] = "";
                }
                texts[item.Key] += string.Join("", item.Value.Select(f => f.Text));
            }

            var keys = new List<string>(texts.Keys);
            foreach (var key in keys) {
                texts[key] = new string(texts[key].Distinct().ToArray());
            }

            return Tuple.Create(fonts, texts);
        }

        public void RaiseException(string message)
        {
            throw new Exception($"{message}\r\n文件：{Path.GetFileName(this.AssFile)}\r\n行：{this.CurrentLine}");
        }
    }
}
