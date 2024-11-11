using System.Runtime.InteropServices;
using System.Text;
using HarfBuzzBinding;
using Microsoft.Extensions.Logging;
using ZLogger;
using SubsetApis = HarfBuzzBinding.Native.Subset.Apis;
using HBApis = HarfBuzzBinding.Native.Apis;
using System.Diagnostics;
using HarfBuzzBinding.Native.Subset;
using OTFontFile;

namespace AssFontSubset.Core;

public unsafe class HarfBuzzSubset(ILogger? logger) : SubsetToolBase
{
    private Version hbssVersion = new Version(Methods.GetHarfBuzzVersion()!);
    public SubsetConfig Config;
    public Stopwatch? sw;
    private long timer;

    public override void SubsetFonts(Dictionary<string, List<SubsetFont>> subsetFonts, string outputFolder, out Dictionary<string, string> nameMap)
    {
        logger?.ZLogInformation($"Start subset font");
        logger?.ZLogInformation($"Font subset use harfbuzz-subset {hbssVersion}");
        nameMap = [];
        logger?.ZLogDebug($"Generate randomly non repeating font names");
        var randoms = SubsetFont.GenerateRandomStrings(8, subsetFonts.Keys.Count);
        
        var i = 0;
        foreach (var kv in subsetFonts)
        {
            nameMap[kv.Key] = randoms[i];
            foreach (var subsetFont in kv.Value)
            {
                subsetFont.RandomNewName = randoms[i];
                logger?.ZLogInformation($"Start subset {subsetFont.OriginalFontFile.Name}");
                timer = 0;
                CreateFontSubset(subsetFont, outputFolder);
                logger?.ZLogInformation($"Subset font completed, use {timer} ms");
            }

            i++;
        }
    }
    
    public override void CreateFontSubset(SubsetFont ssf, string outputFolder)
    {
        if (!Path.Exists(outputFolder))
        {
            new DirectoryInfo(outputFolder).Create();
        }

        var outputFileWithoutSuffix = Path.GetFileNameWithoutExtension(ssf.OriginalFontFile.Name);
        var outputFile = new StringBuilder($"{outputFileWithoutSuffix}.{ssf.TrackIndex}.{ssf.RandomNewName}");
        
        var originalFontFileSuffix = Path.GetExtension(ssf.OriginalFontFile.Name).AsSpan();
        var outFileWithoutSuffix = outputFile.ToString();
        outputFile.Append(originalFontFileSuffix[..3]);
        switch (originalFontFileSuffix[^1])
        {
            case 'c':
                outputFile.Append('f');
                break;
            case 'C':
                outputFile.Append('F');
                break;
            default:
                outputFile.Append(originalFontFileSuffix[^1]);
                break;
        }

        var outputFileName = Path.Combine(outputFolder, outputFile.ToString());

        ssf.Preprocessing();
        var modifyIds = GetModifyNameIds(ssf.OriginalFontFile.FullName, ssf.TrackIndex);
        if (Config.DebugMode)
        {
            ssf.CharactersFile = Path.Combine(outputFolder, $"{outFileWithoutSuffix}.txt");
            ssf.WriteRunesToUtf8File();
        }

        sw ??= new Stopwatch();
        sw.Start();
        
        _ = Methods.TryGetFontFace(ssf.OriginalFontFile.FullName, ssf.TrackIndex, out var facePtr);
        facePtr = SubsetApis.hb_subset_preprocess(facePtr);
        
        var input = SubsetApis.hb_subset_input_create_or_fail();
        
        var unicodes = SubsetApis.hb_subset_input_unicode_set(input);
        foreach (var rune in ssf.Runes)
        {
            HBApis.hb_set_add(unicodes, (uint)rune.Value);
        }
        
        var features = SubsetApis.hb_subset_input_set(input, hb_subset_sets_t.HB_SUBSET_SETS_LAYOUT_FEATURE_TAG);
        HBApis.hb_set_clear(features);
        foreach (var feature in FontConstant.SubsetKeepFeatures)
        {
            HBApis.hb_set_add(features, HBApis.hb_tag_from_string((sbyte*)Marshal.StringToHGlobalAnsi(feature), -1));
        }

        Methods.RenameFontname(input,
            (sbyte*)Marshal.StringToHGlobalAnsi($"Processed by AssFontSubset v{System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version}; harfbuzz-subset {hbssVersion}"),
            (sbyte*)Marshal.StringToHGlobalAnsi(ssf.RandomNewName),
            modifyIds);
        
        var faceNewPtr = SubsetApis.hb_subset_or_fail(facePtr, input);

        var blobPtr = HBApis.hb_face_reference_blob(faceNewPtr);
        Methods.WriteFontFile(blobPtr, outputFileName);
        
        sw.Stop();
        timer += sw.ElapsedMilliseconds;
        sw.Reset();

        SubsetApis.hb_subset_input_destroy(input);
        HBApis.hb_face_destroy(faceNewPtr);
        HBApis.hb_face_destroy(facePtr);
    }

    private static OpenTypeNameId[] GetModifyNameIds(string fontFileName, uint index)
    {
        List<OpenTypeNameId> ids = [];
        var otf = new OTFile();
        otf.open(fontFileName);
        var font = otf.GetFont(index);
        var nameTable = (Table_name)font!.GetTable("name")!;
        for (uint i = 0; i < nameTable.NumberNameRecords; i++)
        {
            var nameRecord = nameTable.GetNameRecord(i);
            if (nameRecord!.NameID is 0 or 1 or 3 or 4 or 6)
            {
                ids.Add(new OpenTypeNameId
                {
                    NameId = nameRecord.NameID,
                    PlatformId = nameRecord.PlatformID,
                    LanguageId = nameRecord.LanguageID,
                    EncodingId = nameRecord.EncodingID,
                });
            }
        }

        return ids.ToArray();
    }
}