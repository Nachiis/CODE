namespace G2LR0
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Use: G2LR0 inputFilePath [outputFilePath.xlsx]");
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
            grammar.Augment();
            Console.WriteLine($"Parsed Grammar:\n{grammar}");
            // 生成所有项目
            var items = LR0TableConstructor.GenerateItems(grammar.Productions);
            var ItemDictionary = LR0TableConstructor.GenerateItemsDictionary(items);
            var ItemIndex = LR0TableConstructor.GenerateIndexOfProduction(items);
            Console.WriteLine($"Generated {items.Count} Items:");
            foreach (var item in items)
            {
                Console.WriteLine(item);
            }
            Item start = items.First(i => i.left == grammar.StartSymbol && i.viablePrefixIndex == 0);

            Console.WriteLine($"Start Item: {start}");
            Dictionary<(int index, int viablePrefixIndex), (List<Item> items, int sort)>
                canonicalCollection = new();

            Queue<List<Item>> toProcess = new();
            var startClosure = start.GenerateClosure(ItemDictionary, ItemIndex, grammar);
            toProcess.Enqueue(startClosure);
            int sortIndex = 0;
            while (toProcess.TryDequeue(out var result))
            {
                // 每个状态的第一个项目总是不同的，可以用来作为状态的标识
                Item identification = result[0];
                if (canonicalCollection.ContainsKey((identification.index, identification.viablePrefixIndex)))
                {
                    continue;
                }
                canonicalCollection[(identification.index, identification.viablePrefixIndex)] = (result, sortIndex++);
                // 计算从该状态出发的所有可能的转移
                foreach (var subItem in result)
                {
                    if (subItem.TryGoto(ItemDictionary, out var gotoItem))
                    {
                        toProcess.Enqueue(gotoItem.GenerateClosure(ItemDictionary, ItemIndex, grammar));
                    }
                }
            }
            Console.WriteLine($"Canonical Collection has {canonicalCollection.Count} states.");
            foreach (var (_, (value, sort)) in canonicalCollection)
            {
                Console.Write($"State {sort} :");
                foreach (var item in value)
                {
                    Console.Write(item);
                }
                Console.WriteLine();
            }
            var LR0Table =
                LR0TableConstructor.GenerateLR0Table(canonicalCollection, grammar);
            Console.WriteLine("LR(0) Parsing Table:");
            foreach (var gotoEntry in LR0Table.Goto)
            {
                Console.WriteLine($"Goto[State {gotoEntry.Key.stateIndex}, Symbol '{gotoEntry.Key.symbol}'] = State {gotoEntry.Value}");
            }
            foreach (var actionEntry in LR0Table.Action)
            {
                Console.WriteLine($"Action[State {actionEntry.Key.stateIndex}, Terminal '{actionEntry.Key.terminal}'] = {actionEntry.Value}");
            }
            if (outputFilePath != null)
            {
                LR0TableConstructor.Export(
                    outputFilePath,
                    LR0Table,
                    grammar,
                    canonicalCollection.Count,
                    items
                );
                Console.WriteLine($"LR(0) table exported to: {outputFilePath}");
            }
        }
    }
}