namespace AssFontSubset.Core;

public struct SubsetConfig
{
    public bool SourceHanEllipsis;
    public bool DebugMode;
    public SubsetBackend Backend;
}

public enum SubsetBackend
{
    PyFontTools = 1,
    HarfBuzzSubset = 2,
}