using System.Text;
using System.Reflection;
using Mobsub.Font;
using Mobsub.SubtitleParse.AssText;
using Mobsub.SubtitleParse.AssTypes;
using static AssFontSubset.Core.AssFont;

namespace AssFontSubset.Core.Tests;

[TestClass]
public class AssFontTests
{
    [TestMethod]
    public void MatchTestTrueBIZ()
    {
        var fn = "Times New Roman";
        var fnDict = new Dictionary<int, string>()
        {
            {1033, fn},
        };

        var afiR = CreateAssFontInfo(fn, 0, false);
        var afiB = CreateAssFontInfo(fn, 1, false);
        var afiI = CreateAssFontInfo(fn, 0, true);
        var afiZ = CreateAssFontInfo(fn, 1, true);
        var afi4 = CreateAssFontInfo(fn, 400, false);

        var fiR = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = false, Weight = 400 };
        var fiB = new FontInfo() { FamilyNames = fnDict, Bold = true, Italic = false, Weight = 700 };
        var fiI = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = true, Weight = 400 };
        var fiZ = new FontInfo() { FamilyNames = fnDict, Bold = true, Italic = true, Weight = 700 };

        var afL = new List<AssFontInfo>() { afiR, afiB, afiI, afiZ, afi4 };
        var fiL = new List<FontInfo>() { fiR, fiB, fiI, fiZ };
        var fiGroups = fiL.GroupBy(fontInfo => fontInfo.FamilyNames[1033]);

        foreach (var a in afL)
        {
            foreach (var f in fiGroups)
            {
                var tfi = GetMatchedFontInfo(a, f);
                if (a == afiR) { Assert.IsTrue(fiR == tfi); }
                if (a == afiB) { Assert.IsTrue(fiB == tfi); }
                if (a == afiI) { Assert.IsTrue(fiI == tfi); }
                if (a == afiZ) { Assert.IsTrue(fiZ == tfi); }
                if (a == afi4) { Assert.IsTrue(fiR == tfi); }
            }
        }
    }

    [TestMethod]
    public void MatchTestFakeBIZ()
    {
        var fn = "FZLanTingHei-R-GBK";
        var fnChs = "方正兰亭黑_GBK";
        var fnDict = new Dictionary<int, string>()
        {
            {1033, fn},
            {2052, fnChs}
        };

        var afi = CreateAssFontInfo(fn, 0, false);
        var afiBF = CreateAssFontInfo(fn, 1, false);
        var afiIF = CreateAssFontInfo(fn, 0, true);
        var afiZF = CreateAssFontInfo(fn, 1, true);
        var afiChs = CreateAssFontInfo(fnChs, 0, false);
        var afiBFChs = CreateAssFontInfo(fnChs, 1, false);
        var afiIFChs = CreateAssFontInfo(fnChs, 0, true);
        var afiZFChs = CreateAssFontInfo(fnChs, 1, true);

        var fi = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = false, Weight = 400 };

        var afL = new List<AssFontInfo>() { afi, afiBF, afiIF, afiZF, afiChs, afiBFChs, afiIFChs, afiZFChs };
        foreach (var af in afL)
        {
            Assert.IsTrue(IsMatch(af, fi, true));
        }
    }

    [TestMethod]
    public void MatchTestPartiallyTrueBIZ()
    {
        var fn = "Times New Roman";
        var fnChs = "Times New Roman";
        var fnDict = new Dictionary<int, string>()
        {
            {1033, fn},
            {2052, fnChs}
        };

        var afiR = CreateAssFontInfo(fn, 0, false);
        var afiB = CreateAssFontInfo(fn, 1, false);
        var afiI = CreateAssFontInfo(fn, 0, true);
        var afiZ = CreateAssFontInfo(fn, 1, true);
        var afi4 = CreateAssFontInfo(fn, 400, false);

        var fiI = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = true, Weight = 400 };
        var fiZ = new FontInfo() { FamilyNames = fnDict, Bold = true, Italic = true, Weight = 700 };

        var afL = new List<AssFontInfo>() { afiR, afiB, afiI, afiZ, afi4 };
        var fiL = new List<FontInfo>() { fiI, fiZ };
        var fiGroups = fiL.GroupBy(fontInfo => fontInfo.FamilyNames[1033]);

        foreach (var a in afL)
        {
            foreach (var f in fiGroups)
            {
                var tfi = GetMatchedFontInfo(a, f);
                if (a == afiR) { Assert.IsTrue(null == tfi); }
                if (a == afiB) { Assert.IsTrue(null == tfi); }
                if (a == afiI) { Assert.IsTrue(fiI == tfi); }
                if (a == afiZ) { Assert.IsTrue(fiZ == tfi); }
                if (a == afi4) { Assert.IsTrue(null == tfi); }
            }
        }
    }

    [TestMethod]
    public void MatchTestTrueB()
    {
        var fn = "Source Han Sans";
        var fnChs = "思源黑体";
        var fnDict = new Dictionary<int, string>()
        {
            {1033, fn},
            {2052, fnChs}
        };

        var afiR = CreateAssFontInfo(fn, 0, false);
        var afiB = CreateAssFontInfo(fn, 1, false);
        var afiI = CreateAssFontInfo(fn, 0, true);
        var afiZ = CreateAssFontInfo(fn, 1, true);
        var afi4 = CreateAssFontInfo(fn, 400, false);

        var fiR = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = false, Weight = 400, MaxpNumGlyphs = 65535 };
        var fiB = new FontInfo() { FamilyNames = fnDict, Bold = true, Italic = false, Weight = 700, MaxpNumGlyphs = 65535 };

        var afL = new List<AssFontInfo>() { afiR, afiB, afiI, afiZ, afi4 };
        var fiL = new List<FontInfo>() { fiR, fiB };
        var fiGroups = fiL.GroupBy(fontInfo => fontInfo.FamilyNames[1033]);

        foreach (var a in afL)
        {
            foreach (var f in fiGroups)
            {
                var tfi = GetMatchedFontInfo(a, f);
                if (a == afiR) { Assert.IsTrue(fiR == tfi); }
                if (a == afiB) { Assert.IsTrue(fiB == tfi); }
                if (a == afiI) { Assert.IsTrue(fiR == tfi); }
                if (a == afiZ) { Assert.IsTrue(fiB == tfi); }
                if (a == afi4) { Assert.IsTrue(fiR == tfi); }
            }
        }
    }

    [TestMethod]
    public void MatchTestFullNameAndWrongBoldFlag()
    {
        var familyEn = "FZRuiZhengHei_GBK";
        var familyZh = "方正锐正黑_GBK";
        var fullBoldEn = "FZRuiZhengHei_GBK Bold";
        var fullBoldZh = "方正锐正黑_GBK Bold";

        var fnDict = new Dictionary<int, string>()
        {
            {1033, familyEn},
            {2052, familyZh}
        };

        var fiR = new FontInfo()
        {
            FamilyNames = fnDict,
            MatchNames = new HashSet<string>(StringComparer.Ordinal) { familyEn, familyZh },
            Bold = false,
            Italic = false,
            Weight = 400,
        };
        var fiB = new FontInfo()
        {
            FamilyNames = fnDict,
            MatchNames = new HashSet<string>(StringComparer.Ordinal) { familyEn, familyZh, fullBoldEn, fullBoldZh },
            Bold = false,
            Italic = false,
            Weight = 700,
        };

        var fiGroups = new List<FontInfo>() { fiR, fiB }.GroupBy(fontInfo => fontInfo.FamilyNames[1033]);

        var afiFamilyR = CreateAssFontInfo(familyZh, 0, false);
        var afiFamilyB = CreateAssFontInfo(familyZh, 1, false);
        var afiFullZh = CreateAssFontInfo(fullBoldZh, 0, false);
        var afiFullEn = CreateAssFontInfo(fullBoldEn, 0, false);

        foreach (var f in fiGroups)
        {
            Assert.IsTrue(fiR == GetMatchedFontInfo(afiFamilyR, f));
            Assert.IsTrue(fiB == GetMatchedFontInfo(afiFamilyB, f));
            Assert.IsTrue(fiB == GetMatchedFontInfo(afiFullZh, f));
            Assert.IsTrue(fiB == GetMatchedFontInfo(afiFullEn, f));
        }
    }

    [TestMethod]
    public void ConvertToFontInfo_LocalizedOnlyFamilyNameStillMatchesIssue16Fonts()
    {
        const string familyName = "迷你简毡笔黑";
        const string postScriptName = "Ftiebihei";

        var faceInfo = new FontFaceInfoOpenType
        {
            FileInfo = new FontFileInfo { FilePath = @"F:\fonts\迷你简毡笔黑.ttf" },
            FamilyNamesGdi = new Dictionary<int, string> { { 2052, familyName } },
            FamilyNames = new Dictionary<int, string> { { 2052, familyName } },
            FullNames = new Dictionary<int, string> { { 2052, familyName } },
            PostScriptName = postScriptName,
            Weight = 400,
        };
        faceInfo.fsSelection = 0;
        faceInfo.MaxpNumGlyphs = 4096;

        var fontInfo = ConvertToFontInfo(faceInfo);
        var matched = GetMatchedFontInfo(
            CreateAssFontInfo(familyName, 0, false),
            new[] { fontInfo }.GroupBy(x => x.FamilyNames[1033]).Single());

        Assert.AreEqual(familyName, fontInfo.FamilyNames[1033]);
        Assert.IsNotNull(fontInfo.MatchNames);
        Assert.IsTrue(fontInfo.MatchNames.Contains(familyName));
        Assert.IsTrue(fontInfo.MatchNames.Contains(postScriptName));
        Assert.IsTrue(fontInfo == matched);
    }

    [TestMethod]
    public void GetAssFonts_ResetStyleUsesDefinedStyleFont()
    {
        var assPath = CreateTempAssFile(
            """
            [Script Info]
            ScriptType: v4.00+
            PlayResX: 1920
            PlayResY: 1080

            [V4+ Styles]
            Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
            Style: Default,Base Font,50,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,0,2,10,10,10,1
            Style: Alt,Alt Font,50,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,0,2,10,10,10,1

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,{\rAlt}abc
            """
        );

        try
        {
            var result = GetAssFonts(assPath, out _);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Keys.Any(x => x.Name == "Alt Font"));
            CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, result.Single().Value.Select(x => x.ToString()).ToArray());
        }
        finally
        {
            if (File.Exists(assPath))
            {
                File.Delete(assPath);
            }
        }
    }

    [TestMethod]
    public void GetAssFonts_ResetStyleToUndefinedStyleFallsBackToDefault()
    {
        var assPath = CreateTempAssFile(
            """
            [Script Info]
            ScriptType: v4.00+
            PlayResX: 1920
            PlayResY: 1080

            [V4+ Styles]
            Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
            Style: Default,Base Font,50,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,0,2,10,10,10,1

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,{\rMissingStyle}abc
            """
        );

        try
        {
            var result = GetAssFonts(assPath, out _);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Keys.Any(x => x.Name == "Base Font"));
            CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, result.Single().Value.Select(x => x.ToString()).ToArray());
        }
        finally
        {
            if (File.Exists(assPath))
            {
                File.Delete(assPath);
            }
        }
    }

    [TestMethod]
    public void GetAssFonts_NormalizesEncodingToOne()
    {
        var assPath = CreateTempAssFile(
            """
            [Script Info]
            ScriptType: v4.00+
            PlayResX: 1920
            PlayResY: 1080

            [V4+ Styles]
            Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
            Style: Default,Encoded Font,50,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,0,2,10,10,10,134

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,abc
            """
        );

        try
        {
            var result = GetAssFonts(assPath, out _);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result.Single().Key.Encoding);
            Assert.IsFalse(result.Keys.Any(x => x.Encoding != 1));
        }
        finally
        {
            if (File.Exists(assPath))
            {
                File.Delete(assPath);
            }
        }
    }

    private static AssFontInfo CreateAssFontInfo(string name, int weight, bool italic, int encoding = 1)
    {
        return new($"{name},{weight},{(italic ? 1 : 0)},{encoding}");
    }

    private static FontInfo ConvertToFontInfo(FontFaceInfoOpenType faceInfo)
    {
        var method = typeof(FontParse).GetMethod("ConvertToFontInfo", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (FontInfo)method.Invoke(null, [faceInfo])!;
    }

    private static string CreateTempAssFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ass");
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return path;
    }
}
