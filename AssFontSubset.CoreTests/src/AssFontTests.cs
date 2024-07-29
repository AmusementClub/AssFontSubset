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
        // var fnChs = "Times New Roman";
        var fnDict = new Dictionary<int, string>()
        {
            {1033, fn},
        };
        
        var afiR =  new AssFontInfo() { Name = fn, Weight = 0, Italic = false };
        var afiB = new AssFontInfo() { Name = fn, Weight = 1, Italic = false };
        var afiI = new AssFontInfo() { Name = fn, Weight = 0, Italic = true };
        var afiZ = new AssFontInfo() { Name = fn, Weight = 1, Italic = true };
        var afi4 = new AssFontInfo() { Name = fn, Weight = 400, Italic = false };

        var fiR  = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = false, Weight = 400 };
        var fiB = new FontInfo() { FamilyNames = fnDict, Bold = true,  Italic = false, Weight = 700 };
        var fiI = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = true,  Weight = 400 };
        var fiZ = new FontInfo() { FamilyNames = fnDict, Bold = true,  Italic = true,  Weight = 700 };

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

        var afi = new AssFontInfo() { Name = fn, Weight = 0, Italic = false };
        var afiBF = new AssFontInfo() { Name = fn, Weight = 1, Italic = false };
        var afiIF = new AssFontInfo() { Name = fn, Weight = 0, Italic = true };
        var afiZF = new AssFontInfo() { Name = fn, Weight = 1, Italic = true };
        var afiChs = new AssFontInfo() { Name = fnChs, Weight = 0, Italic = false };
        var afiBFChs = new AssFontInfo() { Name = fnChs, Weight = 1, Italic = false };
        var afiIFChs = new AssFontInfo() { Name = fnChs, Weight = 0, Italic = true };
        var afiZFChs = new AssFontInfo() { Name = fnChs, Weight = 1, Italic = true };

        var fi = new FontInfo() { FamilyNames = fnDict, Bold = false, Italic = false, Weight = 400 };

        var afL = new List<AssFontInfo>() { afi, afiBF, afiIF, afiZF, afiChs, afiBFChs, afiIFChs, afiZFChs };
        foreach ( var af in afL )
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

        var afiR = new AssFontInfo() { Name = fn, Weight = 0, Italic = false };
        var afiB = new AssFontInfo() { Name = fn, Weight = 1, Italic = false };
        var afiI = new AssFontInfo() { Name = fn, Weight = 0, Italic = true };
        var afiZ = new AssFontInfo() { Name = fn, Weight = 1, Italic = true };
        var afi4 = new AssFontInfo() { Name = fn, Weight = 400, Italic = false };

        //var fiR = new FontInfo() { FamilyName = fn, FamilyNameChs = fnChs, Bold = false, Italic = false, Weight = 400 };
        //var fiB = new FontInfo() { FamilyName = fn, FamilyNameChs = fnChs, Bold = true, Italic = false, Weight = 700 };
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

        var afiR = new AssFontInfo() { Name = fn, Weight = 0, Italic = false };
        var afiB = new AssFontInfo() { Name = fn, Weight = 1, Italic = false };
        var afiI = new AssFontInfo() { Name = fn, Weight = 0, Italic = true };
        var afiZ = new AssFontInfo() { Name = fn, Weight = 1, Italic = true };
        var afi4 = new AssFontInfo() { Name = fn, Weight = 400, Italic = false };

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
}