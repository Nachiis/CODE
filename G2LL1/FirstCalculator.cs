using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace G2LL1
{
    using FirstSet = Dictionary<string, HashSet<string>>;
    internal class FirstCalculator
    {
        public static FirstSet CalcFirstSet(Grammar grammar)
        {
            var FIRST = new FirstSet();
            foreach(var terminal in grammar.Terminals)
            {
                FIRST[terminal] = new HashSet<string> { terminal };
            }
            foreach(var variable in grammar.Variables)
            {
                FIRST[variable] = new HashSet<string>();
            }
            FIRST[Grammar.Epsilon] = new HashSet<string> { Grammar.Epsilon };
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var (left,rights) in grammar.Productions)
                {
                    foreach(var right in rights)
                    { 
                        // 空
                        if(right.Count == 1 && right[0] == Grammar.Epsilon)
                        {
                            
                            if (!FIRST[left].Contains(Grammar.Epsilon))
                            {
                                FIRST[left].Add(Grammar.Epsilon);
                                changed = true;
                            }
                            continue;
                        }
                        // A-> X1 X2 ... Xn
                        for (int i = 0; i < right.Count; i++)
                        {
                            var symbol = right[i];
                            foreach (var firstSymbol in FIRST[symbol])
                            {
                                if (firstSymbol != Grammar.Epsilon && FIRST[left].Add(firstSymbol))
                                {
                                    changed = true;
                                }
                            }
                            if (!FIRST[symbol].Contains(Grammar.Epsilon))
                            {
                                break;
                            }
                            else
                            {
                                // X1 X2 ... Xn 全部能推导出 ε
                                if (i == right.Count - 1)
                                {
                                    if (FIRST[left].Add(Grammar.Epsilon))
                                    {
                                        changed = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return FIRST;
        }
        public static string FirstSetToString(FirstSet firstSet)
        {
            StringBuilder sb = new();
            foreach (var (symbol, firsts) in firstSet)
            {
                sb.AppendLine($"FIRST({symbol}) = {{ {string.Join(", ", firsts)} }}, size: {firstSet[symbol].Count}");
            }
            return sb.ToString();
        }
    }
}
