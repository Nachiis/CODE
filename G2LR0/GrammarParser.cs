using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G2LR0
{
    /// <summary>
    /// 对词法分析器生成的词法单元进行语法分析，生成文法对象。
    /// </summary>
    internal static class GrammarParser
    {
        public static Grammar Parse(List<GrammarToken> tokens)
        {
            Grammar grammar = new();
            int index = 0;

            void Expect(GrammarTokenType type)
            {
                if (index >= tokens.Count || tokens[index].Type != type)
                {
                    throw new Exception($"Unexpected token: {tokens.ElementAtOrDefault(index)}");
                }
                index++;
            }
            // 解析开始符号
            Expect(GrammarTokenType.StartKeyword);
            Expect(GrammarTokenType.Colon);
            grammar.StartSymbol = tokens[index].Lexeme;
            grammar.Variables.Add(grammar.StartSymbol);
            index++;
            // 解析产生式
            while (tokens[index].Type != GrammarTokenType.EndOfFile)
            {
                ParseProduction(grammar, tokens, ref index);
            }
            // 验证所有非终结符均有产生式
            if (!grammar.Check())
            {
                throw new Exception("Some variables have no productions.");
            }

            return grammar;
        }

        private static void ParseProduction(Grammar grammar, List<GrammarToken> tokens, ref int index)
        {
            if (tokens[index].Type != GrammarTokenType.Variable)
            {
                throw new Exception($"Left must be variable, but {tokens[index]}");
            }
            string left = tokens[index].Lexeme;
            grammar.Variables.Add(left);
            index++;
            if (tokens[index].Type != GrammarTokenType.Arrow)
            {
                throw new Exception($"Expected '->' after variable {left}, but found: {tokens[index]}");
            }
            index++;
            List<List<string>> productions = new();
            List<string> currentProduction = new();
            while (true)
            {
                GrammarToken token = tokens[index];
                index++;
                if (token.Type == GrammarTokenType.Variable)
                {
                    grammar.Variables.Add(token.Lexeme);
                    currentProduction.Add(token.Lexeme);
                }
                else if (token.Type == GrammarTokenType.Terminal)
                {
                    grammar.Terminals.Add(token.Lexeme);
                    currentProduction.Add(token.Lexeme);
                }
                else if (token.Type == GrammarTokenType.Epsilon)
                {
                    currentProduction.Add(Grammar.Epsilon);
                }
                else if (token.Type == GrammarTokenType.Or)
                {
                    productions.Add(currentProduction);
                    currentProduction = new();
                }
                else if (token.Type == GrammarTokenType.EndOfProduction)
                {
                    productions.Add(currentProduction);
                    break;
                }
                else
                {
                    throw new Exception($"Unexpected token in production: {token}");
                }
            }
            if (grammar.Productions.ContainsKey(left))
            {
                grammar.Productions[left].AddRange(productions);
            }
            else
            {
                grammar.Productions[left] = productions;
            }
        }
    }
}
