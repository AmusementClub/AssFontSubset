﻿using Mobsub.SubtitleParse.AssTypes;
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
                throw new Exception($"Please check if font file {file} exists");
            }
        }
        if (!fontPath.Exists) { throw new Exception($"Please check if directory {fontPath} exists"); }
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

    List<FontInfo> GetFontInfoFromFiles(string dir)
    {
        string[] supportFonts = [".ttf", ".otf", ".ttc", "otc"];
        List<FontInfo> fontInfos = [];
        HashSet<string> HasTrueBoldOrItalicRecord = [];

        _logger?.ZLogInformation($"Start scan valid font files in {dir}");
        _logger?.ZLogInformation($"Support font file extension: {string.Join(", ", supportFonts)}");
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
                    fontInfos.Add(fp.GetFontInfo(i));
                }
            }
        }
        _stopwatch.Stop();
        _logger?.ZLogDebug($"Font file scanning completed, use {_stopwatch.ElapsedMilliseconds} ms");
        _stopwatch.Reset();
        return fontInfos;
    }

    Dictionary<AssFontInfo, List<Rune>> GetAssFontInfoFromFiles(FileInfo[] assFiles, string optDir, out Dictionary<string, AssData> assDataWithOutputName)
    {
        assDataWithOutputName = [];
        Dictionary<AssFontInfo, List<Rune>> multiAssFonts = [];

        _logger?.ZLogInformation($"Start parse font info from ass files");
        _stopwatch.Start();

        foreach (var assFile in assFiles)
        {
            //_logger?.ZLogInformation($"{assFile.FullName}");
            var assFileNew = Path.Combine(optDir, assFile.Name);
            var assFonts = AssFont.GetAssFonts(assFile.FullName, out var ass, _logger);
            
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
        _logger?.ZLogInformation($"Ass font info parsing completed, use {_stopwatch.ElapsedMilliseconds} ms");
        _stopwatch.Reset();
        return multiAssFonts;
    }

    Dictionary<string, List<SubsetFont>> GetSubsetFonts(List<FontInfo> fontInfos, Dictionary<AssFontInfo, List<Rune>> assFonts, out Dictionary<FontInfo, List<AssFontInfo>> fontMap)
    {
        _logger?.ZLogInformation($"Start generate subset font info");
        _stopwatch.Start();

        _logger?.ZLogDebug($"Start match font file info and ass font info");
        fontMap = [];
        List<AssFontInfo> matchedAssFontInfos = [];

        var fiGroups = fontInfos.GroupBy(fontInfo => fontInfo.FamilyName);
        foreach (var fig in fiGroups)
        {
            foreach (var afi in assFonts.Keys)
            {
                if (matchedAssFontInfos.Contains(afi)) { continue; }
                var _fontInfo = AssFont.GetMatchedFontInfo(afi, fig, _logger);
                if (_fontInfo == null) { continue; }
                var fontInfo = (FontInfo) _fontInfo;

                if (!fontMap.TryGetValue(fontInfo, out var _))
                {
                    fontMap.Add(fontInfo, []);
                }
                fontMap[fontInfo].Add(afi);

                matchedAssFontInfos.Add(afi);
                _logger?.ZLogDebug($"{afi.ToString()} match {fontInfo.FileName} index {fontInfo.Index}");
            }
        }
        _logger?.ZLogDebug($"Match completed");

        if (matchedAssFontInfos.Count != assFonts.Keys.Count)
        {
            var NotFound = assFonts.Keys.Except(matchedAssFontInfos).ToList();
            throw new Exception($"Not found font file: {string.Join("、", NotFound.Select(x => x.ToString()))}");
        }

        _logger?.ZLogDebug($"Start convert font file info to subset font info");
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
        _logger?.ZLogDebug($"Convert completed");

        _stopwatch.Stop();
        _logger?.ZLogInformation($"Generate completed, use {_stopwatch.ElapsedMilliseconds} ms");
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
