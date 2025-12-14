using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G2LL1
{
    using FollowSet = Dictionary<string, HashSet<string>>;
    using FirstSet = Dictionary<string, HashSet<string>>;
    internal class FollowCalculator
    {
        public static FollowSet CalcFollowSet(Grammar grammar, FirstSet FIRST)
        {
            var FOLLOW = new FollowSet();
            foreach (var variable in grammar.Variables)
            {
                FOLLOW[variable] = new HashSet<string>();
            }
            FOLLOW[grammar.StartSymbol].Add("$");
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var (A, rights) in grammar.Productions)
                {
                    foreach (var right in rights)
                    {
                        for (int i = 0; i < right.Count; i++)
                        {
                            string B = right[i];

                            if (!grammar.Variables.Contains(B)) continue;

                            // beta = 后面部分
                            var beta = right.Skip(i + 1).ToList();

                            if (beta.Count > 0)
                            {
                                // FIRST(beta) - ep
                                foreach (var t in FIRST[beta[0]])
                                {
                                    if (t != Grammar.Epsilon && FOLLOW[B].Add(t))
                                        changed = true;
                                }

                                // 如果 FIRST(beta) 含 ep，则 FOLLOW(A) 加到 FOLLOW(B)
                                bool allNullable = beta.All(s => FIRST[s].Contains(Grammar.Epsilon));

                                if (allNullable)
                                {
                                    foreach (var f in FOLLOW[A])
                                        if (FOLLOW[B].Add(f))
                                            changed = true;
                                }
                            }
                            else
                            {
                                // beta 为空：FOLLOW(A) → FOLLOW(B)
                                foreach (var f in FOLLOW[A])
                                    if (FOLLOW[B].Add(f))
                                        changed = true;
                            }
                        }
                    }
                }
            }
            return FOLLOW;
        }
        public static string FollowSetToString(FollowSet FOLLOW)
        {
            StringBuilder sb = new();
            foreach (var (variable, followSet) in FOLLOW)
            {
                sb.AppendLine($"FOLLOW({variable}) = {{ {string.Join(", ", followSet)} }}, size: {FOLLOW[variable].Count}");
            }
            return sb.ToString();
        }
    }
}
