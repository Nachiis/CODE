

namespace G2LL1
{
    class Program
    {
        public static void Main(string[] args)
        {
            System.Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
            System.Console.WriteLine($"args count:{args.Length}");
            foreach (var arg in args)
            {
                System.Console.WriteLine($"arg:{arg}");
            }
        }
    }
}