using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G2LL1
{
    internal enum GrammarTokenType
    {
        StartKeyword,
        Variable,
        Terminal,
        Arrow,
        Or,
        Colon,
        Epsilon,
        EndOfFile,
        EndOfProduction
    }
    internal struct GrammarToken
    {
        public GrammarTokenType Type { get; set; }
        public string Lexeme { get; set; }
        public GrammarToken(GrammarTokenType type, string lexeme)
        {
            Type = type;
            Lexeme = lexeme;
        }
        public override readonly string ToString()
        {
            return $"<{Type},{Lexeme}>";
        }
    }
    /// <summary>
    /// 给定一个文法文件，进行词法分析，生成词法单元序列。
    /// </summary>
    internal static class GrammarTokenizer
    {
        private static readonly char[] SingleCharTerminals =
        {
            '+','-','*','/','(',')','[',']','{','}','&','^','%','$','?','>','<','='
        };
        public static List<GrammarToken> Tokenize(string filePath)
        {
            List<GrammarToken> tokens = new();
            using (var reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0)
                    {
                        continue;
                    }
                    if (line.StartsWith("start"))
                    {
                        tokens.Add(new GrammarToken(GrammarTokenType.StartKeyword, "start"));
                        int colonIndex = line.IndexOf(':');
                        if (colonIndex < 0)
                        {
                            throw new Exception("start行后续需要有冒号。");
                        }
                        tokens.Add(new GrammarToken(GrammarTokenType.Colon, ":"));
                        string startVariable = line.Substring(colonIndex + 1).Trim();
                        tokens.Add(new GrammarToken(GrammarTokenType.Variable, startVariable));
                        continue;
                    }
                    ParseProduction(line, tokens);
                    tokens.Add(new GrammarToken(GrammarTokenType.EndOfProduction, ";"));
                }
                tokens.Add(new GrammarToken(GrammarTokenType.EndOfFile, "$"));
            }
            return tokens;
        }
        private static void ParseProduction(string line, List<GrammarToken> tokens)
        {
            int arrowIndex = line.IndexOf("->");
            if (arrowIndex < 0)
            {
                throw new Exception($"产生式需要包含'->'符号:{line}");
            }
            string lhs = line.Substring(0, arrowIndex).Trim();
            // todo: 检查lhs是否合法变量
            if (lhs.Length == 0)// 不能为空
            {
                throw new Exception($"产生式左侧不能为空:{line}");
            }
            if (!char.IsUpper(lhs[0])) // 必须以大写字母开头
            {
                throw new Exception($"产生式左侧必须是变量，且以大写字母开头:{line}");
            }
            if (lhs.Length != 1)// 后续必须是数字，如A·1,A·12等
            {
                string suffix = lhs.Substring(1);
                if (!suffix.All(c => char.IsDigit(c)))
                {
                    throw new Exception($"产生式左侧变量格式错误:{line}");
                }
            }
            tokens.Add(new GrammarToken(GrammarTokenType.Variable, lhs));
            tokens.Add(new GrammarToken(GrammarTokenType.Arrow, "->"));
            string rhs = line.Substring(arrowIndex + 2).Trim();
            string[] alternatives = rhs.Split('|');
            // todo: 检查每个alternative是否合法
            for (int i = 0; i < alternatives.Length; i++)
            {
                if (i > 0)
                {
                    tokens.Add(new GrammarToken(GrammarTokenType.Or, "|"));
                }
                string alternative = alternatives[i].Trim();
                if (alternative.Length == 0)
                {
                    throw new Exception($"产生式右侧的某个备选项不能为空:{line}");
                }
                // 右部比较复杂，需要逐字符解析,空产生式为 0
                ParseAlternative(alternative, tokens);
            }
        }
        private static void ParseAlternative(string alternative, List<GrammarToken> tokens)
        {
            int i = 0;
            if (alternative[0] == '0')
            {
                if (alternative.Length != 1)
                {
                    throw new Exception($"空产生式只能包含单个字符0: {alternative}");
                }
                tokens.Add(new GrammarToken(GrammarTokenType.Epsilon, Grammar.Epsilon));
                return;
            }
            while (i < alternative.Length)
            {
                char c = alternative[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }
                if (char.IsLetter(c))
                {
                    int begin = i;
                    i++;
                    // 变量或终结符
                    while (i < alternative.Length && char.IsDigit(alternative[i]))
                    {
                        i++;
                    }
                    string lexeme = alternative.Substring(begin, i - begin);
                    if (char.IsUpper(c))
                    {
                        tokens.Add(new GrammarToken(GrammarTokenType.Variable, lexeme));
                    }
                    else
                    {
                        tokens.Add(new GrammarToken(GrammarTokenType.Terminal, lexeme));
                    }
                }
                else if (SingleCharTerminals.Contains(c))
                {
                    tokens.Add(new GrammarToken(GrammarTokenType.Terminal, c.ToString()));
                    i++;
                }
                else
                {
                    throw new Exception($"产生式右侧包含非法字符:{alternative}");
                }
            }
        }
    }
}
