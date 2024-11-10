using System.Runtime.InteropServices;
using System.Text;
using HarfBuzzBinding.Native;
using SubsetApis = HarfBuzzBinding.Native.Subset.Apis;
using HBApis = HarfBuzzBinding.Native.Apis;

namespace HarfBuzzBinding;

public unsafe class Methods
{
    public static string? GetHarfBuzzVersion() => Marshal.PtrToStringAnsi((IntPtr)HBApis.hb_version_string());
    
    public static bool TryGetFontFace(string fontFile, uint faceIndex, out hb_face_t* face)
    {
        var harfBuzzVersion = new Version(GetHarfBuzzVersion()!);
        
        if (harfBuzzVersion is { Major: >= 10, Minor: >= 1 })
        {
            face = HBApis.hb_face_create_from_file_or_fail((sbyte*)Marshal.StringToHGlobalAnsi(fontFile), faceIndex);
            return (IntPtr)face != IntPtr.Zero;
        }
        
        var blob = HBApis.hb_blob_create_from_file_or_fail((sbyte*)Marshal.StringToHGlobalAnsi(fontFile));
        if ((IntPtr)blob == IntPtr.Zero)
        {
            face = (hb_face_t*)IntPtr.Zero;
            return false;
        }
        
        face = HBApis.hb_face_create(blob, faceIndex);
        HBApis.hb_blob_destroy(blob);
        return true;
    }
    
    public static void WriteFontFile(hb_blob_t* blob, string destFile)
    {
        uint length;
        var dataPtr = HBApis.hb_blob_get_data(blob, &length);
        var stream = new UnmanagedMemoryStream((byte*)dataPtr, length);

        using var fileStream = new FileStream(destFile, FileMode.Create);
        stream.CopyTo(fileStream);
        stream.Dispose();
        
        HBApis.hb_blob_destroy(blob);
    }
    
    public static void RenameFontname(hb_subset_input_t* input, sbyte* versionString, sbyte* nameString, OpenTypeNameId[] ids)
    {
        foreach (var id in ids)
        {
            _ = SubsetApis.hb_subset_input_override_name_table(input, id.NameId, id.PlatformId, id.EncodingId, id.LanguageId, id.NameId == 0 ? versionString : nameString, -1);
        }
    }
}

public struct OpenTypeNameId
{
    public uint NameId;
    public uint PlatformId;
    public uint LanguageId;
    public uint EncodingId;
}