using AssFontSubset.Core;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace AssFontSubset.Console;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var path = new CliArgument<FileInfo[]>("path")
        {
            Description = "要子集化的 ASS 字幕文件路径，可以输入多个同目录的字幕文件"
        };
        var fontPath = new CliOption<DirectoryInfo>("--fonts")
        {
            Description = "ASS 字幕文件需要的字体所在目录，默认为 ASS 同目录的 fonts 文件夹"
        };
        var outputPath = new CliOption<DirectoryInfo>("--output")
        {
            Description = "子集化后成品所在目录，默认为 ASS 同目录的 output 文件夹"
        };
        var binPath = new CliOption<DirectoryInfo>("--bin-path")
        {
            Description = "指定 pyftsubset 和 ttx 所在目录。若未指定，会使用环境变量中的"
        };
        var sourceHanEllipsis = new CliOption<bool>("--source-han-ellipsis")
        {
            Description = "使思源黑体和宋体的省略号居中对齐",
            DefaultValueFactory = _ => true,
        };
        var debug = new CliOption<bool>("--debug")
        {
            Description = "保留子集化期间的各种临时文件，位于 --output-dir 指定的文件夹；同时打印出所有运行的命令",
            DefaultValueFactory = _ => false,
        };

        var rootCommand = new CliRootCommand("使用 fonttools 生成 ASS 字幕文件的字体子集，并自动修改字体名称及 ASS 文件中对应的字体名称")
        {
            path, fontPath, outputPath, binPath, sourceHanEllipsis, debug
        };

        rootCommand.SetAction(async (result, _) =>
        {
            await Subset(
                result.GetValue(path)!,
                result.GetValue(fontPath),
                result.GetValue(outputPath),
                result.GetValue(binPath),
                result.GetValue(sourceHanEllipsis),
                result.GetValue(debug)
            );
        });
        var config = new CliConfiguration(rootCommand)
        {
            EnableDefaultExceptionHandler = false,
        };

        int exitCode;
        try
        {
            exitCode = await rootCommand.Parse(args, config).InvokeAsync();
        }
        catch (Exception)
        {
            exitCode = 1;
        }
        
        if (!System.Console.IsOutputRedirected)
        {
            System.Console.WriteLine("Press Any key to exit...");
            System.Console.ReadKey();
        }
        
        return exitCode;
    }

    static async Task Subset(FileInfo[] path, DirectoryInfo? fontPath, DirectoryInfo? outputPath, DirectoryInfo? binPath, bool sourceHanEllipsis, bool debug)
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
                options.LogToStandardErrorThreshold = LogLevel.Warning;
            });
        });
        var logger = factory.CreateLogger("AssFontSubset.Console");

        if (path.Length == 0)
        {
            logger.ZLogError($"Please input ass files\u001b[0m");
            throw new ArgumentException();
        }

        var ssFt = new SubsetByPyFT(logger);
        try
        {
            await ssFt.SubsetAsync(path, fontPath, outputPath, binPath, subsetConfig);
        }
        catch (Exception ex) 
        {
            logger.ZLogError($"{ex.Message}\u001b[0m");
            throw;
        }
    }
}
