using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace G2LR0
{
    using LR0Table = (
            Dictionary<(int stateIndex, string symbol), int> Goto,
            Dictionary<(int stateIndex, string terminal), Action> Action
        );
    public enum ActionKind { Shift, Reduce, Accept }
    public readonly record struct Action(ActionKind Kind, int Value);


    internal class LR0TableConstructor
    {
        /// <summary>
        /// 给定文法的产生式，生成所有的LR(0)项目
        /// </summary>
        /// <param name="productions"></param>
        /// <returns></returns>
        public static List<Item> GenerateItems(Dictionary<string, List<List<string>>> productions)
        {
            List<Item> items = new();
            int index = 1;

            foreach (var (left, right) in productions)
            {
                int viablePrefixIndex = 0;
                foreach (var production in right)
                {
                    if (production.Count == 1 && production[0] == Grammar.Epsilon)
                    {
                        items.Add(new Item(left, production, 0, index));
                        index++;
                        continue;
                    }
                    for (int i = 0; i <= production.Count; i++)
                    {
                        items.Add(new Item(left, production, viablePrefixIndex, index));
                        viablePrefixIndex++;
                    }
                    viablePrefixIndex = 0;
                    index++;
                }
            }
            return items;
        }
        /// <summary>
        /// 给定一个项目列表，生成一个查询字典，键为(产生式编号, 可行前缀编号)，值为对应的项目
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Dictionary<(int index, int viablePrefixIndex), Item> GenerateItemsDictionary(List<Item> items)
        {
            Dictionary<(int, int), Item> dict = new();
            foreach (var item in items)
            {
                dict[(item.index, item.viablePrefixIndex)] = item;
            }
            return dict;
        }
        /// <summary>
        /// 给定一个项目列表，生成一个索引字典，键为产生式左部，值为产生式的编号集合
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static Dictionary<string, HashSet<int>> GenerateIndexOfProduction(List<Item> items)
        {
            Dictionary<string, HashSet<int>> dict = new();
            foreach (var item in items)
            {
                if (!dict.ContainsKey(item.left))
                {
                    dict[item.left] = new HashSet<int>();
                }
                dict[item.left].Add(item.index);
            }
            return dict;
        }
        public static LR0Table GenerateLR0Table(Dictionary<(int, int), (List<Item>, int)> canonicalCollection, Grammar grammar)
        {
            LR0Table table = (new(), new());
            foreach (var (_, (items, sort)) in canonicalCollection)
            {
                foreach (var item in items)
                {
                    if (item.isEpsilon)
                    {
                        Console.WriteLine("遇到ε产生式，跳过");
                    }
                    // 规约
                    else if (item.viablePrefixIndex == item.right.Count)
                    {
                        if (item.left == grammar.StartSymbol)
                        {
                            table.Action[(sort, "#")] = new(ActionKind.Accept, -1);
                        }
                        else
                        {
                            foreach (var terminal in grammar.Terminals)
                            {
                                table.Action[(sort, terminal)] = new(ActionKind.Reduce, item.index);
                            }
                            table.Action[(sort, "#")] = new(ActionKind.Reduce, item.index);
                        }
                    }
                    // 移进
                    else
                    {
                        string symbol = item.right[item.viablePrefixIndex];
                        var (_, s) = canonicalCollection[(item.index, item.viablePrefixIndex + 1)];
                        if (grammar.IsTerminal(symbol))
                        {

                            table.Action[(sort, symbol)] = new(ActionKind.Shift, s);
                        }
                        else
                        {
                            table.Goto[(sort, symbol)] = s;
                        }
                    }
                }
            }
            return table;
        }
        public static void Export(
             string outputPath,
             LR0Table table,
             Grammar grammar,
             int stateCount,
             IReadOnlyList<Item> allItemsForProductionIndex // 用于 rK -> 产生式对照
         )
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("outputPath is null/empty.");

            ExcelPackage.License.SetNonCommercialPersonal("asdasd");

            // ACTION 列：所有终结符 + "#"
            var terminals = grammar.Terminals
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();
            if (!terminals.Contains("#"))
                terminals.Add("#");

            // GOTO 列：放非终结符（可选排除增广开始符号 S'）
            var nonTerminals = grammar.Variables
                .Where(v => v != grammar.StartSymbol)
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();

            using var package = new ExcelPackage();

            var ws = package.Workbook.Worksheets.Add("LR0");

            int rowHeader = 1;
            int col = 1;

            ws.Cells[rowHeader, col++].Value = "State";

            int actionStartCol = col;
            foreach (var t in terminals) ws.Cells[rowHeader, col++].Value = t;

            int gotoStartCol = col;
            foreach (var nt in nonTerminals) ws.Cells[rowHeader, col++].Value = nt;

            int lastCol = col - 1;

            ws.Cells[1, actionStartCol].AddComment("ACTION", "gen");
            ws.Cells[1, gotoStartCol].AddComment("GOTO", "gen");

            for (int s = 0; s < stateCount; s++)
            {
                int r = rowHeader + s + 1;
                ws.Cells[r, 1].Value = s;

                // ACTION
                for (int i = 0; i < terminals.Count; i++)
                {
                    string term = terminals[i];
                    int c = actionStartCol + i;

                    if (table.Action.TryGetValue((s, term), out var act))
                        ws.Cells[r, c].Value = FormatAction(act);
                }

                // GOTO
                for (int i = 0; i < nonTerminals.Count; i++)
                {
                    string nt = nonTerminals[i];
                    int c = gotoStartCol + i;

                    if (table.Goto.TryGetValue((s, nt), out int toState))
                        ws.Cells[r, c].Value = toState;
                }
            }

            // 样式
            using (var range = ws.Cells[rowHeader, 1, rowHeader, lastCol])
            {
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            ws.View.FreezePanes(2, 2);
            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            // 写文件
            var file = new FileInfo(outputPath);
            package.SaveAs(file);
        }

        private static string FormatAction(Action a)
        {
            return a.Kind switch
            {
                ActionKind.Shift => $"s{a.Value}",
                ActionKind.Reduce => $"r{a.Value}",
                ActionKind.Accept => "acc",
                _ => a.ToString()
            };
        }

        private static string ToProductionString(Item it)
        {
            if (it.isEpsilon) return $"{it.left} -> {Grammar.Epsilon}";
            return $"{it.left} -> {string.Join(" ", it.right)}";
        }
    }
}
