using OTFontFile;
using static OTFontFile.Table_name;

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

public class FontParse(string fontFile)
{
    public string FontFile = fontFile;
    public OTFile FontData = new OTFile();

    public bool Open() => FontData.open(FontFile);
    public bool IsCollection() => FontData.IsCollection();
    public uint GetNumFonts() => FontData.GetNumFonts();
    public OTFont GetFont(uint index) => FontData.GetFont(index)!;

    public FontInfo GetFontInfo(uint index)
    {
        var font = GetFont(index);

        var nameTable = (Table_name)font.GetTable("name")!;
        var os2Table = (Table_OS2)font.GetTable("OS/2")!;
        var fsSel = os2Table.fsSelection;

        var ids = new Dictionary<string, GetStringParams>
        {
            { "postscript_name", new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.postScriptName } },
            //{ "full_name",       new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.fullName } },
            { "family_name",     new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.familyName } },
            { "family_name_loc", new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.zh_Hans_CN, NameID = (ushort)NameID.familyName } },
            //{ "subfamily_name",  new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.subfamilyName } },
        };

        var result = GetBuffers(nameTable, ids);

        var nameDict = new Dictionary<string, string>();
        foreach (var kv in result)
        {
            if (kv.Value.buf != null)
            {
                var s = DecodeString(kv.Value.curPlatID, kv.Value.curEncID, kv.Value.curLangID, kv.Value.buf);
                nameDict.Add(kv.Key, s!);
            }
        }
        
        var fnHad = nameDict.TryGetValue("family_name", out var familyName);
        var fnlocHad = nameDict.TryGetValue("family_name_loc", out var familyNameLoc);

        if (!fnHad && !fnlocHad)
        {
            throw new Exception($"Please check {FontFile}, it does not have a recognizable font family name");
        }

        if (!fnHad)
        {
            familyName = familyNameLoc;
        }
        else if (!fnlocHad)
        {
            familyNameLoc = familyName;
        }

        return new FontInfo()
        {
            FamilyName = familyName!,
            FamilyNameChs = familyNameLoc!,
            //Regular = ((fsSel & 0b_0100_0000) >> 6) == 1,   // bit 6
            Bold = ((fsSel & 0b_0010_0000) >> 5) == 1,  // bit 5
            Italic = (fsSel & 0b_1) == 1,   // bit 0
            Weight = os2Table.usWeightClass,
            //MaybeHasTrueBoldOrItalic = false,
            FileName = FontFile,
            Index = index,
            MaxpNumGlyphs = font.GetMaxpNumGlyphs(),
        };
    }


    private struct GetStringParams
    {
        //public ushort PlatID;
        public ushort EncID;
        public ushort LangID;
        public ushort NameID;
    }

    private struct GetBufResult
    {
        public byte[]? buf;
        public ushort curPlatID;
        public ushort curEncID;
        public ushort curLangID;
    }

    private static Dictionary<string, GetBufResult> GetBuffers(Table_name nameTable, Dictionary<string, GetStringParams> ids)
    {
        Dictionary<string, GetBufResult> result = [];
        //List<string> tmpKeys = [];
        for (uint i = 0; i < nameTable.NumberNameRecords; i++)
        {
            var nr = nameTable.GetNameRecord(i);
            if (nr == null) { continue; }
            foreach (var kv in ids)
            {
                if ((nr.PlatformID == (ushort)Table_name.PlatformID.Windows) &&
                ((kv.Value.EncID == 0xffff || nr.EncodingID == kv.Value.EncID) && nr.EncodingID != (ushort)EncodingIDWindows.Unicode_full_repertoire) &&
                (kv.Value.LangID == 0xffff || nr.LanguageID == kv.Value.LangID) &&
                nr.NameID == kv.Value.NameID)
                {
                    var r = new GetBufResult
                    {
                        buf = nameTable.GetEncodedString(nr),
                        curPlatID = nr.PlatformID,
                        curEncID = nr.EncodingID,
                        curLangID = nr.LanguageID,
                    };
                    result.Add(kv.Key, r);
                    //yield return new Dictionary<string, GetBufResult> { { kv.Key, r} };
                }
            }
        }
        return result;
    }
}
