namespace multitarget
{
    using Antlr4.Runtime;
    using System.Text;

    public class Program
    {
        static void Main(string[] args)
        {
            Try("1 + 2 + 3");
            Try("1 2 + 3");
            Try("1 + +");
        }

        static void Try(string input)
        {
            var str = new AntlrInputStream(input);
            System.Console.WriteLine(input);
            var lexer = new abbLexer(str);
            var tokens = new CommonTokenStream(lexer);
            var parser = new abbParser(tokens);
            var listener_lexer = new ErrorListener<int>();
            var listener_parser = new ErrorListener<IToken>();
            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();
            lexer.AddErrorListener(listener_lexer);
            parser.AddErrorListener(listener_parser);
            var tree = parser.module();
            if (listener_lexer.had_error || listener_parser.had_error)
                System.Console.WriteLine("error in parse.");
            else
                System.Console.WriteLine("parse completed.");
            System.Console.WriteLine(tree.ToStringTree(parser));
        }
    }
}
