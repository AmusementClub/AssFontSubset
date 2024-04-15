using Mobsub.SubtitleParse;
using Mobsub.SubtitleParse.AssTypes;
using System.Text;

namespace AssFontSubset.Core;

public class AssFont
{
    public static Dictionary<AssFontInfo, List<Rune>> GetAssFonts(string file, out AssData ass)
    {
        ass = new AssData();
        ass.ReadAssFile(file);
        return AssFontParse.GetUsedFontInfos(ass.Events.Collection, ass.Styles.Collection);
    }

    public static bool IsMatch(AssFontInfo afi, FontInfo fi)
    {
        var assFn = afi.Name.StartsWith('@') ? afi.Name.AsSpan(1) : afi.Name.AsSpan();
        if ((assFn.SequenceEqual(fi.FamilyName.AsSpan()) || assFn.SequenceEqual(fi.FamilyNameChs.AsSpan()) )
            && (afi.Italic == fi.Italic || ((afi.Italic == true && !fi.MaybeHasTrueBoldOrItalic)))
            )
        {
            if (afi.Weight == 0)
            {
                return !fi.Bold;
            }
            else if (afi.Weight == 1)
            {
                return !fi.MaybeHasTrueBoldOrItalic || fi.Bold;
            }
            else if (afi.Weight == fi.Weight)
            {
                // Maybe wrong
                return true;
            }
        }
        return false;
    }
}
