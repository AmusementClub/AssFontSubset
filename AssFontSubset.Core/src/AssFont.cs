using Microsoft.Extensions.Logging;
using Mobsub.SubtitleParse;
using Mobsub.SubtitleParse.AssTypes;
using System.Text;
using ZLogger;

namespace AssFontSubset.Core;

public class AssFont
{
    public static Dictionary<AssFontInfo, List<Rune>> GetAssFonts(string file, out AssData ass, ILogger? logger = null)
    {
        ass = new AssData(logger);
        ass.ReadAssFile(file);

        var usedStyles = GetUsedStyles(ass.Events.Collection);
        var undefinedStyles = new HashSet<string>(usedStyles);
        undefinedStyles.ExceptWith(ass.Styles.Names);
        if (undefinedStyles.Count > 0)
        {
            throw new Exception($"以下样式未在 Styles 中定义：{string.Join(", ", undefinedStyles)}");
        }

        return AssFontParse.GetUsedFontInfos(ass.Events.Collection, ass.Styles.Collection);
    }

    public static bool IsMatch(AssFontInfo afi, FontInfo fi, bool single, int? minimalWeight = null, bool? hadItalic = null, ILogger? logger = null)
    {
        var boldMatch = false;
        var italicMatch = false;
        if (!single) { if (minimalWeight is null || hadItalic is null) throw new ArgumentNullException(); }

        logger?.ZLogDebug($"Try match {afi.ToString()} and {fi.FamilyName}_w{fi.Weight}_b{(fi.Bold ? 1 : 0)}_i{(fi.Italic ? 1 : 0)}");
        switch (afi.Weight)
        {
            case 0:
                boldMatch = fi.Bold ? single : true;    // cant get only true bold
                break;
            case 1:
                if (single)
                {
                    // maybe faux bold
                    if (fi.Weight >= 550) { logger?.ZLogWarning($"{afi.Name} use \\b1 will not get faux bold"); }
                    boldMatch = true;
                }
                else
                {
                    // strict
                    boldMatch = fi.Bold;
                }
                break;
            default:
                if (afi.Weight == fi.Weight)
                {
                    boldMatch = true;
                }
                else
                {
                    if (fi.Weight > (afi.Weight + 150)) { logger?.ZLogDebug($"{afi.Name} should use \\b{fi.Weight}"); }
                }
                break;
        }

        if (afi.Italic)
        {
            if (fi.Italic)
            {
                italicMatch = true;
            }
            else
            {
                // maybe faux italic
                if (single) { italicMatch = true; }
                else
                {
                    if (!(bool)hadItalic!) { italicMatch = true; }
                    else if (!(fi.MaxpNumGlyphs < 6000 && fi.FamilyName == fi.FamilyNameChs))
                    {
                        // maybe cjk fonts
                        italicMatch = true;
                        logger?.ZLogDebug($"{afi.Name} use \\i1 maybe get faux italic");
                    }
                }
            }
        }
        else
        {
            if (!fi.Italic) { italicMatch = true; }
        }

        return boldMatch && italicMatch;
    }

    public static FontInfo? GetMatchedFontInfo(AssFontInfo afi, IGrouping<string, FontInfo> fig, ILogger? logger = null)
    {
        var assFn = afi.Name.StartsWith('@') ? afi.Name.AsSpan(1) : afi.Name.AsSpan();
        if (!(assFn.SequenceEqual(fig.Key.AsSpan()) || assFn.SequenceEqual(fig.First().FamilyNameChs.AsSpan()))) { return null; }

        if (fig.Count() == 1)
        {
            if (IsMatch(afi, fig.First(), true, null, null, logger)) { return fig.First(); }
            else { return null; }
        }
        else
        {
            var minimalWeight = fig.Select(fig => fig.Weight).Min();
            var hadItalic = fig.Select(fig => fig.Italic is true).Count() > 0;
            foreach (var fi in fig)
            {
                if (IsMatch(afi, fi, false, minimalWeight, hadItalic, logger)) { return fi; }
            }
            return null;
        }
    }

    private static HashSet<string> GetUsedStyles(List<AssEvent> events)
    {
        var styles = new HashSet<string>();
        var str = new StringBuilder();
        foreach (var et in events)
        {
            if (et.IsDialogue)
            {
                var text = et.Text.ToArray();

                styles.Add(et.Style);

                char[] block = [];
                for (var i = 0; i < text.Length; i++)
                {
                    block = text[i];
                    if (block[0] == '{' && block[^1] == '}' && block.Length > 2 && i != text.Length - 1)
                    {
                        foreach (var ca in AssTagParse.GetTagsFromOvrBlock(block))
                        {
                            if (ca[0] == 'r' && ca.Length > 1 && ca.Length >= 3 && !ca.AsSpan()[..3].SequenceEqual("rnd".AsSpan()))
                            {
                                styles.Add(new string(ca[1..]));
                            }
                        }
                    }
                }
            }
        }
        return styles;
    }
}
