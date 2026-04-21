using Mobsub.SubtitleParse.AssText;
using System.Reflection;
using System.Text;
using Mobsub.SubtitleParse.AssTypes;

namespace AssFontSubset.Core.Tests;

[TestClass]
public class SubsetCoreTests
{
    [TestMethod]
    public void ChangeAssFontName_ReplacesVerticalStyleAndOverrideFontNames()
    {
        var assPath = CreateTempAssFile(
            """
            [Script Info]
            ScriptType: v4.00+
            PlayResX: 1920
            PlayResY: 1080

            [V4+ Styles]
            Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
            Style: Default,Example Font,50,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,0,2,10,10,10,1
            Style: Vertical,@Example Font,50,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,0,2,10,10,10,1

            [Events]
            Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
            Dialogue: 0,0:00:00.00,0:00:01.00,Default,,0,0,0,,{\fnExample Font}hello
            Dialogue: 0,0:00:01.00,0:00:02.00,Vertical,,0,0,0,,{\fn@Example Font}world
            """
        );

        try
        {
            var ass = new AssData();
            ass.ReadAssFile(assPath);

            var fontInfo = new FontInfo
            {
                FamilyNames = new Dictionary<int, string> { [1033] = "Example Font" },
                FileName = "Example.ttf",
            };

            var fontMap = new Dictionary<FontInfo, List<AssFontInfo>>
            {
                [fontInfo] =
                [
                    CreateAssFontInfo("Example Font", 0, false),
                    CreateAssFontInfo("@Example Font", 0, false),
                ]
            };

            var nameMap = new Dictionary<string, string>
            {
                ["Example Font"] = "SUBSET123",
            };

            InvokeChangeAssFontName(ass, nameMap, fontMap);

            Assert.AreEqual("SUBSET123", ass.Styles.Collection.Single(x => x.Name == "Default").Fontname);
            Assert.AreEqual("@SUBSET123", ass.Styles.Collection.Single(x => x.Name == "Vertical").Fontname);
            Assert.AreEqual(@"{\fnSUBSET123}hello", ass.Events!.Collection[0].Text);
            Assert.AreEqual(@"{\fn@SUBSET123}world", ass.Events!.Collection[1].Text);
            CollectionAssert.Contains(ass.ScriptInfo.Comment, "Font Subset: SUBSET123 - Example Font");
        }
        finally
        {
            if (File.Exists(assPath))
            {
                File.Delete(assPath);
            }
        }
    }

    private static void InvokeChangeAssFontName(AssData ass, Dictionary<string, string> nameMap, Dictionary<FontInfo, List<AssFontInfo>> fontMap)
    {
        var method = typeof(SubsetCore).GetMethod("ChangeAssFontName", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(null, [ass, nameMap, fontMap]);
    }

    private static string CreateTempAssFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ass");
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return path;
    }

    private static AssFontInfo CreateAssFontInfo(string name, int weight, bool italic, int encoding = 1)
    {
        return new($"{name},{weight},{(italic ? 1 : 0)},{encoding}");
    }
}
