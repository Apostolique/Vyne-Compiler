using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

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

        public static Parser<string> Regexp(string regexp) {
            return new Parser<string>(source => source.Match(new Regex(regexp)));
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
}
