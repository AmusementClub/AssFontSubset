using System.Text;

namespace AssFontSubset.Core;

public class SubsetFont(FileInfo originalFontFile, uint index, List<Rune> runes)
{
    private readonly FileInfo _originalFontFile = originalFontFile;
    public FileInfo OriginalFontFile
    {
        get => _originalFontFile ?? throw new Exception("Please set OriginalFontFile fullpath");
        set
        {
            //_originalFontFile = value;
            if (value != null)
            {
                CharactersFile = Path.GetFileNameWithoutExtension(value.Name) + ".txt";
            }
        }
    }

    //public bool IsCollection = isCollection;
    public readonly uint TrackIndex = index;

    //public List<string>? FontNameInAss;
    public List<Rune> Runes = runes;

    //public string? OriginalFamilyName;
    public string? RandomNewName;
    public string? CharactersFile;
    public string? SubsetFontFileTemp;
    public string? SubsetFontTtxTemp;
    public string? SubsetFontFile;

    public override int GetHashCode() => HashCode.Combine(_originalFontFile.Name, TrackIndex);

    public static string[] GenerateRandomStrings(int length, int count)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var key = string.Empty;
        var keys = new HashSet<string>(count);
        for (var i = 0; i < count; i++)
        {
            do
            {
                key = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            } while (!keys.Add(key));
        }
        var result = keys.ToArray();
        keys = null;
        return result;
    }

    public void Preprocessing()
    {
        //var chars = new List<Rune>();
        //var emojis = new List<Rune>();
        var runes = new List<Rune>();

        foreach (var rune in Runes)
        {
            if (rune.IsBmp)
            {
                if (Rune.IsDigit(rune) || char.IsAsciiLetter((char)rune.Value) || (rune.Value >= 0xFF10 && rune.Value <= 0xFF19))
                {
                    continue;
                }
                else
                {
                    runes.Add(rune);
                }
            }
            else
            {
                runes.Add(rune);
            }
        }
        AppendNecessaryRunes(runes);
        Runes = runes;
    }

    /// <summary>
    /// Subset all half-width letters and digits, will fix font fallback on ellipsis
    /// Subset all half-width digits
    /// </summary>
    /// <param name="runes"></param>
    private static void AppendNecessaryRunes(List<Rune> runes)
    {
        // letters
        // Uppercase Latin alphabet
        for (var i = 0x0041; i <= 0x005A; i++)
        {
            runes.Add(new Rune(i));
        }
        // Lowercase Latin alphabet
        for (var i = 0x0061; i <= 0x007A; i++)
        {
            runes.Add(new Rune(i));
        }

        // digits
        // half-width and full-width
        for (var i = 0x0030; i <= 0x0039; i++)
        {
            runes.Add(new Rune(i));
            runes.Add(new Rune(i + 65248));
        }
    }

    public void WriteRunesToUtf8File()
    {
        using var fs = new FileStream(CharactersFile!, FileMode.Create, FileAccess.Write, FileShare.None);
        var buffer = new byte[1024];
        int bufferOffset = 0;

        foreach (var rune in Runes)
        {
            if (bufferOffset + 4 > buffer.Length)
            {
                fs.Write(buffer, 0, bufferOffset);
                bufferOffset = 0;
            }

            var bytesWritten = rune.EncodeToUtf8(buffer.AsSpan(bufferOffset));
            bufferOffset += bytesWritten;
        }

        if (bufferOffset > 0)
        {
            fs.Write(buffer, 0, bufferOffset);
        }
        
        fs.Flush();
        fs.Close();
    }
}
