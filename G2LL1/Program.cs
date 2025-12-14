
namespace G2LL1
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Use: G2LL1 inputFilePath [outputFilePath.xlsx]");
                return;
            }
            string inputFilePath = args[0];
            string? outputFilePath = null;
            if (args.Length == 2)
            {
                if (Path.GetExtension(args[1]).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    outputFilePath = args[1];
                }
                else
                {
                    Console.WriteLine("Output file must have .xlsx extension.");
                    return;
                }
            }
                

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"Input file '{inputFilePath}' does not exist.");
                return;
            }
            var tokens = GrammarTokenizer.Tokenize(inputFilePath);
            for (int i = 0; i < tokens.Count; i++)
            {
                Console.WriteLine($"{i}: {tokens[i]}");
            }
            var grammar = GrammarParser.Parse(tokens);
            Console.WriteLine($"Parsed Grammar:\n{grammar}");
            var firstSets = FirstCalculator.CalcFirstSet(grammar);
            string firstSetStr = FirstCalculator.FirstSetToString(firstSets);
            Console.WriteLine("First Sets:");
            Console.WriteLine(firstSetStr);
            var followSets = FollowCalculator.CalcFollowSet(grammar, firstSets);
            string followSetStr = FollowCalculator.FollowSetToString(followSets);
            Console.WriteLine("Follow Sets:");
            Console.WriteLine(followSetStr);
            // 检查是否是 ll1 文法
            bool isLL1 = LL1TableConstructor.IsLL1Grammar(grammar, firstSets, followSets, out string conflictMessage);
            if(!isLL1)
            {
                Console.WriteLine("The grammar is not LL(1):");
                Console.WriteLine(conflictMessage);
                return;
            }

            var ll1Table = LL1TableConstructor.ConstructLL1Table(grammar, firstSets, followSets);
            string ll1TableStr = LL1TableConstructor.LL1TableToString(ll1Table);
            Console.WriteLine("LL(1) Parsing Table:");
            Console.WriteLine(ll1TableStr);

            if (outputFilePath != null)
            {
                LL1TableConstructor.ExportToExcel(ll1Table, grammar, outputFilePath);
                Console.WriteLine($"LL(1) Parsing Table exported to '{outputFilePath}'.");
            }
        }
    }
}