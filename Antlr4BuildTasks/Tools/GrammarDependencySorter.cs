using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Antlr4.Build.Tasks.Tools
{
    /// <summary>
    /// Orders grammars as trgen does: imported grammars and token vocabularies
    /// must be generated before the grammars that use them.
    /// </summary>
    internal static class GrammarDependencySorter
    {
        private static readonly Regex Declaration = new Regex(
            @"(?m)^\s*(?:(?<kind>lexer|parser)\s+)?grammar\s+(?<name>[A-Za-z_]\w*)\s*;");
        private static readonly Regex Imports = new Regex(
            @"(?m)^\s*import\s+(?<names>[^;{}]+)\s*;");
        private static readonly Regex Options = new Regex(
            @"\boptions\s*\{(?<body>[^{}]*)\}");
        private static readonly Regex TokenVocab = new Regex(
            @"\btokenVocab\s*=\s*(?<name>[A-Za-z_]\w*)\s*;");
        private static readonly Regex Identifier = new Regex(@"^[A-Za-z_]\w*$");

        private sealed class Grammar
        {
            internal string File;
            internal string Name;
            internal string Kind;
            internal readonly List<string> Dependencies = new List<string>();
        }

        internal static List<string> Sort(IEnumerable<string> files)
        {
            var grammars = files.Select(ReadGrammar).ToList();
            var byName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < grammars.Count; i++)
            {
                if (byName.ContainsKey(grammars[i].Name))
                    throw new InvalidOperationException(
                        $"Multiple grammar files declare '{grammars[i].Name}'.");
                byName.Add(grammars[i].Name, i);
            }

            var successors = new List<HashSet<int>>();
            var indegree = new int[grammars.Count];
            for (var i = 0; i < grammars.Count; i++)
                successors.Add(new HashSet<int>());

            for (var i = 0; i < grammars.Count; i++)
            {
                foreach (var name in grammars[i].Dependencies)
                {
                    if (byName.TryGetValue(name, out var dependency) &&
                        successors[dependency].Add(i))
                        indegree[i]++;
                    // An import supplied through -lib is outside this file list.
                }
            }

            // Preserve project order among grammars with no dependency relation.
            var ready = new SortedSet<int>();
            for (var i = 0; i < indegree.Length; i++)
                if (indegree[i] == 0) ready.Add(i);

            var result = new List<string>(grammars.Count);
            while (ready.Count != 0)
            {
                var next = ready.Min;
                ready.Remove(next);
                result.Add(grammars[next].File);
                foreach (var successor in successors[next])
                    if (--indegree[successor] == 0) ready.Add(successor);
            }

            if (result.Count != grammars.Count)
                throw new InvalidOperationException("Cyclic grammar imports or tokenVocab dependencies: " +
                    string.Join(", ", grammars.Where((grammar, index) => indegree[index] != 0)
                        .Select(grammar => grammar.Name)));
            return result;
        }

        private static Grammar ReadGrammar(string file)
        {
            var source = MaskCommentsAndLiterals(File.ReadAllText(file));
            var declaration = Declaration.Match(source);
            if (!declaration.Success)
                throw new InvalidOperationException($"Cannot find a grammar declaration in '{file}'.");

            var grammar = new Grammar
            {
                File = file,
                Name = declaration.Groups["name"].Value,
                Kind = declaration.Groups["kind"].Value
            };

            foreach (Match match in Imports.Matches(source))
            {
                foreach (var entry in match.Groups["names"].Value.Split(','))
                {
                    // ANTLR permits import Alias=Grammar as well as import Grammar.
                    var parts = entry.Trim().Split('=');
                    var imported = parts[parts.Length - 1].Trim();
                    if (Identifier.IsMatch(imported)) grammar.Dependencies.Add(imported);
                }
            }

            foreach (Match options in Options.Matches(source))
                foreach (Match tokenVocab in TokenVocab.Matches(options.Groups["body"].Value))
                    grammar.Dependencies.Add(tokenVocab.Groups["name"].Value);

            // Match trgen's implicit FooParser -> FooLexer dependency.
            if (grammar.Kind == "parser" && grammar.Name.EndsWith("Parser", StringComparison.Ordinal))
                grammar.Dependencies.Add(grammar.Name.Substring(0, grammar.Name.Length - 6) + "Lexer");

            return grammar;
        }

        private static string MaskCommentsAndLiterals(string source)
        {
            var result = new StringBuilder(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    while (i < source.Length && source[i] != '\r' && source[i] != '\n')
                    {
                        result.Append(' ');
                        i++;
                    }
                    i--;
                }
                else if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    result.Append("  ");
                    i += 2;
                    while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                    {
                        result.Append(source[i] == '\r' || source[i] == '\n' ? source[i] : ' ');
                        i++;
                    }
                    if (i + 1 < source.Length) { result.Append("  "); i++; }
                }
                else if (c == '\'' || c == '"')
                {
                    var quote = c;
                    result.Append(' ');
                    while (++i < source.Length)
                    {
                        if (source[i] == '\\' && i + 1 < source.Length)
                        {
                            result.Append("  ");
                            i++;
                        }
                        else if (source[i] == quote) { result.Append(' '); break; }
                        else result.Append(source[i] == '\r' || source[i] == '\n' ? source[i] : ' ');
                    }
                }
                else result.Append(c);
            }
            return result.ToString();
        }
    }
}
