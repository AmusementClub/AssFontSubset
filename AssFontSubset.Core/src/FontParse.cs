using Mobsub.Helper.Font;

namespace AssFontSubset.Core;

public struct FontInfo
{
    public string FamilyName;
    public string FamilyNameChs;
    //public bool Regular;
    public bool Bold;
    public bool Italic;
    public int Weight;
    //public bool MaybeHasTrueBoldOrItalic;
    public string FileName;
    public uint Index;
    public ushort MaxpNumGlyphs;

    public override bool Equals(object? obj)
    {
        return obj is FontInfo info &&
               FamilyName == info.FamilyName &&
               FamilyNameChs == info.FamilyNameChs &&
               //Regular == info.Regular &&
               Bold == info.Bold &&
               Italic == info.Italic &&
               Weight == info.Weight &&
               //MaybeHasTrueBoldOrItalic == info.MaybeHasTrueBoldOrItalic &&
               FileName == info.FileName &&
               Index == info.Index &&
               MaxpNumGlyphs == info.MaxpNumGlyphs;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(FamilyName);
        hash.Add(FamilyNameChs);
        //hash.Add(Regular);
        hash.Add(Bold);
        hash.Add(Italic);
        hash.Add(Weight);
        //hash.Add(MaybeHasTrueBoldOrItalic);
        hash.Add(FileName);
        hash.Add(Index);
        hash.Add(MaxpNumGlyphs);
        return hash.ToHashCode();
    }

    public static bool operator ==(FontInfo lhs, FontInfo rhs) => lhs.Equals(rhs);
    public static bool operator !=(FontInfo lhs, FontInfo rhs) => !lhs.Equals(rhs);
}

public static class FontParse
{
    public static List<FontInfo> GetFontInfos(DirectoryInfo dirInfo)
    {
        List<FontInfo> fontInfos = [];
        var fileInfos = dirInfo.GetFiles();
        var faceInfos = OpenType.GetLocalFontsInfo(fileInfos);

        foreach (var faceInfo in faceInfos)
        {
            fontInfos.Add(ConvertToFontInfo(faceInfo));
        }

        return fontInfos;
    }

    private static FontInfo ConvertToFontInfo(FontFaceInfoBase faceInfo)
    {
        var info = (FontFaceInfoOpenType)faceInfo;
        var fsSel = info.fsSelection;
        return new FontInfo
        {
            FamilyName = info.FamilyNameGdi!,
            FamilyNameChs = info.FamilyNameGdiChs!,
            //Regular = ((fsSel & 0b_0100_0000) >> 6) == 1,   // bit 6
            Bold = ((fsSel & 0b_0010_0000) >> 5) == 1, // bit 5
            Italic = (fsSel & 0b_1) == 1, // bit 0
            Weight = info.Weight,
            //MaybeHasTrueBoldOrItalic = false,
            FileName = info.FileInfo!.FilePath!,
            Index = info.FaceIndex,
            MaxpNumGlyphs = info.MaxpNumGlyphs,
        };
    }
}
