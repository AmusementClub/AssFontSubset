# AssFontSubset
使用 fonttools 生成 ASS 字幕文件的字体子集，并自动修改字体名称及 ASS 文件中对应的字体名称

## 依赖
1. [fonttools](https://github.com/fonttools/fonttools)
2. Path 环境变量中存在 pyftsubset.exe 和 ttx.exe

## 使用方法
1. 建立一个新目录，放入 .ass 文件
2. 在新建立的目录中创建 fonts 目录，放入字体文件
3. 将 .ass 文件拖入窗口中，点击开始
4. 程序会自动生成 output 目录并放入修改后的 .ass 文件及子集化的字体文件
