using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace VyneCompiler.Parsers {
    /// <summary>
    /// Base class for parsers.
    /// It's up to the parser's parent to clean up white space and comments after it.
    /// </summary>
    public abstract class Parser {
        public bool CachedValidNext {
            get;
            private set;
        } = true;
        public bool CachedValid {
            get;
            private set;
        } = false;

        /// <summary>
        /// This must always be called before Add is called.
        /// </summary>
        /// <returns>Returns true when Add can be called.</returns>
        public bool ValidateNext(char c) {
            CachedValidNext = validateNext(c);
            return CachedValidNext;
        }
        /// <summary>
        /// Called at the end to make sure the parse is still valid.
        /// </summary>
        public bool IsValid() {
            CachedValid = isValid();
            return CachedValid;
        }

        public virtual void Add(char c) {
            Text += c;
        }
        /// <summary>
        /// The json output is useful for debugging.
        /// </summary>
        public abstract ExpandoObject ToJson();

        protected abstract bool validateNext(char c);
        protected abstract bool isValid();

        protected string Text = "";
    }
    public class Alternative : Parser {
        public Alternative(string name, params Lazy<Parser>[] parsers) {
            _name = name;
            Parsers = parsers.ToList();
        }

        public List<Lazy<Parser>> Parsers;

        public override void Add(char c) {
            for (int i = Parsers.Count - 1; i >= 0; i--) {
                if (Parsers[i].Value.CachedValidNext) {
                    Parsers[i].Value.Add(c);
                } else {
                    Parsers.RemoveAt(i);
                }
            }
        }
        protected override bool validateNext(char c) {
            bool isValidNext = false;

            foreach (Lazy<Parser> p in Parsers) {
                if (p.Value.CachedValidNext) {
                    p.Value.ValidateNext(c);
                    isValidNext = isValidNext || p.Value.CachedValidNext;
                }
            }
            return isValidNext;
        }
        protected override bool isValid() {
            bool isValid = false;

            for (int i = 0; i < Parsers.Count; i++) {
                if (Parsers[i].Value.IsValid()) {
                    isValid = true;
                }
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            var json = new ExpandoObject() as IDictionary<string, object>;

            int count = 0;
            List<ExpandoObject> parsersJson = new List<ExpandoObject>();
            if (Parsers.Count > 0) {
                Parser parser = Parsers[0].Value;
                foreach (Lazy<Parser> p in Parsers) {
                    if (p.Value.CachedValid) {
                        parsersJson.Add(p.Value.ToJson());
                        count++;
                        parser = p.Value;
                    }
                }
                if (count == 1) {
                    json.Add(_name, parser.ToJson());
                }
            }
            if (count != 1) {
                json.Add(_name, parsersJson);
            }

            return (ExpandoObject)json;
        }

        private string _name;
    }
    public class Sequential : Parser {
        public Sequential(params Lazy<Parser>[] parsers) {
            Parsers = parsers;
        }

        public Lazy<Parser>[] Parsers;

        public override void Add(char c) {
            if (Parsers.Length > 0) {
                if (Parsers[0].Value.CachedValidNext) {
                    Parsers[0].Value.Add(c);
                }
                for (int i = 1; i < Parsers.Length; i++) {
                    if (!Parsers[i - 1].Value.CachedValidNext && Parsers[i].Value.CachedValidNext) {
                        Parsers[i].Value.Add(c);
                    }
                }
            }
        }
        protected override bool validateNext(char c) {
            if (Parsers.Length > 0) {
                if (Parsers[0].Value.CachedValidNext) {
                    Parsers[0].Value.ValidateNext(c);
                }
                for (int i = 1; i < Parsers.Length; i++) {
                    if (!Parsers[i - 1].Value.CachedValidNext && Parsers[i].Value.CachedValidNext) {
                        Parsers[i].Value.ValidateNext(c);
                    }
                }
                return Parsers.Last().Value.CachedValidNext;
            }
            return false;
        }
        protected override bool isValid() {
            bool isValid = true;

            for (int i = 0; i < Parsers.Length; i++) {
                isValid = Parsers[i].Value.IsValid() && isValid;
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            dynamic sequence = new ExpandoObject();

            List<ExpandoObject> parsers = new List<ExpandoObject>();
            for (int i = 0; i < Parsers.Length; i++) {
                parsers.Add(Parsers[i].Value.ToJson());
            }
            sequence.Sequence = parsers;

            return sequence;
        }
    }
    public class Repeat : Parser {
        public Repeat(string name, Func<Parser> createParser) {
            _name = name;
            CreateParser = createParser;
            Parser = CreateParser();
            CompletedParsers = new List<Parser>();
        }

        public Parser Parser;
        public List<Parser> CompletedParsers;

        public override void Add(char c) {
            Parser.Add(c);
        }
        protected override bool validateNext(char c) {
            Parser.ValidateNext(c);

            if (!Parser.CachedValidNext) {
                CompletedParsers.Add(Parser);

                Parser = CreateParser();
                return Parser.ValidateNext(c);
            }

            return true;
        }
        protected override bool isValid() {
            bool isValid = true;

            for (int i = 0; i < CompletedParsers.Count; i++) {
                isValid = CompletedParsers[i].IsValid() && isValid;
            }
            if (Parser.IsValid()) {
                CompletedParsers.Add(Parser);
            } else if (CompletedParsers.Count == 0) {
                return false;
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            var repeat = new ExpandoObject() as IDictionary<string, object>;

            List<ExpandoObject> parsers = new List<ExpandoObject>();
            for (int i = 0; i < CompletedParsers.Count; i++) {
                parsers.Add(CompletedParsers[i].ToJson());
            }
            repeat.Add(_name, parsers);

            return (ExpandoObject)repeat;
        }

        private string _name;
        private Func<Parser> CreateParser;
    }
    public class Single : Parser {
        protected override bool validateNext(char c) {
            return Text.Length < 1;
        }
        protected override bool isValid() {
            return Text.Length == 1;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = Text;
            dynamic single = new ExpandoObject();
            single.Single = text;
            return single;
        }
    }
    public class Whitespace : Parser {
        protected override bool validateNext(char c) {
            return char.IsWhiteSpace(c);
        }
        protected override bool isValid() {
            return Text.Length > 0;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = Text;
            dynamic whitespace = new ExpandoObject();
            whitespace.Whitespace = text;
            return whitespace;
        }
    }
    public class Clear : Parser {
        public Clear(Parser parser) {
            Parser = parser;
        }

        Parser Parser;

        public override void Add(char c) {
            if (_discard1 != null) {
                _discard1.Add(c);
            } else if (Parser.CachedValidNext) {
                Parser.Add(c);
            } else if (_discard2 != null) {
                _discard2.Add(c);
            }
        }
        protected override bool validateNext(char c) {
            if (_discard1 != null) {
                if (!_discard1.ValidateNext(c)) {
                    _discard1 = null;
                }
            }
            if (_discard1 != null) {
                return true;
            } else if (Parser.CachedValidNext && Parser.ValidateNext(c)) {
                return true;
            } else if (_discard2 != null) {
                if (!_discard2.ValidateNext(c)) {
                    _discard2 = null;
                }
            }
            return _discard2 != null;
        }
        protected override bool isValid() {
            return Parser.IsValid();
        }
        public override ExpandoObject ToJson() {
            return Parser.ToJson();
        }

        private Whitespace _discard1 = new Whitespace();
        private Whitespace _discard2 = new Whitespace();
    }
    public class Token : Parser {
        public Token(string name, string sequence) {
            _name = name;
            _sequence = sequence.ToArray();
        }

        protected override bool validateNext(char c) {
            return Text.Length < _sequence.Length && c == _sequence[Text.Length];
        }
        protected override bool isValid() {
            return Text.Length == _sequence.Length;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = Text;
            var op = new ExpandoObject() as IDictionary<string, object>;
            op.Add(_name, text);
            return (dynamic)op;
        }

        private string _name;
        private char[] _sequence;
    }
    public class Identifier : Parser {
        protected override bool validateNext(char c) {
            if (c == '_') {
                return true;
            } else if (Text.Length == 0) {
                return char.IsLetter(c);
            } else {
                return char.IsLetterOrDigit(c);
            }
        }
        protected override bool isValid() {
            bool foundUnderscore = false;
            for (int i = 0; i < Text.Length; i++) {
                if (Text[i] == '_') {
                    foundUnderscore = true;
                } else if (!foundUnderscore && char.IsDigit(Text[i])) {
                    break;
                } else if (char.IsLetterOrDigit(Text[i])) {
                    return true;
                }
            }
            return false;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = Text;
            dynamic identifier = new ExpandoObject();
            identifier.Identifier = text;
            return identifier;
        }
    }
    public class Integer : Parser {
        protected override bool validateNext(char c) {
            return char.IsDigit(c);
        }
        protected override bool isValid() {
            // Perhaps it will make sense to check if the value is within a Signed 32-bit integer.
            // That is, until we get a real type system.
            return Text.Length > 0;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = Text;
            dynamic integer = new ExpandoObject();
            integer.Integer = text;
            return integer;
        }
    }
    public class LineComment : Parser {
        protected override bool validateNext(char c) {
            if (Text.Length <= 1) {
                return c == '/';
            }
            return Text.Last() != '\n';
        }
        protected override bool isValid() {
            return Enumerable.SequenceEqual(Text.Take(2), _opening);
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = Text;
            dynamic lineComment = new ExpandoObject();
            lineComment.LineComment = text;
            return lineComment;
        }

        private char[] _opening = new char[] { '/', '/' };
    }
    public class MultilineComment : Parser {
        protected override bool validateNext(char c) {
            if (Text.Length == 0) {
                return c == '/';
            } else if (Text.Length == 1) {
                return c == '*';
            } else if (Text.Length >= 4) {
                if (Enumerable.SequenceEqual(Text.TakeLast(2).Append(c), _breakOut)) {
                    return true;
                } else if (_nestingLevel == 0) {
                    return false;
                }
            }
            return true;
        }
        protected override bool isValid() {
            return Text.Length >= 4 && _nestingLevel <= 0;
        }
        public override void Add(char c) {
            Text += c;

            int char2 = Text.Length - 2;
            int char3 = Text.Length - 3;
            IEnumerable<char> last2 = Text.TakeLast(2);
            IEnumerable<char> last3 = Text.TakeLast(3);
            if (_lastCharClosed < char2 && Enumerable.SequenceEqual(last2, _opening)) {
                _nestingLevel++;
                _lastCharOpen = Text.Length - 1;
            } else if (_lastCharOpen < char3 && Enumerable.SequenceEqual(last3, _breakOut)) {
                _nestingLevel = 0;
                _lastCharClosed = Text.Length - 1;
            } else if (_lastCharOpen < char2 && Enumerable.SequenceEqual(last2, _closing)) {
                _nestingLevel--;
                _lastCharClosed = Text.Length - 1;
            }
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = Text;
            dynamic multilineComment = new ExpandoObject();
            multilineComment.MultilineComment = text;
            return multilineComment;
        }

        private int _nestingLevel = 0;
        private int _lastCharOpen = -1;
        private int _lastCharClosed = -1;
        private char[] _opening = new char[] { '/', '*' };
        private char[] _closing = new char[] { '*', '/' };
        private char[] _breakOut = new char[] { '*', '/', '/' };
    }
    public class Factor : Alternative {
        public Factor() : base("Factor",
            new Lazy<Parser>(() => new Clear(new Integer())),
            new Lazy<Parser>(() => new Clear(new Identifier()))
        ) { }
    }
    public class Term : Alternative {
        public Term() : base("Term",
            new Lazy<Parser>(() => new Factor()),
            new Lazy<Parser>(() => new Sequential(
                new Lazy<Parser>(() => new Repeat("FactorOperator", () =>
                    new Sequential(
                        new Lazy<Parser>(() => new Factor()),
                        new Lazy<Parser>(() => new Alternative("Operator",
                            new Lazy<Parser>(() => new Token("Multiply", "*")),
                            new Lazy<Parser>(() => new Token("Divide", "/")),
                            new Lazy<Parser>(() => new Token("Modulo", "%"))
                        )
                    ))
                )),
                new Lazy<Parser>(() => new Factor())
            ))
        ) { }
    }
    public class Expression : Alternative {
        public Expression() : base("Expression",
            new Lazy<Parser>(() => new Term()),
            new Lazy<Parser>(() => new Sequential(
                new Lazy<Parser>(() => new Repeat("TermOperator", () =>
                    new Sequential(
                        new Lazy<Parser>(() => new Term()),
                        new Lazy<Parser>(() => new Alternative("Operator",
                            new Lazy<Parser>(() => new Token("Addition", "+")),
                            new Lazy<Parser>(() => new Token("Subtract", "-"))
                        )
                    ))
                )),
                new Lazy<Parser>(() => new Term())
            ))
        ) { }
    }
}