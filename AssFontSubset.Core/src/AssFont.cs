using Microsoft.Extensions.Logging;
using Mobsub.SubtitleParse;
using Mobsub.SubtitleParse.AssTypes;
using Mobsub.SubtitleParse.AssUtils;
using System.Text;
using ZLogger;

namespace AssFontSubset.Core;

public class AssFont
{
    public static Dictionary<AssFontInfo, HashSet<Rune>> GetAssFonts(string file, out AssData ass, ILogger? logger = null)
    {
        ass = new AssData(logger);
        ass.ReadAssFile(file);

        var anlz = new AssAnalyze(ass, logger);
        var undefinedStylesTemp = anlz.GetUndefinedStyles();
        HashSet<string> undefinedStyles = [];
        foreach (var und in undefinedStylesTemp)
        {
            if (und.StartsWith('*') && ass.Styles.Names.Contains(und.TrimStart('*')))
            {
                // vsfilter ingore starting asterisk
                logger?.ZLogWarning($"Style '{und}' should remove the starting asterisk");
                continue;
            }

            if (ass.Events.Collection.Where(x => x.Style == und).All(x => string.IsNullOrEmpty(x.Text)))
            {
                logger?.ZLogWarning($"Please check style '{und}', it may have been actually used but not defined");
                continue;
            }

            undefinedStyles.Add(und);
        }
        
        if (undefinedStyles.Count > 0)
        {
            throw new Exception($"Undefined styles in ass Styles section: {string.Join(", ", undefinedStyles)}");
        }

        return anlz.GetUsedFontInfos();
    }

    public static bool IsMatch(AssFontInfo afi, FontInfo fi, bool single, int? minimalWeight = null, bool? hadItalic = null, ILogger? logger = null)
    {
        var boldMatch = false;
        var italicMatch = false;
        if (!single) { if (minimalWeight is null || hadItalic is null) throw new ArgumentNullException(); }

        logger?.ZLogDebug($"Try match {afi.ToString()} and {string.Join('|', fi.FamilyNames.Values.Distinct())}_w{fi.Weight}_b{(fi.Bold ? 1 : 0)}_i{(fi.Italic ? 1 : 0)}");
        switch (afi.Weight)
        {
            case 0:
                boldMatch = fi.Bold ? single : true;    // cant get only true bold
                break;
            case 1:
                if (single)
                {
                    // The following cases exist:
                    // 1. Only the bold weight font file has been correctly matched
                    // 2. Font weight less than 550, get faux bold
                    // 3. Font weight great than or equal 550, \b1 is invalid
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
                    else if (!(fi.MaxpNumGlyphs < 6000 && !fi.FamilyNames.Keys.Any(key => key is 2052 or 1028 or 3076 or 5124)))
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
        if (!(assFn.SequenceEqual(fig.Key.AsSpan()) || fig.First().FamilyNames.ContainsValue(assFn.ToString()))) { return null; }

        if (fig.Count() == 1)
        {
            if (IsMatch(afi, fig.First(), true, null, null, logger)) { return fig.First(); }
            else { return null; }
        }
        else
        {
            var minimalWeight = fig.Select(fi => fi.Weight).Min();
            var hadItalic = fig.Select(fi => fi.Italic is true).Any();
            foreach (var fi in fig)
            {
                if (IsMatch(afi, fi, false, minimalWeight, hadItalic, logger)) { return fi; }
            }
            return null;
        }
    }
    
}
