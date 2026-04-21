using Microsoft.Extensions.Logging;
using Mobsub.SubtitleParse;
using Mobsub.SubtitleParse.AssText;
using Mobsub.SubtitleParse.AssTypes;
using System.Text;
using ZLogger;

namespace AssFontSubset.Core;

public class AssFont
{
    public static Dictionary<AssFontInfo, HashSet<Rune>> GetAssFonts(string file, out AssData ass, ILogger? logger = null)
    {
        ass = new AssData(logger, AssParseTarget.ParseAssFontsInfo);
        ass.ReadAssFile(file);

        HashSet<string> undefinedStyles = [];
        foreach (var und in GetUndefinedStyles(ass))
        {
            if (und.StartsWith('*') && ass.Styles.Names.Contains(und.TrimStart('*')))
            {
                // vsfilter ingore starting asterisk
                logger?.ZLogWarning($"Style '{und}' should remove the starting asterisk");
                continue;
            }

            if (ass.Events!.Collection.Where(x => x.Style == und).All(x => string.IsNullOrEmpty(x.Text)))
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

        var processor = ass.Processor as AssFontProcessor ?? throw new InvalidOperationException("Ass font processor is unavailable.");
        return NormalizeAssFontInfos(processor.Results);
    }

    private static Dictionary<AssFontInfo, HashSet<Rune>> NormalizeAssFontInfos(IReadOnlyDictionary<AssFontInfo, HashSet<Rune>> assFonts)
    {
        Dictionary<AssFontInfo, HashSet<Rune>> normalized = [];
        foreach (var (fontInfo, runes) in assFonts)
        {
            var key = fontInfo;
            key.Encoding = 1;

            if (normalized.TryGetValue(key, out var value))
            {
                value.UnionWith(runes);
            }
            else
            {
                normalized[key] = [.. runes];
            }
        }

        return normalized;
    }

    private static HashSet<string> GetUndefinedStyles(AssData ass)
    {
        HashSet<string> usedStyles = [];
        if (ass.Events is null)
        {
            return usedStyles;
        }

        foreach (var evt in ass.Events.Collection)
        {
            if (evt.IsDialogue)
            {
                usedStyles.Add(evt.Style);
            }
        }

        usedStyles.UnionWith(GetResetStyles(ass));
        usedStyles.ExceptWith(ass.Styles.Names);
        return usedStyles;
    }

    private static HashSet<string> GetResetStyles(AssData ass)
    {
        HashSet<string> resetStyles = [];
        if (ass.Events is null)
        {
            return resetStyles;
        }

        foreach (var evt in ass.Events.Collection)
        {
            if (!evt.IsDialogue || evt.TextSpan.IsEmpty)
            {
                continue;
            }

            AssEventTextParser.WithParsedSegments(evt.TextSpan, segments =>
            {
                foreach (var segment in segments)
                {
                    if (segment.SegmentKind != AssEventSegmentKind.TagBlock || !segment.Tags.HasValue)
                    {
                        continue;
                    }

                    foreach (var tag in segment.Tags.Value.Span)
                    {
                        if (tag.Tag == AssTag.Reset && tag.TryGet<ReadOnlyMemory<byte>>(out var styleName) && !styleName.IsEmpty)
                        {
                            resetStyles.Add(Encoding.UTF8.GetString(styleName.Span));
                        }
                    }
                }
            });
        }

        return resetStyles;
    }

    public static bool IsMatch(AssFontInfo afi, FontInfo fi, bool single, int? minimalWeight = null, int? maximumWeight = null, bool? hadItalic = null, bool? hadTrueBold = null, ILogger? logger = null)
    {
        var boldMatch = false;
        var italicMatch = false;
        if (!single) { if (minimalWeight is null || maximumWeight is null || hadItalic is null || hadTrueBold is null) throw new ArgumentNullException(); }

        logger?.ZLogDebug($"Try match {afi.ToString()} and {string.Join('|', fi.FamilyNames.Values.Distinct())}_w{fi.Weight}_b{(fi.Bold ? 1 : 0)}_i{(fi.Italic ? 1 : 0)}");
        switch (afi.Weight)
        {
            case 0:
                if (single)
                {
                    // A single face has no alternative; bold/regular is renderer-dependent (true vs faux).
                    boldMatch = true;
                }
                else
                {
                    // Prefer metadata-driven style-linking first; fall back to weight when the flags are broken.
                    boldMatch = (bool)hadTrueBold! ? !fi.Bold : ((int)minimalWeight! < 550 ? fi.Weight < 550 : true);
                }
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
                    // Prefer the declared bold face when available; otherwise fall back to weight class.
                    boldMatch = (bool)hadTrueBold! ? fi.Bold : ((int)maximumWeight! >= 550 ? fi.Weight >= 550 : true);
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
        var assFnStr = assFn.ToString();

        var fonts = fig.ToList();
        if (fonts.Count == 0) { return null; }

        var isFamilyMatch = assFn.SequenceEqual(fig.Key.AsSpan()) || fonts[0].FamilyNames.ContainsValue(assFnStr);
        List<FontInfo> candidates;
        if (isFamilyMatch)
        {
            candidates = fonts;
        }
        else
        {
            candidates = fonts.Where(fi => fi.MatchNames?.Contains(assFnStr) == true).ToList();
            if (candidates.Count == 0) { return null; }

            // A face-specific full name should bind directly to that face.
            if (candidates.Count == 1 && fonts.Count > 1) { return candidates[0]; }
        }

        if (candidates.Count == 1)
        {
            var fi = candidates[0];
            return IsMatch(afi, fi, true, null, null, null, null, logger) ? fi : null;
        }

        var targetWeight = afi.Weight switch
        {
            0 => 400,
            1 => 700,
            _ => afi.Weight,
        };

        var minimalWeight = candidates.Select(fi => fi.Weight).Min();
        var maximumWeight = candidates.Select(fi => fi.Weight).Max();
        var hadItalic = candidates.Any(fi => fi.Italic);
        var hadTrueBold = candidates.Any(fi => fi.Bold);

        var sorted = candidates
            .OrderBy(fi => fi.Italic == afi.Italic ? 0 : 1)
            .ThenBy(fi => Math.Abs(fi.Weight - targetWeight));

        foreach (var fi in sorted)
        {
            if (IsMatch(afi, fi, false, minimalWeight, maximumWeight, hadItalic, hadTrueBold, logger)) { return fi; }
        }

        return null;
    }
}
