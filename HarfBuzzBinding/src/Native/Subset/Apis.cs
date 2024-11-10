global using hb_subset_input_t = System.IntPtr;
global using hb_subset_plan_t = System.IntPtr;
using System.Runtime.InteropServices;
using static HarfBuzzBinding.Native.Library;
// ReSharper disable InconsistentNaming

namespace HarfBuzzBinding.Native.Subset
{
    public static unsafe partial class Apis
    {
        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_subset_input_t* hb_subset_input_create_or_fail();

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_subset_input_t* hb_subset_input_reference(hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void hb_subset_input_destroy(hb_subset_input_t* input);

        // [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        // [return: NativeTypeName("hb_bool_t")]
        // public static extern int hb_subset_input_set_user_data(hb_subset_input_t* input, hb_user_data_key_t* key, void* data, [NativeTypeName("hb_destroy_func_t")] delegate* unmanaged[Cdecl]<void*, void> destroy, [NativeTypeName("hb_bool_t")] int replace);

        // [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        // public static extern void* hb_subset_input_get_user_data([NativeTypeName("const hb_subset_input_t *")] hb_subset_input_t* input, hb_user_data_key_t* key);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void hb_subset_input_keep_everything(hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_set_t* hb_subset_input_unicode_set(hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_set_t* hb_subset_input_glyph_set(hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_set_t* hb_subset_input_set(hb_subset_input_t* input, hb_subset_sets_t set_type);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_map_t* hb_subset_input_old_to_new_glyph_mapping(hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_subset_flags_t hb_subset_input_get_flags(hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void hb_subset_input_set_flags(hb_subset_input_t* input, [NativeTypeName("unsigned int")] uint value);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("hb_bool_t")]
        public static extern int hb_subset_input_pin_all_axes_to_default(hb_subset_input_t* input, hb_face_t* face);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("hb_bool_t")]
        public static extern int hb_subset_input_pin_axis_to_default(hb_subset_input_t* input, hb_face_t* face, [NativeTypeName("hb_tag_t")] uint axis_tag);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("hb_bool_t")]
        public static extern int hb_subset_input_pin_axis_location(hb_subset_input_t* input, hb_face_t* face, [NativeTypeName("hb_tag_t")] uint axis_tag, float axis_value);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_face_t* hb_subset_preprocess(hb_face_t* source);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_face_t* hb_subset_or_fail(hb_face_t* source, [NativeTypeName("const hb_subset_input_t *")] hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_face_t* hb_subset_plan_execute_or_fail(hb_subset_plan_t* plan);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_subset_plan_t* hb_subset_plan_create_or_fail(hb_face_t* face, [NativeTypeName("const hb_subset_input_t *")] hb_subset_input_t* input);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void hb_subset_plan_destroy(hb_subset_plan_t* plan);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_map_t* hb_subset_plan_old_to_new_glyph_mapping([NativeTypeName("const hb_subset_plan_t *")] hb_subset_plan_t* plan);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_map_t* hb_subset_plan_new_to_old_glyph_mapping([NativeTypeName("const hb_subset_plan_t *")] hb_subset_plan_t* plan);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_map_t* hb_subset_plan_unicode_to_old_glyph_mapping([NativeTypeName("const hb_subset_plan_t *")] hb_subset_plan_t* plan);

        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern hb_subset_plan_t* hb_subset_plan_reference(hb_subset_plan_t* plan);

        // [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        // [return: NativeTypeName("hb_bool_t")]
        // public static extern int hb_subset_plan_set_user_data(hb_subset_plan_t* plan, hb_user_data_key_t* key, void* data, [NativeTypeName("hb_destroy_func_t")] delegate* unmanaged[Cdecl]<void*, void> destroy, [NativeTypeName("hb_bool_t")] int replace);
        //
        // [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        // public static extern void* hb_subset_plan_get_user_data([NativeTypeName("const hb_subset_plan_t *")] hb_subset_plan_t* plan, hb_user_data_key_t* key);
        
        // HB_EXPERIMENTAL_API
        [DllImport(HarfBuzzSubsetDll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("hb_bool_t")]
        public static extern int hb_subset_input_override_name_table(hb_subset_input_t* input, [NativeTypeName("hb_ot_name_id_t")] uint name_id, [NativeTypeName("unsigned int")] uint platform_id, [NativeTypeName("unsigned int")] uint encoding_id, [NativeTypeName("unsigned int")] uint language_id, [NativeTypeName("const char *")] sbyte* name_str, int str_len);
    }
}
