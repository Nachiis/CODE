using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace G2LR0
{
    using Productions = Dictionary<string, List<List<string>>>;
    internal class Grammar
    {
        public readonly static string Epsilon = "ε";
        public string StartSymbol { get; set; } = "";
        public HashSet<string> Variables { get; set; } = new();
        public HashSet<string> Terminals { get; set; } = new();
        public Productions Productions { get; set; } = new();
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Start Symbol: {StartSymbol}");
            sb.AppendLine("Variables: " + string.Join(", ", Variables));
            sb.AppendLine("Terminals: " + string.Join(", ", Terminals));
            sb.AppendLine("Productions:");
            foreach(var kvp in Productions)
            {
                string left = kvp.Key;
                foreach(var production in kvp.Value)
                {
                    sb.AppendLine($"  {left} -> {string.Join(" ", production)}");
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 检查所有非终结符是否都有产生式。
        /// </summary>
        /// <returns>True 表示合法</returns>
        public bool Check()
        {
            return Variables.All(v => Productions.ContainsKey(v));
        }

        public void Augment()
        {
            string oldStart = StartSymbol;
            StartSymbol = oldStart + "'";
            Variables.Add(StartSymbol);
            Productions[StartSymbol] = new List<List<string>> { new List<string> { oldStart } };
        }

        public bool IsTerminal(string symbol)
        {
            return Terminals.Contains(symbol);
        }
    }
}
