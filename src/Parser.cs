using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VyneCompiler.A;

namespace VyneCompiler.P {
    public class Parser<T> {
        public Parser(Func<Source, ParseResult<T>?> parse) {
            Parse = parse;
        }

        public Func<Source, ParseResult<T>?> Parse { get; set; }

        public Parser<T> Or(Parser<T> parser) {
            return new Parser<T>(source => {
                ParseResult<T>? result = Parse(source);
                if (result != null) {
                    return result;
                } else {
                    return parser.Parse(source);
                }
            });
        }
        public Parser<U> Bind<U>(Func<T, Parser<U>> callback) {
            return new Parser<U>(source => {
                var result = Parse(source);
                if (result != null) {
                    return callback(result.Value).Parse(result.Source);
                } else {
                    return null;
                }
            });
        }
        public Parser<U> And<U>(Parser<U> parser) {
            return Bind((_) => parser);
        }
        public Parser<U> Map<U>(Func<T, U> callback) {
            return Bind(value => Constant(callback(value)));
        }
        public T ParserStringToCompletion(string str) {
            Source source = new Source(str, 0);

            var result = Parse(source);
            if (result == null) throw new ArgumentException("Parse error at index 0.");

            int index = result.Source.Index;
            if (index != result.Source.Str.Length) throw new ArgumentException($"Parse error at index {index}.");

            return result.Value;
        }

        public static Parser<string> Regexp(string regexp, RegexOptions options = RegexOptions.None) {
            return new Parser<string>(source => source.Match(new Regex(@"\G" + regexp, options)));
        }
        public static Parser<U> Constant<U>(U value) {
            return new Parser<U>(source => new ParseResult<U>(value, source));
        }
        public static Parser<U> Error<U>(string message) {
            return new Parser<U>(source => {
                throw new ArgumentException(message);
            });
        }
        public static Parser<List<U>> ZeroOrMore<U>(Parser<U> parser) {
            return new Parser<List<U>>(source => {
                List<U> results = new List<U>();
                ParseResult<U>? item;
                while ((item = parser.Parse(source)) != null) {
                    source = item.Source;
                    results.Add(item.Value);
                }
                return new ParseResult<List<U>>(results, source);
            });
        }
        public static Parser<U> Maybe<U>(Parser<U> parser) {
            return parser.Or(Constant<U>(default(U)));
        }
    }

    public class Source {
        public Source(string str, int index) {
            Str = str;
            Index = index;
        }

        public string Str { get; set; }
        public int Index { get; set; }

        public ParseResult<string>? Match(Regex regexp) {
            Match match = regexp.Match(Str, Index);
            if (match.Success) {
                string value = match.Value;
                int newIndex = Index + value.Length;
                Source source = new Source(Str, newIndex);
                return new ParseResult<string>(value, source);
            }

            return null;
        }
    }

    public class ParseResult<T> {
        public ParseResult(T value, Source source) {
            Value = value;
            Source = source;
        }

        public T Value { get; set; }
        public Source Source { get; set; }
    }

    public static class Lexer {
        public static Parser<string> Whitespace = Parser<string>.Regexp(@"[ \n\r\t]+");
        public static Parser<string> Comments = Parser<string>.Regexp(@"[/][/].*").Or(Parser<string>.Regexp(@"[/][*].*[*][/]", RegexOptions.Singleline));
        public static Parser<List<string>> Ignored = Parser<string>.ZeroOrMore(Whitespace.Or(Comments));

        public static Func<string, Parser<string>> token = pattern => Parser<string>.Regexp(pattern).Bind(value => Ignored.And(Parser<string>.Constant(value)));

        public static Parser<string> FUNCTION = token(@"function\b");
        public static Parser<string> IF = token(@"if\b");
        public static Parser<string> ELSE = token(@"else\b");
        public static Parser<string> RETURN = token(@"return\b");
        public static Parser<string> LET = token(@"let\b");
        public static Parser<string> WHILE = token(@"while\b");

        public static Parser<string> COMMA = token(@"[,]");
        public static Parser<string> SEMICOLON = token(@";");
        public static Parser<string> LEFT_PAREN = token(@"[(]");
        public static Parser<string> RIGHT_PAREN = token(@"[)]");
        public static Parser<string> LEFT_BRACE = token(@"[{]");
        public static Parser<string> RIGHT_BRACE = token(@"[}]");

        public static Parser<Integer> INTEGER = token(@"[0-9]+").Map(digits => new Integer(Int32.Parse(digits)));
        public static Parser<string> ID = token(@"[a-zA-Z_]");
        public static Parser<Id> id = ID.Map(x => new Id(x));

        public static Parser<Func<AST, Not>> NOT = token(@"!").Map<Func<AST, Not>>(_ => term => new Not(term));
        public static Parser<Func<AST, AST, Equal>> EQUAL = token(@"==").Map<Func<AST, AST, Equal>>(_ => (left, right) => new Equal(left, right));
        public static Parser<Func<AST, AST, NotEqual>> NOT_EQUAL = token(@"!=").Map<Func<AST, AST, NotEqual>>(_ => (left, right) => new NotEqual(left, right));
        public static Parser<Func<AST, AST, Add>> PLUS = token(@"[+]").Map<Func<AST, AST, Add>>(_ => (left, right) => new Add(left, right));
        public static Parser<Func<AST, AST, Subtract>> MINUS = token(@"[-]").Map<Func<AST, AST, Subtract>>(_ => (left, right) => new Subtract(left, right));
        public static Parser<Func<AST, AST, Multiply>> STAR = token(@"[*]").Map<Func<AST, AST, Multiply>>(_ => (left, right) => new Multiply(left, right));
        public static Parser<Func<AST, AST, Divide>> SLASH = token(@"[/]").Map<Func<AST, AST, Divide>>(_ => (left, right) => new Divide(left, right));

        public static Parser<AST> expression = Parser<string>.Error<AST>("expression parser used before definition");

        public static Parser<List<AST>> args =
            expression.Bind(arg =>
                Parser<string>.ZeroOrMore(COMMA.And(expression)).Bind(args =>
                    Parser<string>.Constant<List<AST>>(PrependList(arg, args)))).Or(Parser<string>.Constant(new List<AST>()));

        private static List<T> PrependList<T>(T x, List<T> nx) {
            nx.Insert(0, x);
            return nx;
        }
    }
}
