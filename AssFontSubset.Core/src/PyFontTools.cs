using System.Diagnostics;
using System.Text;
using System.Xml;

namespace AssFontSubset.Core;

public struct SubsetConfig
{
    public bool SourceHanEllipsis;
    public bool DebugMode;
}

public class PyFontTools(string pyftsubset, string ttx)
{
    private readonly string _pyftsubset = pyftsubset;
    private readonly string _ttx = ttx;
    public SubsetConfig Config;

    public void SubsetFonts(List<SubsetFont> subsetFonts, string outputFolder)
    {
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
        nameMap = [];
        var randoms = SubsetFont.GenerateRandomStrings(8, subsetFonts.Keys.Count);

        var i = 0;
        foreach (var kv in subsetFonts)
        {
            nameMap[kv.Key] = randoms[i];
            foreach (var subsetFont in kv.Value)
            {
                subsetFont.RandomNewName = randoms[i];
                CreateFontSubset(subsetFont, outputFolder);
                DumpFont(subsetFont);
                ChangeXmlFontName(subsetFont);
                CompileFont(subsetFont);

                if (!Config.DebugMode)
                {
                    DeleteTempFiles(subsetFont);
                }
            }
            //Parallel.ForEach(kv.Value, subsetFont =>
            //{
            //    subsetFont.RandomNewName = randoms[i];
            //    CreateFontSubset(subsetFont, outputFolder);
            //    DumpFont(subsetFont);
            //    ChangeXmlFontName(subsetFont);
            //    CompileFont(subsetFont);

            //    if (!Config.KeepTempFiles)
            //    {
            //        DeleteTempFiles(subsetFont);
            //    }
            //});
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
        ExecuteCmd(subsetCmd, Config.DebugMode);
    }

    public void DumpFont(SubsetFont ssf) => ExecuteCmd(GetDumpFontCmd(ssf), Config.DebugMode);

    private void CompileFont(SubsetFont ssf) => ExecuteCmd(GetCompileFontCmd(ssf), Config.DebugMode);

    private void DeleteTempFiles(SubsetFont ssf)
    {
        File.Delete(ssf.SubsetFontFileTemp!);
        File.Delete(ssf.SubsetFontTtxTemp!);
        File.Delete(ssf.CharactersFile!);
    }

    private void ChangeXmlFontName(SubsetFont font)
    {
        var ttxFile = font.SubsetFontTtxTemp;

        if (!File.Exists(ttxFile))
        {
            throw new Exception($"字体生成 ttx 文件失败，请尝试使用 FontForge 重新生成字体。{Environment.NewLine}" +
                $"文件名：{font.OriginalFontFile}");
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
                    record.InnerText = $"Processed by AssFontSubset v{System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version}";
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
            throw new Exception($"字体名称替换失败，请尝试使用 FontForge 重新生成字体。{Environment.NewLine}" +
                $"文件名：{font.OriginalFontFile}");
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


    private static void ExecuteCmd(ProcessStartInfo startInfo, bool printCmd)
    {
        var success = true;
        using var process = Process.Start(startInfo);
        if (process != null)
        {
            var output = process.StandardOutput.ReadToEnd();
            var errorOutput = process.StandardError.ReadToEnd();

            if (printCmd) { 
                Console.WriteLine($"{startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}");
                Console.WriteLine($"正在执行");
            }

            process.WaitForExit();
            var exitCode = process.ExitCode;

            if (exitCode != 0)
            {
                Console.WriteLine("Running command: " + startInfo.Arguments);
                Console.WriteLine("Process failed with exit code: " + exitCode);
                Console.WriteLine("Error Output: " + errorOutput);
                success = false;
            }
            else
            {
                if (printCmd) { Console.WriteLine($"执行成功"); }
                return;
            }
        }
        else
        {
            success = false;
        }

        if (!success)
        {
            throw new Exception($"命令执行失败：{startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}");
        }

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
        string[] argus = [
            ssf.OriginalFontFile.FullName,
            $"--text-file={ssf.CharactersFile!}",
            $"--output-file={ssf.SubsetFontFileTemp!}",
            "--name-languages=*",
            $"--font-number={ssf.TrackIndex}",
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
        startInfo.ArgumentList.Add(ssf.SubsetFontTtxTemp!);
        startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        return startInfo;
    }
}
