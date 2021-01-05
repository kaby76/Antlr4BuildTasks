// Template generated code from Antlr4BuildTasks.Template v 8.10
namespace TemplateNamespace
{
    using Antlr4.Runtime;
    using System.Text;

    public class Program
    {
        static bool have_files = false;
        static void Main(string[] args)
        {
            have_files = args.Length > 0;
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
            var str = new AntlrInputStream(input);
            System.Console.WriteLine(input);
            var lexer = new MyLexerGrammar(str);
            var tokens = new CommonTokenStream(lexer);
            var parser = new MyParserGrammar(tokens);
            var listener_lexer = new ErrorListener<int>();
            var listener_parser = new ErrorListener<IToken>();
            lexer.AddErrorListener(listener_lexer);
            parser.AddErrorListener(listener_parser);
            var tree = parser.TemplateStart();
            if (listener_lexer.had_error || listener_parser.had_error)
                System.Console.WriteLine("error in parse.");
            else
                System.Console.WriteLine("parse completed.");
        }

        static string ReadAllInput(string fn)
        {
            var input = System.IO.File.ReadAllText(fn);
            return input;
        }
    }
}
