using System.Runtime.InteropServices;
using static HarfBuzzBinding.Native.Library;
// ReSharper disable InconsistentNaming

namespace HarfBuzzBinding.Native;

public static unsafe partial class Apis
{
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern sbyte* hb_version_string();
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern hb_blob_t* hb_blob_create_from_file_or_fail(sbyte* file_name);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void* hb_blob_get_data(hb_blob_t* blob, uint* length);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void hb_blob_destroy(hb_blob_t* blob);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern hb_face_t* hb_face_create(hb_blob_t* blob, uint index);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern hb_face_t* hb_face_create_from_file_or_fail(sbyte* file_name, uint index);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern hb_blob_t* hb_face_reference_blob(hb_face_t* face);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void hb_face_destroy(hb_face_t* face);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void hb_set_add(hb_set_t* set, [NativeTypeName("hb_codepoint_t")] uint codepoint);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void hb_set_clear(hb_set_t* set);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void hb_set_destroy(hb_set_t* set);
    
    [DllImport(HarfBuzzDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("hb_tag_t")]
    public static extern uint hb_tag_from_string(sbyte* str, int len);
}