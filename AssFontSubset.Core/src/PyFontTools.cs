using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Xml;
using ZLogger;

namespace AssFontSubset.Core;

public struct SubsetConfig
{
    public bool SourceHanEllipsis;
    public bool DebugMode;
}

public class PyFontTools(string pyftsubset, string ttx, ILogger? logger)
{
    private readonly string _pyftsubset = pyftsubset;
    private readonly string _ttx = ttx;
    private string pyFtVersion = string.Empty;
    private readonly ILogger? _logger = logger;
    public SubsetConfig Config;
    public Stopwatch? sw;
    private long timer;

    public void SubsetFonts(List<SubsetFont> subsetFonts, string outputFolder)
    {
        GetFontToolsVersion();
        var randoms = SubsetFont.GenerateRandomStrings(8, subsetFonts.Count);
        var num = 0;
        foreach (var subsetFont in subsetFonts)
        {
            subsetFont.RandomNewName = randoms[num];
            CreateFontSubset(subsetFont, outputFolder);
            DumpFont(subsetFont);
            ChangeXmlFontName(subsetFont);
            CompileFont(subsetFont);

            if (!Config.DebugMode)
            {
                DeleteTempFiles(subsetFont);
            }

            num++;
        }
    }

    public void SubsetFonts(Dictionary<string, List<SubsetFont>> subsetFonts, string outputFolder, out Dictionary<string, string> nameMap)
    {
        _logger?.ZLogInformation($"Start subset font");
        GetFontToolsVersion();
        nameMap = [];
        _logger?.ZLogDebug($"Generate randomly non repeating font names");
        var randoms = SubsetFont.GenerateRandomStrings(8, subsetFonts.Keys.Count);

        var i = 0;
        foreach (var kv in subsetFonts)
        {
            nameMap[kv.Key] = randoms[i];
            foreach (var subsetFont in kv.Value)
            {
                subsetFont.RandomNewName = randoms[i];
                _logger?.ZLogInformation($"Start subset {subsetFont.OriginalFontFile.Name}");
                timer = 0;
                CreateFontSubset(subsetFont, outputFolder);
                DumpFont(subsetFont);
                ChangeXmlFontName(subsetFont);
                CompileFont(subsetFont);
                _logger?.ZLogInformation($"Subset font completed, use {timer} ms");

                if (!Config.DebugMode)
                {
                    DeleteTempFiles(subsetFont);
                }
            }
            i++;
        }
    }

    public void CreateFontSubset(SubsetFont ssf, string outputFolder)
    {
        if (!Path.Exists(outputFolder))
        {
            new DirectoryInfo(outputFolder).Create();
        }

        var outputFileWithoutSuffix = Path.GetFileNameWithoutExtension(ssf.OriginalFontFile.Name);
        var outputFileMain = $"{outputFileWithoutSuffix}.{ssf.TrackIndex}.{ssf.RandomNewName}";

        ssf.CharactersFile = Path.Combine(outputFolder, $"{outputFileMain}.txt");
        ssf.SubsetFontFileTemp = Path.Combine(outputFolder, $"{outputFileMain}._tmp_");
        ssf.SubsetFontTtxTemp = Path.Combine(outputFolder, $"{outputFileMain}.ttx");

        ssf.Preprocessing();
        ssf.WriteRunesToUtf8File();

        var subsetCmd = GetSubsetCmd(ssf);
        ExecuteCmd(subsetCmd);
    }

    public void DumpFont(SubsetFont ssf) => ExecuteCmd(GetDumpFontCmd(ssf));

    private void CompileFont(SubsetFont ssf) => ExecuteCmd(GetCompileFontCmd(ssf));

    private void DeleteTempFiles(SubsetFont ssf)
    {
        _logger?.ZLogDebug($"Start delete temp files：{Environment.NewLine}temp subset font files：{ssf.SubsetFontFileTemp}{Environment.NewLine}ttx files：{ssf.SubsetFontTtxTemp}{Environment.NewLine}glyphs files：{ssf.CharactersFile}");
        File.Delete(ssf.SubsetFontFileTemp!);
        File.Delete(ssf.SubsetFontTtxTemp!);
        File.Delete(ssf.CharactersFile!);
        _logger?.ZLogDebug($"Clean completed");
    }

    private void ChangeXmlFontName(SubsetFont font)
    {
        var ttxFile = font.SubsetFontTtxTemp;

        if (!File.Exists(ttxFile))
        {
            throw new Exception($"Font dump to ttx failed, please use FontForge remux font. {Environment.NewLine}" +
                $"File: {font.OriginalFontFile}");
        }

        var ttxContent = File.ReadAllText(ttxFile);
        ttxContent = ttxContent.Replace("\0", ""); // remove null characters. it might be a bug in ttx.exe. 
        var replaced = false;

        var specialFont = ""; // special hack for some fonts

        var xd = new XmlDocument();
        xd.LoadXml(ttxContent);

        // replace font name
        var namerecords = xd.SelectNodes(@"ttFont/name/namerecord");

        foreach (XmlNode record in namerecords!)
        {
            string nameID = record.Attributes!["nameID"]!.Value.Trim();
            switch (nameID)
            {
                case "0":
                    record.InnerText = $"Processed by AssFontSubset v{System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version}; pyFontTools {pyFtVersion}";
                    break;
                case "1":
                case "3":
                case "4":
                case "6":
                    if (record.InnerText.Contains("Source Han"))
                    {
                        specialFont = "Source Han";
                    }
                    record.InnerText = font.RandomNewName!;
                    replaced = true;
                    break;
                default:
                    break;
            }
        }

        // remove substitution for ellipsis for source han sans/serif font
        if (Config.SourceHanEllipsis && specialFont == "Source Han")
        {
            SourceHanFontEllipsis(ref xd);
        }

        xd.Save(ttxFile);

        if (!replaced)
        {
            throw new Exception($"Font name replacement failed, please use FontForge remux font. {Environment.NewLine}" +
                $"File: {font.OriginalFontFile}");
        }
    }

