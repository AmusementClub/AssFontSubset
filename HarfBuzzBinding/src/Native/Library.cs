﻿global using hb_blob_t = System.IntPtr;
global using hb_buffer_t = System.IntPtr;
global using hb_face_t = System.IntPtr;
global using hb_font_funcs_t = System.IntPtr;
global using hb_font_t = System.IntPtr;
global using hb_language_impl_t = System.IntPtr;
global using hb_map_t = System.IntPtr;
global using hb_set_t = System.IntPtr;
global using hb_shape_plan_t = System.IntPtr;
global using hb_unicode_funcs_t = System.IntPtr;

namespace HarfBuzzBinding.Native;

internal static class Library
{
    internal const string HarfBuzzDll = "harfbuzz";
    internal const string HarfBuzzSubsetDll = "harfbuzz-subset";
}