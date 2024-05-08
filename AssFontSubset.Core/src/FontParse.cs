using OTFontFile;
using static OTFontFile.Table_name;

namespace AssFontSubset.Core;

public struct FontInfo
{
    public string FamilyName;
    public string FamilyNameChs;
    public bool Bold;
    public bool Italic;
    public int Weight;
    public bool MaybeHasTrueBoldOrItalic;
    public string FileName;
    public uint Index;
    public ushort MaxpNumGlyphs;

    public override bool Equals(object? obj)
    {
        return obj is FontInfo info &&
               FamilyName == info.FamilyName &&
               FamilyNameChs == info.FamilyNameChs &&
               Bold == info.Bold &&
               Italic == info.Italic &&
               Weight == info.Weight &&
               MaybeHasTrueBoldOrItalic == info.MaybeHasTrueBoldOrItalic &&
               FileName == info.FileName &&
               Index == info.Index &&
               MaxpNumGlyphs == info.MaxpNumGlyphs;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(FamilyName);
        hash.Add(FamilyNameChs);
        hash.Add(Bold);
        hash.Add(Italic);
        hash.Add(Weight);
        hash.Add(MaybeHasTrueBoldOrItalic);
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

    public FontInfo GetFontInfo(uint index, HashSet<string>? trueRecord = null)
    {
        var font = GetFont(index);
        var infoFileBased = GetFontInfo(font);

        var familyName = infoFileBased["family_name"];
        var weight = int.Parse(infoFileBased["weight"]);

        if (!infoFileBased.TryGetValue("family_name_loc", out var familyNameLoc))
        {
            familyNameLoc = familyName;
        }

        var infoAssLike = new FontInfo()
        {
            FamilyName = familyName,
            FamilyNameChs = familyNameLoc,
            Bold = false,
            Italic = false,
            Weight = weight,
            MaybeHasTrueBoldOrItalic = false,
            FileName = FontFile,
            Index = index,
            MaxpNumGlyphs = font.GetMaxpNumGlyphs(),
        };

        if (infoFileBased["subfamily_name"].Contains("Bold"))
        {
            // 600 DB maybe regular+bold
            if (weight == 700 || weight == 600)
            {
                // maybe only sign style (such as morisawa), normal is DB/B/ED, hanyi use 75J/F/W/S
                // UD Digi Kyokasho N-B maybe correct regular+bold
                string[] boldIndicators = [" B", " DB", " EB", "75W", "75S", "75J", "75F"];
                // but some morisawa fonts is weird, such as A-OTF Jun Pro 501, will exclude all
                string[] excludedPrefixes = ["A-OTF", "A P-OTF", "G-OTF"];

                if (!(boldIndicators.Any(familyName.EndsWith) || excludedPrefixes.Any(familyName.StartsWith)))
                {
                    infoAssLike.Bold = true;
                    trueRecord?.Add(familyName);
                }
            }            
        }

        if (infoFileBased["subfamily_name"].Contains("Italic"))
        {
            infoAssLike.Italic = true;
            trueRecord?.Add(familyName);
        }

        return infoAssLike;
    }
    public FontInfo GetFontInfo(uint index) => GetFontInfo(index, null);

    public static Dictionary<string, string> GetFontInfo(OTFont font)
    {
        var nameTable = (Table_name)font.GetTable("name")!;

        var psName = nameTable.GetString((ushort)Table_name.PlatformID.Windows, 0xffff, (ushort)LanguageIDWindows.en_US, (ushort)NameID.postScriptName);
        //var fullName = nameTable.GetString

        var ids = new Dictionary<string, GetStringParams>
        {
            { "postscript_name", new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.postScriptName } },
            { "full_name",       new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.fullName } },
            { "family_name",     new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.familyName } },
            { "family_name_loc", new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.zh_Hans_CN, NameID = (ushort)NameID.familyName } },
            { "subfamily_name",  new GetStringParams { EncID = 0xffff, LangID = (ushort)LanguageIDWindows.en_US, NameID = (ushort)NameID.subfamilyName } },
        };

        var result = GetBuffers(nameTable, ids);

        var stringDict = new Dictionary<string, string>();
        foreach (var kv in result)
        {
            if (kv.Value.buf != null)
            {
                var s = DecodeString(kv.Value.curPlatID, kv.Value.curEncID, kv.Value.curLangID, kv.Value.buf);
                stringDict.Add(kv.Key, s!);
            }
        }

        if (stringDict.Count > 0)
        {
            var os2Table = (Table_OS2)font.GetTable("OS/2")!;
            stringDict.Add("weight", os2Table.usWeightClass.ToString());
        }

        return stringDict;
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
                (kv.Value.EncID == 0xffff || nr.EncodingID == kv.Value.EncID) &&
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
