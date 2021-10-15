# AssFontSubset
使用 fonttools 生成 ASS 字幕文件的字体子集，并自动修改字体名称及 ASS 文件中对应的字体名称

## 依赖
1. [fonttools](https://github.com/fonttools/fonttools)
2. Path 环境变量中存在 pyftsubset.exe 和 ttx.exe

## 使用方法

### 基本使用方法

1. 建立一个新目录，放入 .ass 文件
2. 在新建立的目录中创建 fonts 目录，放入字体文件
3. 将 .ass 文件拖入窗口中，点击开始
4. 程序会自动生成 output 目录并放入修改后的 .ass 文件及子集化的字体文件

#### 注意：
1. 每次生成时会自动删除 ass 同目录下的 output 文件夹。
2. 如果有 full name 和 family name 不同的字体，请不要混用 full name 和 family name，否则会提示找不到 family name 所指名的字体。
3. 类似思源黑体那样普通和粗体同名的字体，如果两者在同一个 ttc 文件里，则会出错。请分成两个不同的文件再进行子集化。

### 选项

1. 居中思源省略号

   思源黑体和宋体的中文省略号在某些特殊的情况下会变成变成类似 ... 的下对齐。如果不打开此选项，子集化后的所有的省略号都变成下对齐。打开后，所有的省略号会被居中对齐。

2. 使用云跳过列表

   该功能暂未实现。

3. 使用本地跳过列表

   该功能暂未实现。

### 命令行

`assfontsubset [subtitle files]`

用命令行调用程序时，只需要把 .ass 文件名作为参数输入进去即可，支持多个文件。

`assfontsubset a.ass b.ass c.ass` 

命令行模式暂不支持设置其他选项，使用命令行模式前请先手动打开程序 GUI 配置成想要的选项，程序关闭时会自动记忆选项配置。



## Todo

1. 多线程查找字体名

2. 跳过列表
