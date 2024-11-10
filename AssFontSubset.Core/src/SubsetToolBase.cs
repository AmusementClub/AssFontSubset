namespace AssFontSubset.Core;

public abstract class SubsetToolBase
{
    public abstract void SubsetFonts(Dictionary<string, List<SubsetFont>> subsetFonts, string outputFolder, out Dictionary<string, string> nameMap);
    public abstract void CreateFontSubset(SubsetFont ssf, string outputFolder);
}