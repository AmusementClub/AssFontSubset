# AssFontSubset
使用 fonttools 生成 ASS 字幕文件的字体子集，并自动修改字体名称及 ASS 文件中对应的字体名称

## 依赖
1. [.Net SDK 7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
   
   不要安装 runtime 而是要安装 SDK。虽说只安装 runtime 也可以使用，但不知为何程序启动会耗时很久，换成 SDK 后秒开。我也不知道微软是怎么搞的。

   </br>
   
2. 安装 python 3 并安装 [fonttools](https://github.com/fonttools/fonttools)
   
   可以使用 pip 直接安装
   ```
   pip3 install fonttools
   ```
   亦或者采用 fonttools 说明中的任意方法。

   </br>

3. Path 环境变量中存在 pyftsubset.exe 和 ttx.exe
   
   如果成功安装 fonttools, pyftsubset.exe 和 ttx.exe 会自动添加到 Path 环境变量中。


## 使用方法

### 基本使用方法

1. 建立一个新目录，放入 .ass 文件
2. 在新建立的目录中创建 fonts 目录，放入字体文件
3. 将 .ass 文件拖入窗口中，点击开始
4. 程序会自动生成 output 目录并放入修改后的 .ass 文件及子集化的字体文件

#### 注意：
1. 每次生成时会自动删除 ass 同目录下的 output 文件夹。

### 选项

1. 居中思源省略号
   
   默认：打开
   
   思源黑体和宋体的中文省略号在某些特殊的情况下会变成变成类似 ... 的下对齐。如果不打开此选项，子集化后的所有的省略号都变成下对齐。打开后，所有的省略号会被居中对齐。

2. 调试选项
   
   默认：关闭
   
   打开后会保留各种临时文件，用于检查字体名字等在各个阶段是否正确。

3. 使用云跳过列表
   
   默认：打开。
   
   打开后会尝试读取 GitHub 上已判明子集化后会出现问题的字体列表，并将其跳过不进行处理。
   
   使用此功能必须要可以连接到 GitHub。

   欢迎大家报告子集化后有问题的字体。

4. 使用本地跳过列表

   默认：打开

   使用方法：在 exe 文件同目录下使用 utf-8 编码创建 skiplist.txt，然后将自己想要跳过，不进行子集化处理的字体名字填入其中，以换行分割。

   注意
   - 要填写 ass 文件中使用的名字。（例：如果你在 ass 中使用的 `Source Han Sans SC Medium`，那该文件中也要填写相同的名字，而不能填写 `思源黑体 Medium`）。
   - 程序依旧会把跳过的未经子集化的字体复制到 output 文件夹下，该行为是为了便于自动化 remux。
   - 如果跳过的字体是属于一个 ttc，那整个 ttc 都会被复制到 output 文件夹下。

   示例 skiplist.txt：
   ```
   Source Han Sans SC Medium
   思源黑体 Medium
   方正兰亭圆_GBK_特
   A-OTF Shin Maru Go Pr6N H
   ```

### 命令行

`assfontsubset [subtitle files]`

用命令行调用程序时，只需要把 .ass 文件名作为参数输入进去即可，支持多个文件。

`assfontsubset a.ass b.ass c.ass` 

命令行模式暂不支持设置其他选项，使用命令行模式前请先手动打开程序 GUI 配置成想要的选项，程序关闭时会自动记忆选项配置。



## Todo

1. 多线程查找字体名

## FAQ 常见问题和故障排除

1. 如果弹出的错误信息中有提到`请尝试使用 FontForge 重新生成字体。`： 请下载并安装 [Fontforge](https://fontforge.org/en-US/)，然后使用 Fontforge 打开有问题的字体，不需要改动任何信息，直接点文件——生成字体（File - Generate Font），然后生成一个新的字体文件，无视中途弹出的警告。再使用新生成的字体进行子集化操作。


2. 如果 Fontforge 无法解决问题，或出现奇怪的错误，且没有有用的错误信息，请尝试更新 fonttools:

```
pip3 install --upgrade fonttools
```

3. 其他已知问题：
   [在竖排字体的符号可能会出现问题](https://github.com/tastysugar/AssFontSubset/issues/5)
   [一些 otf 字体竖排时子集化后，字体大小在 vsfilter 中显示不正常](https://github.com/tastysugar/AssFontSubset/issues/2)