CODE 文件夹为无符号数识别

ConvertToDFA 文件夹包含正规式转化为最小DFA的部分

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
