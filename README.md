CODE 文件夹为无符号数识别

ConvertToDFA 文件夹包含正规式转化为最小DFA的部分

正规式文件可包含多条正规式，格式为
```
(a|b)*b
(00|11)*((01|10)(00|11)*(01|10)(00|11)*)*
1*
```

G2LL1 文件夹包含将给定文法转化为LL1分析表的部分

文法格式为：
```
start:E
E->TE1
E1->+TE1|0
T->FT1
T1->*FT1|0
F->(E)|i
```

G2LR0 文件夹包含将给定文法转化为LR0分析表的部分，文法格式与LL1相同。

DFA转换，生成LL1，生成LR0的使用方式：

```
ConvertToDFA <input_file> [output_file]
example:
ConvertToDFA input.txt output.txt
```

```
G2LL1 inputFilePath [outputFilePath.xlsx]
example:
G2LL1 input1.txt output1.xlsx
```

```
G2LR0 inputFilePath [outputFilePath.xlsx]
example:
G2LR0 input1.txt output1.xlsx
```