    // Special Hack for Source Han Sans & Source Han Serif ellipsis
    private static void SourceHanFontEllipsis(ref XmlDocument xd)
    {
        // find cid for ellipsis (\u2026)
        var cmap = xd.SelectSingleNode(@"//map[@code='0x2026']");
        if (cmap != null)
        {
            var ellipsisCid = cmap.Attributes!["name"]!.Value;  // why Trim()
            XmlNodeList substitutionNodes = xd.SelectNodes($"//Substitution[@in='{ellipsisCid}']")!;
            // remove substitution for lower ellipsis. 
            // NOTE: Vertical ellipsis is cid5xxxxx, and we need to keep it. Hopefully Adobe won't change it.
            foreach (XmlNode sNode in substitutionNodes)
            {
                if (sNode.Attributes!["out"]!.Value.StartsWith("cid6"))
                {
                    sNode.ParentNode!.RemoveChild(sNode);
                }
            }
        }
    }


    private void ExecuteCmd(ProcessStartInfo startInfo)
    {
        sw ??= new Stopwatch();
        var success = true;
        sw.Start();
        using var process = Process.Start(startInfo);
        _logger?.ZLogDebug($"Start command: {startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}");

        if (process != null)
        {
            var output = process.StandardOutput.ReadToEnd();
            var errorOutput = process.StandardError.ReadToEnd();

            _logger?.ZLogDebug($"Executing...");
            process.WaitForExit();
            var exitCode = process.ExitCode;
            sw.Stop();

            if (exitCode != 0)
            {
                _logger?.ZLogError($"Return exitcode {exitCode}，error output: {errorOutput}");
                success = false;
            }
            else
            {
                _logger?.ZLogDebug($"Successfully executed, use {sw.ElapsedMilliseconds} ms");
            }
            timer += sw.ElapsedMilliseconds;
        }
        else
        {
            success = false;
            _logger?.ZLogDebug($"Process not start");
        }

        sw.Reset();
        if (!success)
        {
            throw new Exception($"Command execution failed: {startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}");
        }
        else { return; }

        //return success;
    }

    private static ProcessStartInfo GetSimpleCmd(string exe) => new()
    {
        FileName = exe,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardInput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        StandardErrorEncoding = Encoding.UTF8,
        StandardOutputEncoding = Encoding.UTF8,
    };

    private ProcessStartInfo GetSubsetCmd(SubsetFont ssf)
    {
        var startInfo = GetSimpleCmd(_pyftsubset);
        
        // GDI doesn’t seem to use any features (may use vert?), and it has its own logic for handling vertical layout.
        // https://learn.microsoft.com/en-us/typography/opentype/spec/features_uz#tag-vrt2
        // GDI may according it:
        // OpenType font with CFF outlines to be used for vertical writing must have vrt2, otherwise fallback
        // OpenType font without CFF outlines use vert map default glyphs to vertical writing glyphs
        
        // https://github.com/libass/libass/pull/702
        // libass seems to be trying to use features like vert to solve this problem.
        // These are features related to vertical layout but are not enabled: "vchw", "vhal", "vkrn", "vpal", "vrtr".
        // https://github.com/libass/libass/blob/6e83137cdbaf4006439d526fef902e123129707b/libass/ass_shaper.c#L147
        string[] enableFeatures = [
            "vert", "vrtr",
            "vrt2",
            "vkna",
        ];
        string[] argus = [
            ssf.OriginalFontFile.FullName,
            $"--text-file={ssf.CharactersFile!}",
            $"--output-file={ssf.SubsetFontFileTemp!}",
            "--name-languages=*",
            $"--font-number={ssf.TrackIndex}",
            // "--no-layout-closure",
            $"--layout-features={string.Join(",", enableFeatures)}",
            // "--layout-features=*",
            
            // Affects VSFilter vertical layout, it can’t find correct fonts when change OS/2 ulCodePageRange*
            // Perhaps it only works with OpenType font that don’t have CFF outlines
            "--no-prune-codepage-ranges"
        ];
        foreach (var arg in argus)
        {
            startInfo.ArgumentList.Add(arg);
        }
        startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        return startInfo;
    }

    private ProcessStartInfo GetDumpFontCmd(SubsetFont ssf)
    {
        var startInfo = GetSimpleCmd(_ttx);
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(ssf.SubsetFontTtxTemp!);
        startInfo.ArgumentList.Add(ssf.SubsetFontFileTemp!);
        //startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        return startInfo;
    }

    private ProcessStartInfo GetCompileFontCmd(SubsetFont ssf)
    {
        var startInfo = GetSimpleCmd(_ttx);
        startInfo.ArgumentList.Add("-f");
        
        // https://github.com/libass/libass/issues/619#issuecomment-1244561188
        // Don’t recalc glyph bounding boxes
        startInfo.ArgumentList.Add("-b");
        
        startInfo.ArgumentList.Add(ssf.SubsetFontTtxTemp!);
        startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        return startInfo;
    }

    private void GetFontToolsVersion()
    {
        var startInfo = GetSimpleCmd(_ttx);
        startInfo.ArgumentList.Add("--version");
        using var process = Process.Start(startInfo);
        if (process != null)
        {
            pyFtVersion = process.StandardOutput.ReadToEnd().Trim('\n');
            process.WaitForExit();
            _logger?.ZLogInformation($"Font subset use pyFontTools {pyFtVersion}");
        }
        else
        {
            throw new Exception($"Command execution failed: {startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}");
        }
    }
}
