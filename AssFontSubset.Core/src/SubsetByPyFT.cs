using Mobsub.SubtitleParse.AssTypes;
using Mobsub.SubtitleParse;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;
using System.Diagnostics;

namespace AssFontSubset.Core;

public class SubsetByPyFT(ILogger? logger = null)
{
    private readonly ILogger? _logger = logger;
    private static readonly Stopwatch _stopwatch = new();

    public async Task SubsetAsync(FileInfo[] path, DirectoryInfo? fontPath, DirectoryInfo? outputPath, DirectoryInfo? binPath, SubsetConfig subsetConfig)
    {
        var baseDir = path[0].Directory!.FullName;
        fontPath ??= new DirectoryInfo(Path.Combine(baseDir, "fonts"));
        outputPath ??= new DirectoryInfo(Path.Combine(baseDir, "output"));
        var pyftsubset = binPath is null ? "pyftsubset" : Path.Combine(binPath.FullName, "pyftsubset");
        var ttx = binPath is null ? "ttx" : Path.Combine(binPath.FullName, "ttx");

        foreach (var file in path)
        {
            if (!file.Exists)
            {
                throw new Exception($"请检查字体文件{file}是否存在");
            }
        }
        if (!fontPath.Exists) { throw new Exception($"请检查字体文件夹 {fontPath} 是否存在"); }
        if (outputPath.Exists) { outputPath.Delete(true); }
        var fontDir = fontPath.FullName;
        var optDir = outputPath.FullName;

        await Task.Run(() =>
        {
            var fontInfos = GetFontInfoFromFiles(fontDir);
            var pyFT = new PyFontTools(pyftsubset, ttx, _logger) { Config = subsetConfig, sw = _stopwatch };
            var assFonts = GetAssFontInfoFromFiles(path, optDir, out var assMulti);
            var subsetFonts = GetSubsetFonts(fontInfos, assFonts, out var fontMap);
            pyFT.SubsetFonts(subsetFonts, optDir, out var nameMap);

            foreach (var kv in assMulti)
            {
                ChangeAssFontName(kv.Value, nameMap, fontMap);
                kv.Value.WriteAssFile(kv.Key);
            }
        });
    }

    static void GetFontInfo(string fontFile)
    {
        var fp = new FontParse(fontFile);
        if (!fp.Open()) { throw new FileNotFoundException(); };

        var fontInfos = new Dictionary<string, string>[fp.GetNumFonts()];
        for (uint i = 0; i < fontInfos.Length; i++)
        {
            fontInfos[i] = FontParse.GetFontInfo(fp.GetFont(i)!);
        }
    }

