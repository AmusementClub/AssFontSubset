# HarfBuzzBinding

This is a binding library for HarfBuzz, primarily targeting the use of `harfbuzz-subset`.

The dependent native libraries are provided by `MIR.NativeLib.Harfbuzz.*`, shared libraries are used for debugging, and static libraries are employed for release builds, so you can only `PublishAot` when publish release. The version of `MIR.NativeLib.Harfbuzz.*` is harfbuzz version + package patch version. Currently, it only supports the following RIDs:

- Windows:
    - win-x64
    - win-arm64
- Linux:
    - linux-musl-x64
    - linux-musl-arm64
- Mac:
    - osx-x64
    - osx-arm64