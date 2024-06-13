# AssFontSubset

使用 fonttools 生成 ASS 字幕文件的字体子集，并自动修改字体名称及 ASS 文件中对应的字体名称

## 依赖

1. [fonttools](https://github.com/fonttools/fonttools)，推荐使用最新版本
2. Path 环境变量中存在 pyftsubset 和 ttx，也可以通过选项指定所在目录，此二者是 fonttools 的一部分

## AssFontSubset.Console

```
Usage:
  AssFontSubset.Console [<path>...] [options]

Arguments:
  <path>  要子集化的 ASS 字幕文件路径，可以输入多个同目录的字幕文件

Options:
  --fonts <fonts>        ASS 字幕文件需要的字体所在目录，默认为 ASS 同目录的 fonts 文件夹
  --output <output>      子集化后成品所在目录，默认为 ASS 同目录的 output 文件夹
  --bin-path <bin-path>  指定 pyftsubset 和 ttx 所在目录。若未指定，会使用环境变量中的
  --source-han-ellipsis  使思源黑体和宋体的省略号居中对齐 [default: True]
  --debug                保留子集化期间的各种临时文件，位于 --output-dir 指定的文件夹；同时打印出所有运行的命令 [default: False]
  --version              Show version information
  -?, -h, --help         Show help and usage information
```

## AssFontSubset.Avalonia

大致可以参考 [./README_v1.md] 中的 gui 使用方法，目前不支持显示实时的子集化进度。

## 注意

1. 每次生成时会自动删除 ass 同目录下的 output 文件夹。
2. 目前，子集化后不支持任何 OpenType layout 特性，可参照[此处](https://github.com/AmusementClub/AssFontSubset/issues/13)。

## Todo

1. 考虑移除 gui 支持
2. 考虑增加对 hb-subset 和 fontations subset 的支持
3. 不确定是否要支持可变字体（variable fonts）

## FAQ 常见问题和故障排除

1. 如果弹出的错误信息中有提到`请尝试使用 FontForge 重新生成字体。`： 请下载并安装 [Fontforge](https://fontforge.org/en-US/)，然后使用 Fontforge 打开有问题的字体，不需要改动任何信息，直接点文件——生成字体（File - Generate Font），然后生成一个新的字体文件，无视中途弹出的警告。再使用新生成的字体进行子集化操作。

2. 如果 Fontforge 无法解决问题，或出现奇怪的错误，且没有有用的错误信息，请尝试更新 fonttools:

```
pip3 install --upgrade fonttools
```

3. 其他已知问题，多数为字幕渲染器问题：
   [在竖排字体的符号可能会出现问题](https://github.com/AmusementClub/AssFontSubset/issues/5)
   [一些 otf 字体竖排时子集化后，字体大小在 vsfilter 中显示不正常](https://github.com/AmusementClub/AssFontSubset/issues/2)

4. 若有无法解决的问题，欢迎回报 issue