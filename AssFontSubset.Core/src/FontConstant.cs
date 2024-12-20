﻿namespace AssFontSubset.Core;

public static class FontConstant
{
    // Unicode 15.1
    public static Dictionary<char, char> VertMapping = new Dictionary<char, char>()
    {
        // Vertical Forms
        { '\u002c', '\ufe10' },
        { '\u3001', '\ufe11' },
        { '\u3002', '\ufe12' },
        { '\u003a', '\ufe13' },
        { '\u003b', '\ufe14' },
        { '\u0021', '\ufe15' },
        { '\u003f', '\ufe16' },
        { '\u3016', '\ufe17' },
        { '\u3017', '\ufe18' },
        { '\u2026', '\ufe19' },
        
        // CJK Compatibility Forms - Glyphs for vertical variants
        // { '', '\ufe30' },
        { '\u2014', '\ufe31' },
        { '\u2013', '\ufe32' },
        // { '\u005f', '\ufe33' },
        // { '', '\ufe34' },
        { '\u0028', '\ufe35' },
        { '\u0029', '\ufe36' },
        { '\u007b', '\ufe37' },
        { '\u007d', '\ufe38' },
        { '\u3014', '\ufe39' },
        { '\u3015', '\ufe3a' },
        { '\u3010', '\ufe3b' },
        { '\u3011', '\ufe3c' },
        { '\u300a', '\ufe3d' },
        { '\u300b', '\ufe3e' },
        { '\u2329', '\ufe3f' },
        { '\u232a', '\ufe40' },
        { '\u300c', '\ufe41' },
        { '\u300d', '\ufe42' },
        { '\u300e', '\ufe43' },
        { '\u300f', '\ufe44' },
        { '\u005b', '\ufe47' },
        { '\u005d', '\ufe48' },
    };

    public const int LanguageIdEnUs = 1033;
    
    
    // GDI doesn’t seem to use any features (may use vert?), and it has its own logic for handling vertical layout.
    // https://learn.microsoft.com/en-us/typography/opentype/spec/features_uz#tag-vrt2
    // GDI may according it:
    // OpenType font with CFF outlines to be used for vertical writing must have vrt2, otherwise fallback
    // OpenType font without CFF outlines use vert map default glyphs to vertical writing glyphs
        
    // https://github.com/libass/libass/pull/702
    // libass seems to be trying to use features like vert to solve this problem.
    // These are features related to vertical layout but are not enabled: "vchw", "vhal", "vkrn", "vpal", "vrtr".
    // https://github.com/libass/libass/blob/6e83137cdbaf4006439d526fef902e123129707b/libass/ass_shaper.c#L147
    public static readonly string[] SubsetKeepFeatures = [
        "vert", "vrtr",
        "vrt2",
        "vkna",
    ];
}