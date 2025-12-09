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
                        if (t == "") continue;

                        table[(A, t)] = right;
                    }

                    if (first.Contains(""))
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
                        string alpha = prod.Count == 1 && prod[0] == "" ? "ε" : string.Join(" ", prod);
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
                string productionStr = production.Count == 1 && production[0] == "" ? "ε" : string.Join(" ", production);
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

            if (str.Count == 1 && str[0] == "")
            {
                result.Add("");
                return result;
            }

            foreach (var symbol in str)
            {
                foreach (var x in FIRST[symbol])
                {
                    if (x != "")
                        result.Add(x);
                }

                if (!FIRST[symbol].Contains(""))
                    return result;
            }

            result.Add("");

            return result;
        }
    }
}
