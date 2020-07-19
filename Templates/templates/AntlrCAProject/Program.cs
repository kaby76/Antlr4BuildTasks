// Template generated code from Antlr4BuildTasks.Template v 8.1
namespace AntlrTemplate
{
    using Antlr4.Runtime;
    using System.Text;
    using System.Runtime.CompilerServices;

    public class Program
    {
        static bool show_tree = false;
        static bool show_tokens = false;
        static bool show_input = false;
        static bool have_files = false;

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-tree":
                        show_tree = true;
                        break;
                    case "-tokens":
                        show_tokens = true;
                        break;
                    case "-input":
                        show_input = true;
                        break;
                    default:
                        have_files = true;
                        break;
                }
            }
            if (have_files)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    if (args[i].StartsWith('-')) continue;
                    var fn = args[i];
                    var input = ReadAllInput(fn);
                    Try(input);
                }
            }
            else
            {
                Try("1 + 2 + 3");
                Try("1 2 + 3");
                Try("1 + +");
            }
        }

        static void Try(string input)
        {
            if (show_input)
            {
                System.Console.WriteLine("Input:");
                System.Console.WriteLine(input);
            }
            var str = new AntlrInputStream(input);
            var lexer = new arithmeticLexer(str);
            var tokens = new CommonTokenStream(lexer);
            var parser = new arithmeticParser(tokens);
            var listener = new ErrorListener<IToken>(parser, lexer, tokens);
            parser.AddErrorListener(listener);
            lexer.AddErrorListener(new ErrorListener<int>(parser, lexer, tokens));
            var tree = parser.file();
            if (listener.had_error)
                System.Console.WriteLine("error in parse.");
            else
                System.Console.WriteLine("parse completed.");
            if (show_tokens)
                System.Console.WriteLine(tokens.OutputTokens(lexer));
            if (show_tree)
                System.Console.WriteLine(tree.OutputTree(tokens));
        }

        static string ReadAllInput(string fn)
        {
            var input = System.IO.File.ReadAllText(fn);
            return input;
        }
    }
}
