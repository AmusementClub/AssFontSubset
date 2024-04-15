using Mobsub.SubtitleParse.AssTypes;
using Mobsub.SubtitleParse;
using System.Text;
using System.Threading.Tasks;

namespace AssFontSubset.Core;

public class SubsetByPyFT
{
    public static void Subset(FileInfo[] path, DirectoryInfo? fontPath, DirectoryInfo? outputPath, DirectoryInfo? binPath, bool sourceHanEllipsis, bool debug)
    {
        var subsetConfig = new SubsetConfig
        {
            SourceHanEllipsis = sourceHanEllipsis,
            DebugMode = debug,
        };

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

        var fontInfos = GetFontInfoFromFiles(fontDir);
        var pyFT = new PyFontTools(pyftsubset, ttx) { Config = subsetConfig };

        Dictionary<string, AssData> assMulti = [];
        List<Dictionary<AssFontInfo, List<Rune>>> assMultiFonts = [];
        foreach (var assFile in path)
        {
            var assFileNew = Path.Combine(optDir, assFile.Name);
            var assFonts = AssFont.GetAssFonts(assFile.FullName, out var ass);
            assMultiFonts.Add(assFonts);
            assMulti.Add(assFileNew, ass);
            //var subsetFonts = GetSubsetFonts(fontInfos, assFonts, out var fontMap);
            //pyFT.SubsetFonts(subsetFonts, optDir, out var nameMap);
            //ChangeAssFontName(ass, nameMap, fontMap);
            //ass.WriteAssFile(assFileNew);
        }

        for (var i = 1; i < assMultiFonts.Count; i++)
        {
            foreach (var kv in assMultiFonts[i])
            {
                if (assMultiFonts[0].TryGetValue(kv.Key, out List<Rune>? value))
                {
                    value.AddRange(kv.Value);
                }
                else
                {
                    assMultiFonts[0].Add(kv.Key, kv.Value);
                }
            }
        }

        foreach (var kv in assMultiFonts[0])
        {
            assMultiFonts[0][kv.Key] = kv.Value.Distinct().ToList();
        }
        var subsetFonts = GetSubsetFonts(fontInfos, assMultiFonts[0], out var fontMap);
        pyFT.SubsetFonts(subsetFonts, optDir, out var nameMap);

        foreach (var kv in assMulti)
        {
            ChangeAssFontName(kv.Value, nameMap, fontMap);
            kv.Value.WriteAssFile(kv.Key);
        }

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

    static List<FontInfo> GetFontInfoFromFiles(string dir)
    {
        string[] supportFonts = [".ttf", ".otf", ".ttc", "otc"];
        List<FontInfo> fontInfos = [];
        HashSet<string> HasTrueBoldOrItalicRecord = [];

        foreach (string f in Directory.GetFiles(dir))
        {
            if (supportFonts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            {
                var fp = new FontParse(f);
                if (!fp.Open()) { throw new FormatException(); };
                for (uint i = 0; i < fp.GetNumFonts(); i++)
                {
                    fontInfos.Add(fp.GetFontInfo(i, HasTrueBoldOrItalicRecord));
                }
            }
        }

        for (var i = 0; i < fontInfos.Count; i++)
        {
            var info = fontInfos[i];
            if (!info.Bold && !info.Italic)
            {
                if (HasTrueBoldOrItalicRecord.Contains(info.FamilyName))
                {
                    info.MaybeHasTrueBoldOrItalic = true;
                    fontInfos[i] = info;
                }
                else
                {
                    string[] prefix = ["Arial", "Avenir Next", "Microsoft YaHei", "Source Han", "Noto", "Yu Gothic"];
                    if ((info.Weight == 500 && info.FamilyName.StartsWith("Avenir Next"))
                        || (info.Weight == 400 && (prefix.Any(info.FamilyName.StartsWith) || (info.FamilyName.StartsWith("FZ") && info.FamilyName.EndsWith("JF")) || (info.MaxpNumGlyphs < 6000 && (info.FamilyName == info.FamilyNameChs)))))
                    {
                        info.MaybeHasTrueBoldOrItalic = true;
                        fontInfos[i] = info;
                    }
                }
            }
        }

        return fontInfos;
    }

    static Dictionary<string, List<SubsetFont>> GetSubsetFonts(List<FontInfo> fontInfos, Dictionary<AssFontInfo, List<Rune>> assFonts, out Dictionary<FontInfo, List<AssFontInfo>> fontMap)
    {
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
                }
            }
        }

        if (matchedAssFontInfos.Count != assFonts.Keys.Count)
        {
            var NotFound = assFonts.Keys.Except(matchedAssFontInfos).ToList();
            throw new Exception($"Not found font file: {string.Join("、", NotFound.Select(x => x.ToString()))}");
        }

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
