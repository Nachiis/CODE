using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// EPPlus
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace G2LL1
{
    using LL1Table = Dictionary<(string, string), List<string>>;
    internal static class LL1TableConstructor
    {
        public static LL1Table ConstructLL1Table(Grammar grammar, Dictionary<string, HashSet<string>> FIRST, Dictionary<string, HashSet<string>> FOLLOW)
        {
            var table = new LL1Table();

            foreach (var (A, rights) in grammar.Productions)
            {
                foreach (var right in rights)
                {
                    var first = FirstOfString(right, FIRST);

                    foreach (var t in first)
                    {
                        if (t == Grammar.Epsilon) continue;

                        table[(A, t)] = right;
                    }

                    if (first.Contains(Grammar.Epsilon))
                    {
                        foreach (var b in FOLLOW[A])
                        {
                            table[(A, b)] = right;
                        }
                    }
                }
            }

            return table;
        }
        public static void ExportToExcel(
            Dictionary<(string, string), List<string>> table,
            Grammar grammar,
            string filePath)
        {
            ExcelPackage.License.SetNonCommercialPersonal("asdasd");
            using var package = new ExcelPackage();

            var ws = package.Workbook.Worksheets.Add("LL(1) Table");

            // ==== 列标题：所有终结符 + $ ====
            var terminals = grammar.Terminals.ToList();
            if (!terminals.Contains("$"))
                terminals.Add("$");

            ws.Cells[1, 1].Value = "NonTerminal";

            for (int j = 0; j < terminals.Count; j++)
                ws.Cells[1, j + 2].Value = terminals[j];

            // ==== 行标题：所有非终结符 ====
            var variables = grammar.Variables.ToList();

            for (int i = 0; i < variables.Count; i++)
            {
                string A = variables[i];
                ws.Cells[i + 2, 1].Value = A;

                for (int j = 0; j < terminals.Count; j++)
                {
                    string a = terminals[j];
                    var key = (A, a);

                    if (table.ContainsKey(key))
                    {
                        var prod = table[key];
                        string alpha = string.Join(" ", prod);
                        ws.Cells[i + 2, j + 2].Value = $"{A} → {alpha}";
                    }
                    else
                    {
                        ws.Cells[i + 2, j + 2].Value = "";
                    }
                }
            }

            // ==== 美化表格 ====
            using (var range = ws.Cells[1, 1, variables.Count + 1, terminals.Count + 1])
            {
                range.AutoFitColumns();
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                range.Style.Border.Top.Style =
                range.Style.Border.Bottom.Style =
                range.Style.Border.Left.Style =
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            // 保存文件
            package.SaveAs(new FileInfo(filePath));
        }
        public static string LL1TableToString(LL1Table table)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ((variable, terminal), production) in table)
            {
                string productionStr = string.Join(" ", production);
                sb.AppendLine($"M[{variable}, {terminal}] = {variable} -> {productionStr}");
            }
            return sb.ToString();
        }
        /// <summary>
        /// FIRST(字符串)
        /// </summary>
        private static HashSet<string> FirstOfString(
            List<string> str,
            Dictionary<string, HashSet<string>> FIRST)
        {
            HashSet<string> result = new();

            if (str.Count == 1 && str[0] == Grammar.Epsilon)
            {
                result.Add(Grammar.Epsilon);
                return result;
            }

            foreach (var symbol in str)
            {
                foreach (var x in FIRST[symbol])
                {
                    if (x != Grammar.Epsilon)
                        result.Add(x);
                }

                if (!FIRST[symbol].Contains(Grammar.Epsilon))
                    return result;
            }

            result.Add(Grammar.Epsilon);

            return result;
        }
        /// <summary>
        /// 检查文法是否是LL(1)文法
        /// </summary>
        public static bool IsLL1Grammar(
                Grammar grammar,
                Dictionary<string, HashSet<string>> FIRST,
                Dictionary<string, HashSet<string>> FOLLOW,
                out string conflictMessage)
        {
            StringBuilder sb = new();

            foreach (var A in grammar.Variables)
            {
                var rights = grammar.Productions[A];

                // 两两比较产生式
                for (int i = 0; i < rights.Count; i++)
                {
                    var alpha = rights[i];
                    var FIRST_alpha = FirstOfString(alpha, FIRST);

                    for (int j = i + 1; j < rights.Count; j++)
                    {
                        var beta = rights[j];
                        var FIRST_beta = FirstOfString(beta, FIRST);

                        // -------- 1. FIRST/FIRST 冲突 --------
                        var interFF = FIRST_alpha.Intersect(FIRST_beta).ToList();
                        if (interFF.Count > 0)
                        {
                            sb.AppendLine(
                                $"冲突: 对 左部 = {A}, 产生式 {ProdStr(alpha)} 和 {ProdStr(beta)} 的 FIRST 集相交：{{ {ProdStr(interFF)} }}");
                        }

                        // -------- 2. FIRST/FOLLOW 冲突（ε 情况） --------
                        if (FIRST_alpha.Contains(Grammar.Epsilon))
                        {
                            var interAF = FIRST_beta.Intersect(FOLLOW[A]).ToList();
                            if (interAF.Count > 0)
                            {
                                sb.AppendLine(
                                    $"冲突: 对 左部 = {A}, 产生式 {ProdStr(alpha)} 可推出 ε，且 FIRST({ProdStr(beta)}) 与 FOLLOW(A) 相交：{{ {ProdStr(interAF)} }}"
                                    );
                            }
                        }

                        if (FIRST_beta.Contains(Grammar.Epsilon))
                        {
                            var interBF = FIRST_alpha.Intersect(FOLLOW[A]).ToList();
                            if (interBF.Count > 0)
                            {
                                sb.AppendLine(
                                    $"冲突: 对 左部 = {A}, 产生式 {ProdStr(beta)} 可推出 ε，且 FIRST({ProdStr(alpha)}) 与 FOLLOW(A) 相交：{{ {ProdStr(interBF)} }}"
                                    );
                            }
                        }
                    }
                }
            }

            conflictMessage = sb.ToString();
            return conflictMessage.Length == 0;
        }
        private static string ProdStr(List<string> p)
            => string.Join(" ", p);
    }
}