    List<FontInfo> GetFontInfoFromFiles(string dir)
    {
        string[] supportFonts = [".ttf", ".otf", ".ttc", "otc"];
        List<FontInfo> fontInfos = [];
        HashSet<string> HasTrueBoldOrItalicRecord = [];

        _logger?.ZLogInformation($"开始扫描分析 {dir} 中的有效字体文件");
        _logger?.ZLogInformation($"支持的字体后缀名为：{string.Join(", ", supportFonts)}");
        _stopwatch.Start();

        foreach (string f in Directory.GetFiles(dir))
        {
            if (supportFonts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            {
                _logger?.ZLogInformation($"{f}");
                var fp = new FontParse(f);
                if (!fp.Open()) { throw new FormatException(); };
                for (uint i = 0; i < fp.GetNumFonts(); i++)
                {
                    fontInfos.Add(fp.GetFontInfo(i, HasTrueBoldOrItalicRecord));
                }
            }
        }
        _stopwatch.Stop();
        var pass1 = _stopwatch.ElapsedMilliseconds;
        _logger?.ZLogDebug($"初次扫描和解析完成，用时 {pass1} ms");
        //_stopwatch.Reset();
        _logger?.ZLogDebug($"开始分析记录可能有多种变体的 fontfamily");
        _stopwatch.Restart();
        for (var i = 0; i < fontInfos.Count; i++)
        {
            var info = fontInfos[i];
            if (!info.Bold && !info.Italic)
            {
                if (HasTrueBoldOrItalicRecord.Contains(info.FamilyName))
                {
                    info.MaybeHasTrueBoldOrItalic = true;
                    fontInfos[i] = info;
                    _logger?.ZLogDebug($"{info.FileName} 中的 {info.FamilyName} 检测到其他变体");
                }
                else
                {
                    string[] prefix = ["Arial", "Avenir Next", "Microsoft YaHei", "Source Han", "Noto", "Yu Gothic"];
                    if ((info.Weight == 500 && info.FamilyName.StartsWith("Avenir Next"))
                        || (info.Weight == 400 && (prefix.Any(info.FamilyName.StartsWith) || (info.FamilyName.StartsWith("FZ") && info.FamilyName.EndsWith("JF")) || (info.MaxpNumGlyphs < 6000 && (info.FamilyName == info.FamilyNameChs)))))
                    {
                        info.MaybeHasTrueBoldOrItalic = true;
                        fontInfos[i] = info;
                        _logger?.ZLogDebug($"{info.FileName} 中的 {info.FamilyName} 未在现有字体中检测到其他变体");
                    }
                }
            }
        }
        _stopwatch.Stop();
        _logger?.ZLogDebug($"变体分析完成，用时 {_stopwatch.ElapsedMilliseconds} ms");
        _logger?.ZLogInformation($"字体文件扫描完成，用时 {pass1 + _stopwatch.ElapsedMilliseconds} ms");
        _stopwatch.Reset();

        return fontInfos;
    }

    Dictionary<AssFontInfo, List<Rune>> GetAssFontInfoFromFiles(FileInfo[] assFiles, string optDir, out Dictionary<string, AssData> assDataWithOutputName)
    {
        assDataWithOutputName = [];
        Dictionary<AssFontInfo, List<Rune>> multiAssFonts = [];

        _logger?.ZLogInformation($"开始提取输入 ass 的字体信息");
        _stopwatch.Start();

        foreach (var assFile in assFiles)
        {
            _logger?.ZLogInformation($"{assFile.FullName}");
            var assFileNew = Path.Combine(optDir, assFile.Name);
            var assFonts = AssFont.GetAssFonts(assFile.FullName, out var ass);
            
            foreach (var kv in assFonts)
            {
                if (multiAssFonts.Count > 0 && multiAssFonts.TryGetValue(kv.Key, out List<Rune>? value))
                {
                    value.AddRange(kv.Value);
                }
                else
                {
                    multiAssFonts.Add(kv.Key, kv.Value);
                }
            }
            assDataWithOutputName.Add(assFileNew, ass);
        }

        foreach (var kv in multiAssFonts)
        {
            multiAssFonts[kv.Key] = kv.Value.Distinct().ToList();
        }

        _stopwatch.Stop();
        _logger?.ZLogInformation($"ass 字体信息提取完成，用时 {_stopwatch.ElapsedMilliseconds} ms");
        _stopwatch.Reset();
        return multiAssFonts;
    }

    Dictionary<string, List<SubsetFont>> GetSubsetFonts(List<FontInfo> fontInfos, Dictionary<AssFontInfo, List<Rune>> assFonts, out Dictionary<FontInfo, List<AssFontInfo>> fontMap)
    {
        _logger?.ZLogInformation($"开始生成子集字体信息");
        _stopwatch.Start();

        _logger?.ZLogDebug($"开始对字体文件信息与 ass 定义的字体进行匹配");
        fontMap = [];
        List<AssFontInfo> matchedAssFontInfos = [];
        foreach (FontInfo fontInfo in fontInfos)
        {
            foreach (var assFont in assFonts)
            {
                if (!matchedAssFontInfos.Contains(assFont.Key) && AssFont.IsMatch(assFont.Key, fontInfo))
                {
                    if (!fontMap.TryGetValue(fontInfo, out var _))
                    {
                        fontMap.Add(fontInfo, []);
                    }
                    fontMap[fontInfo].Add(assFont.Key);

                    matchedAssFontInfos.Add(assFont.Key);
                    _logger?.ZLogDebug($"{assFont.Key.ToString()} 匹配到了 {fontInfo.FileName} 的索引 {fontInfo.Index}");
                }
            }
        }
        _logger?.ZLogDebug($"匹配完成");

        if (matchedAssFontInfos.Count != assFonts.Keys.Count)
        {
            var NotFound = assFonts.Keys.Except(matchedAssFontInfos).ToList();
            throw new Exception($"Not found font file: {string.Join("、", NotFound.Select(x => x.ToString()))}");
        }

        _logger?.ZLogDebug($"开始把字体文件信息转换为子集字体信息");
        //List<SubsetFont> subsetFonts = [];
        Dictionary<string, List<SubsetFont>> subsetFonts = [];
        foreach (var kv in fontMap)
        {
            var runes = kv.Value.Count > 1 ? kv.Value.SelectMany(i => assFonts[i]).ToHashSet().ToList() : assFonts[kv.Value[0]];
            //subsetFonts.Add(new SubsetFont(new FileInfo(kv.Key.FileName), kv.Key.Index, runes) { OriginalFamilyName = kv.Key.FamilyName });

            if (!subsetFonts.TryGetValue(kv.Key.FamilyName, out var _))
            {
                subsetFonts.Add(kv.Key.FamilyName, []);
            }
            subsetFonts[kv.Key.FamilyName].Add(new SubsetFont(new FileInfo(kv.Key.FileName), kv.Key.Index, runes));
        }
        _logger?.ZLogDebug($"转换完成");

        _stopwatch.Stop();
        _logger?.ZLogInformation($"处理完成，用时 {_stopwatch.ElapsedMilliseconds} ms");
        _stopwatch.Reset();
        return subsetFonts;
    }

    static void ChangeAssFontName(AssData ass, Dictionary<string, string> nameMap, Dictionary<FontInfo, List<AssFontInfo>> fontMap)
    {
        // map ass font name and random name
        Dictionary<string, string> assFontNameMap = [];
        foreach (var (kv, kv2) in from kv in nameMap
                                  from kv2 in fontMap
                                  where kv2.Key.FamilyName == kv.Key
                                  select (kv, kv2))
        {
            foreach (var afi in kv2.Value)
            {
                assFontNameMap.TryAdd(afi.Name.StartsWith('@') ? afi.Name[1..] : afi.Name, kv.Value);
            }
        }

        foreach (var style in ass.Styles.Collection)
        {
            if (assFontNameMap.TryGetValue(style.Fontname, out var newFn))
            {
                style.Fontname = newFn;
            }
        }

        foreach (var evt in ass.Events.Collection)
        {
            if (!evt.IsDialogue) { continue; }
            for (var i = 0; i < evt.Text.Count; i++)
            {
                var span = evt.Text[i].AsSpan();
                if (SpanReplace(ref span, assFontNameMap))
                {
                    evt.Text[i] = span.ToArray();
                }
            }
        }

        List<string> subsetList = [];
        foreach (var kv in assFontNameMap)
        {
            subsetList.Add(string.Format("Font Subset: {0} - {1}", kv.Value, kv.Key));
        }
        subsetList.AddRange(ass.ScriptInfo.Comment);
        ass.ScriptInfo.Comment = subsetList;
    }

    private static bool SpanReplace(ref Span<char> span, Dictionary<string, string> nameMap)
    {
        if (!AssTagParse.IsOvrrideBlock(span)) { return false; }

        var changed = false;
        foreach (var kv in nameMap)
        {
            if (Utils.ReplaceFirst(ref span, kv.Key, kv.Value))
            {
                changed = true;
                break;
            }
        }
        return changed;
    }
}
