using AssFontSubset.Core;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace AssFontSubset.Console;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var path = new Argument<FileInfo[]>(name: "path", description: "要子集化的 ASS 字幕文件路径，可以输入多个同目录的字幕文件");
        var fontPath = new Option<DirectoryInfo>(name: "--fonts", description: "ASS 字幕文件需要的字体所在目录，默认为 ASS 同目录的 fonts 文件夹");
        var outputPath = new Option<DirectoryInfo>(name: "--output", description: "子集化后成品所在目录，默认为 ASS 同目录的 output 文件夹");
        var binPath = new Option<DirectoryInfo>(name: "--bin-path", description: "指定 pyftsubset 和 ttx 所在目录。若未指定，会使用环境变量中的");
        var sourceHanEllipsis = new Option<bool>(name: "--source-han-ellipsis", description: "使思源黑体和宋体的省略号居中对齐", getDefaultValue: () => true);
        var debug = new Option<bool>(name: "--debug", description: "保留子集化期间的各种临时文件，位于 --output-dir 指定的文件夹；同时打印出所有运行的命令", getDefaultValue: () => false);

        var rootCommand = new RootCommand("使用 fonttools 生成 ASS 字幕文件的字体子集，并自动修改字体名称及 ASS 文件中对应的字体名称")
        {
            path, fontPath, outputPath, binPath, sourceHanEllipsis, debug
        };

        rootCommand.SetHandler(Subset, path, fontPath, outputPath, binPath, sourceHanEllipsis, debug);

        return await rootCommand.InvokeAsync(args);
    }

    static void Subset(FileInfo[] path, DirectoryInfo? fontPath, DirectoryInfo? outputPath, DirectoryInfo? binPath, bool sourceHanEllipsis, bool debug)
    {
        var subsetConfig = new SubsetConfig
        {
            SourceHanEllipsis = sourceHanEllipsis,
            DebugMode = debug,
        };
        var logLevel = debug ? LogLevel.Debug : LogLevel.Information;

        using var factory = LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(logLevel);
            logging.AddZLoggerConsole(options =>
            {
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0}{1:yyyy-MM-dd'T'HH:mm:sszzz}|{2:short}|", (in MessageTemplate template, in LogInfo info) => 
                    {
                        // \u001b[31m => Red(ANSI Escape Code)
                        // \u001b[0m => Reset
                        var escapeSequence = info.LogLevel switch
                        {
                            LogLevel.Warning => "\u001b[33m",
                            > LogLevel.Warning => "\u001b[31m",
                            _ => "\u001b[0m",
                        };

                        template.Format(escapeSequence, info.Timestamp, info.LogLevel);
                    });
                    
                });
            });
        });
        var logger = factory.CreateLogger("AssFontSubset.Console");

        var ssFt = new SubsetByPyFT(logger);
        try
        {
            ssFt.SubsetAsync(path, fontPath, outputPath, binPath, subsetConfig).Wait();
        }
        catch (Exception ex) 
        {
            logger.ZLogError($"{ex.Message}");
            logger.ZLogInformation($"Press Any key to exit");
            System.Console.ReadKey();
        };
    }
}
