using Microsoft.Extensions.Logging;
using Mobsub.SubtitleParse;
using Mobsub.SubtitleParse.AssTypes;
using System.Text;

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

    public static bool IsMatch(AssFontInfo afi, FontInfo fi)
    {
        var boldMatch = false;
        var italicMatch = false;

        var assFn = afi.Name.StartsWith('@') ? afi.Name.AsSpan(1) : afi.Name.AsSpan();
        if ((assFn.SequenceEqual(fi.FamilyName.AsSpan()) || assFn.SequenceEqual(fi.FamilyNameChs.AsSpan())))
        {
            if (afi.Weight == 0)
            {
                boldMatch = !fi.Bold;
            }
            else if (afi.Weight == 1)
            {
                boldMatch = fi.Bold || (!fi.MaybeHasTrueBoldOrItalic && !fi.Bold && !fi.Italic);
            }
            else if (afi.Weight == fi.Weight)
            {
                // Maybe wrong
                boldMatch = true;
            }

            if (afi.Italic == fi.Italic)
            {
                italicMatch = true;
            }
            else if (afi.Italic == true && (!fi.MaybeHasTrueBoldOrItalic && !fi.Bold && !fi.Italic))
            {
                italicMatch = true;
            }
            else if (afi.Italic == true && fi.MaybeHasTrueBoldOrItalic && fi.FamilyName != fi.FamilyNameChs)
            {
                italicMatch = true;
            }
        }

        return boldMatch && italicMatch;
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
