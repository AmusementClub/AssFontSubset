using Mobsub.SubtitleParse.AssTypes;
using static AssFontSubset.Core.AssFont;

namespace AssFontSubset.Core.Tests;

[TestClass]
public class AssFontTests
{
    [TestMethod]
    public void IsMatchTestTrueBIZ()
    {
        var fn = "Times New Roman";
        var fnChs = "Times New Roman";
        
        var afi =  new AssFontInfo() { Name = fn, Weight = 0, Italic = false };
        var afiB = new AssFontInfo() { Name = fn, Weight = 1, Italic = false };
        var afiI = new AssFontInfo() { Name = fn, Weight = 0, Italic = true };
        var afiZ = new AssFontInfo() { Name = fn, Weight = 1, Italic = true };
        var afi4 = new AssFontInfo() { Name = fn, Weight = 400, Italic = false };

        var fi  = new FontInfo() { FamilyName = fn, FamilyNameChs = fnChs, Bold = false, Italic = false, MaybeHasTrueBoldOrItalic = true,  Weight = 400 };
        var fiB = new FontInfo() { FamilyName = fn, FamilyNameChs = fnChs, Bold = true,  Italic = false, MaybeHasTrueBoldOrItalic = false, Weight = 700 };
        var fiI = new FontInfo() { FamilyName = fn, FamilyNameChs = fnChs, Bold = false, Italic = true,  MaybeHasTrueBoldOrItalic = false, Weight = 400 };
        var fiZ = new FontInfo() { FamilyName = fn, FamilyNameChs = fnChs, Bold = true,  Italic = true,  MaybeHasTrueBoldOrItalic = false, Weight = 700 };

        var afL = new List<AssFontInfo>() { afi, afiB, afiI, afiZ, afi4 };
        var fiL = new List<FontInfo>() { fi, fiB, fiI, fiZ };
        foreach (var a in afL)
        {
            foreach (var f in fiL)
            {
                if ((a == afi && f == fi)
                    || (a == afiB && f == fiB)
                    || (a == afiI && f == fiI)
                    || (a == afiZ && f == fiZ)
                    || (a == afi4 && f == fi)
                    )
                {
                    Assert.IsTrue(IsMatch(a, f));
                }
                else
                {
                    Assert.IsFalse(IsMatch(a, f));
                }
            }
        }
    }

    [TestMethod]
    public void IsMatchTestFakeBIZ()
    {
        var fn = "FZLanTingHei-R-GBK";
        var fnChs = "方正兰亭黑_GBK";

        var afi = new AssFontInfo() { Name = fn, Weight = 0, Italic = false };
        var afiBF = new AssFontInfo() { Name = fn, Weight = 1, Italic = false };
        var afiIF = new AssFontInfo() { Name = fn, Weight = 0, Italic = true };
        var afiZF = new AssFontInfo() { Name = fn, Weight = 1, Italic = true };
        var afiChs = new AssFontInfo() { Name = fnChs, Weight = 0, Italic = false };
        var afiBFChs = new AssFontInfo() { Name = fnChs, Weight = 1, Italic = false };
        var afiIFChs = new AssFontInfo() { Name = fnChs, Weight = 0, Italic = true };
        var afiZFChs = new AssFontInfo() { Name = fnChs, Weight = 1, Italic = true };

        var fi = new FontInfo() { FamilyName = fn, FamilyNameChs = fnChs, Bold = false, Italic = false, MaybeHasTrueBoldOrItalic = false, Weight = 400 };

        var afL = new List<AssFontInfo>() { afi, afiBF, afiIF, afiZF, afiChs, afiBFChs, afiIFChs, afiZFChs };
        foreach ( var af in afL )
        {
            Assert.IsTrue(IsMatch(af, fi));
        }
    }
}