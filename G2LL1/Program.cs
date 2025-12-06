
namespace G2LL1
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Use: G2LL1 inputFilePath [outputFilePath]");
                return;
            }
            string inputFilePath = args[0];
            string? outputFilePath = args.Length == 2 ? args[1] : null;
            if(!File.Exists(inputFilePath))
            {
                Console.WriteLine($"Input file '{inputFilePath}' does not exist.");
                return;
            }
            var tokens = GrammarTokenizer.Tokenize(inputFilePath);
            for(int i = 0; i < tokens.Count; i++)
            {
                Console.WriteLine($"{i}: {tokens[i]}");
            }
            var grammar = GrammarParser.Parse(tokens);
            Console.WriteLine($"Parsed Grammar:{grammar}");

        }
    }
}