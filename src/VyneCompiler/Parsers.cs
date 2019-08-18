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
        public bool CachedValidAdd {
            get;
            private set;
        } = true;
        public bool CachedValid {
            get;
            private set;
        } = false;

        /// <summary>
        /// Tries parse a new character.
        /// </summary>
        /// <returns>Returns true when the character is valid.</returns>
        public bool TryAdd(char c) {
            CachedValidAdd = tryAdd(c);
            return CachedValidAdd;
        }
        /// <summary>
        /// Called at the end to make sure the parse is still valid.
        /// </summary>
        public bool IsValid() {
            CachedValid = isValid();
            return CachedValid;
        }

        /// <summary>
        /// The json output is useful for debugging.
        /// </summary>
        public abstract ExpandoObject ToJson();

        protected abstract bool isValid();
        protected abstract bool tryAdd(char c);
    }
    public class Alternative : Parser {
        public Alternative(string name, params Func<Parser>[] parsers) {
            _name = name;
            _parsers = new List<Parser>();
            foreach (Func<Parser> parserCreator in parsers) {
                _parsers.Add(parserCreator());
            }
        }

        protected override bool tryAdd(char c) {
            bool isValidNext = false;

            for (int i = _parsers.Count - 1; i >= 0; i--) {
                if (_parsers[i].TryAdd(c)) {
                    isValidNext = true;
                }
            }
            if (isValidNext) {
                for (int i = _parsers.Count - 1; i >= 0; i--) {
                    if (!_parsers[i].CachedValidAdd) {
                        _parsers.RemoveAt(i);
                    }
                }
            }
            return isValidNext;
        }
        protected override bool isValid() {
            bool isValid = false;

            for (int i = _parsers.Count - 1; i >= 0; i--) {
                if (_parsers[i].IsValid()) {
                    isValid = true;
                } else {
                    _parsers.RemoveAt(i);
                }
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            var json = new ExpandoObject() as IDictionary<string, object>;

            int count = 0;
            List<ExpandoObject> parsersJson = new List<ExpandoObject>();
            if (_parsers.Count > 0) {
                Parser parser = _parsers[0];
                foreach (Parser p in _parsers) {
                    if (p.CachedValid) {
                        parsersJson.Add(p.ToJson());
                        count++;
                        parser = p;
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
        private List<Parser> _parsers;
    }
    public class Sequential : Parser {
        public Sequential(params Func<Parser>[] parserCreators) {
            _parserCreators = parserCreators;
            _parsers = new List<Parser>();

            if (_parserCreators.Length > 0) {
                _parsers.Add(_parserCreators[0]());
            }
        }

        protected override bool tryAdd(char c) {
            if (_parsers.Count > 0) {
                if (_parsers.Last().TryAdd(c)) {
                    return true;
                } else if (_parsers.Count < _parserCreators.Length) {
                    Parser parser = _parserCreators[_parsers.Count]();
                    parser.TryAdd(c);
                    _parsers.Add(parser);
                    return parser.CachedValidAdd;
                }
            }
            return false;
        }
        protected override bool isValid() {
            bool isValid = _parsers.Count == _parserCreators.Length;

            for (int i = 0; isValid && i < _parsers.Count; i++) {
                isValid = _parsers[i].IsValid() && isValid;
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            dynamic sequence = new ExpandoObject();

            List<ExpandoObject> parsers = new List<ExpandoObject>();
            for (int i = 0; i < _parsers.Count; i++) {
                parsers.Add(_parsers[i].ToJson());
            }
            sequence.Sequence = parsers;

            return sequence;
        }

        private Func<Parser>[] _parserCreators;
        private List<Parser> _parsers;
    }
    public class Repeat : Parser {
        public Repeat(string name, Func<Parser> createParser) {
            _name = name;
            _createParser = createParser;
            _parsers = new List<Parser>();
            _parsers.Add(_createParser());
        }

        protected override bool tryAdd(char c) {
            _parsers.Last().TryAdd(c);

            if (!_parsers.Last().CachedValidAdd) {
                Parser parser = _createParser();
                parser.TryAdd(c);
                if (parser.CachedValidAdd) {
                    _parsers.Add(parser);
                }
                return parser.CachedValidAdd;
            }

            return true;
        }
        protected override bool isValid() {
            if (_parsers.Count == 0) {
                return false;
            }

            bool isValid = true;

            for (int i = 0; i < _parsers.Count; i++) {
                isValid = _parsers[i].IsValid() && isValid;
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            var repeat = new ExpandoObject() as IDictionary<string, object>;

            List<ExpandoObject> parsers = new List<ExpandoObject>();
            for (int i = 0; i < _parsers.Count; i++) {
                parsers.Add(_parsers[i].ToJson());
            }
            repeat.Add(_name, parsers);

            return (ExpandoObject)repeat;
        }

        private string _name;
        private Func<Parser> _createParser;
        private List<Parser> _parsers;
    }
    public class Single : Parser {
        protected override bool tryAdd(char c) {
            if (_text.Length < 1) {
                _text += c;
                return true;
            }
            return false;
        }
        protected override bool isValid() {
            return _text.Length == 1;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = _text;
            dynamic single = new ExpandoObject();
            single.Single = text;
            return single;
        }

        private string _text = "";
    }
    public class Whitespace : Parser {
        protected override bool tryAdd(char c) {
            if (char.IsWhiteSpace(c)) {
                _text += c;
                return true;
            }
            return false;
        }
        protected override bool isValid() {
            return _text.Length > 0;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = _text;
            dynamic whitespace = new ExpandoObject();
            whitespace.Whitespace = text;
            return whitespace;
        }

        private string _text = "";
    }
    public class Clear : Parser {
        public Clear(Parser parser) {
            _parser = parser;
        }

        protected override bool tryAdd(char c) {
            if (_discard1 != null) {
                if (!_discard1.TryAdd(c)) {
                    _discard1 = null;
                } else {
                    return true;
                }
            }
            if (_parser.CachedValidAdd && _parser.TryAdd(c)) {
                return true;
            }
            if (_discard2 != null) {
                if (!_discard2.TryAdd(c)) {
                    _discard2 = null;
                }
            }
            return _discard2 != null;
        }
        protected override bool isValid() {
            return _parser.IsValid();
        }
        public override ExpandoObject ToJson() {
            return _parser.ToJson();
        }

        private Whitespace _discard1 = new Whitespace();
        private Whitespace _discard2 = new Whitespace();
        private Parser _parser;
    }
    public class Token : Parser {
        public Token(string name, string sequence) {
            _name = name;
            _sequence = sequence.ToArray();
        }

        protected override bool tryAdd(char c) {
            if (_text.Length < _sequence.Length && c == _sequence[_text.Length]) {
                _text += c;
                return true;
            }
            return false;
        }
        protected override bool isValid() {
            return _text.Length == _sequence.Length;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = _text;
            var op = new ExpandoObject() as IDictionary<string, object>;
            op.Add(_name, text);
            return (dynamic)op;
        }

        private string _name;
        private char[] _sequence;
        private string _text = "";
    }
    public class Identifier : Parser {
        protected override bool tryAdd(char c) {
            if (validateNext(c)) {
                _text += c;
                return true;
            }
            return false;
        }
        private bool validateNext(char c) {
            if (c == '_') {
                return true;
            } else if (_text.Length == 0) {
                return char.IsLetter(c);
            } else {
                return char.IsLetterOrDigit(c);
            }
        }
        protected override bool isValid() {
            bool foundUnderscore = false;
            for (int i = 0; i < _text.Length; i++) {
                if (_text[i] == '_') {
                    foundUnderscore = true;
                } else if (!foundUnderscore && char.IsDigit(_text[i])) {
                    break;
                } else if (char.IsLetterOrDigit(_text[i])) {
                    return true;
                }
            }
            return false;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = _text;
            dynamic identifier = new ExpandoObject();
            identifier.Identifier = text;
            return identifier;
        }

        private string _text = "";
    }
    public class Integer : Parser {
        protected override bool tryAdd(char c) {
            if (char.IsDigit(c)) {
                _text += c;
                return true;
            }
            return false;
        }
        protected override bool isValid() {
            // Perhaps it will make sense to check if the value is within a Signed 32-bit integer.
            // That is, until we get a real type system.
            return _text.Length > 0;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = _text;
            dynamic integer = new ExpandoObject();
            integer.Integer = text;
            return integer;
        }

        private string _text = "";
    }
    public class LineComment : Parser {
        protected override bool tryAdd(char c) {
            if (validateNext(c)) {
                _text += c;
                return true;
            }
            return false;
        }
        protected bool validateNext(char c) {
            if (_text.Length <= 1) {
                return c == '/';
            }
            return _text.Last() != '\n';
        }
        protected override bool isValid() {
            return Enumerable.SequenceEqual(_text.Take(2), _opening);
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = _text;
            dynamic lineComment = new ExpandoObject();
            lineComment.LineComment = text;
            return lineComment;
        }

        private char[] _opening = new char[] { '/', '/' };
        private string _text = "";
    }
    public class MultilineComment : Parser {
        protected override bool tryAdd(char c) {
            if (validateNext(c)) {
                _text += c;

                int char2 = _text.Length - 2;
                int char3 = _text.Length - 3;
                IEnumerable<char> last2 = _text.TakeLast(2);
                IEnumerable<char> last3 = _text.TakeLast(3);
                if (_lastCharClosed < char2 && Enumerable.SequenceEqual(last2, _opening)) {
                    _nestingLevel++;
                    _lastCharOpen = _text.Length - 1;
                } else if (_lastCharOpen < char3 && Enumerable.SequenceEqual(last3, _breakOut)) {
                    _nestingLevel = 0;
                    _lastCharClosed = _text.Length - 1;
                } else if (_lastCharOpen < char2 && Enumerable.SequenceEqual(last2, _closing)) {
                    _nestingLevel--;
                    _lastCharClosed = _text.Length - 1;
                }
                return true;
            }
            return false;
        }
        private bool validateNext(char c) {
            if (_text.Length == 0) {
                return c == '/';
            } else if (_text.Length == 1) {
                return c == '*';
            } else if (_text.Length >= 4) {
                if (Enumerable.SequenceEqual(_text.TakeLast(2).Append(c), _breakOut)) {
                    return true;
                } else if (_nestingLevel == 0) {
                    return false;
                }
            }
            return true;
        }
        protected override bool isValid() {
            return _text.Length >= 4 && _nestingLevel <= 0;
        }
        public override ExpandoObject ToJson() {
            dynamic text = new ExpandoObject();
            text.Text = _text;
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
        private string _text = "";
    }
    public class Factor : Alternative {
        public Factor() : base("Factor",
            () => new Clear(new Integer()),
            () => new Clear(new Identifier())
        ) { }
    }
    public class Term : Alternative {
        public Term() : base("Term",
            () => new Factor(),
            () => new Sequential(
                () => new Repeat("FactorOperator", () =>
                    new Sequential(
                        () => new Factor(),
                        () => new Alternative("Operator",
                            () => new Token("Multiply", "*"),
                            () => new Token("Divide", "/"),
                            () => new Token("Modulo", "%")
                        )
                    )
                ),
                () => new Factor()
            )
        ) { }
    }
    public class Expression : Alternative {
        public Expression() : base("Expression",
            () => new Term(),
            () => new Sequential(
                () => new Repeat("TermOperator",
                    () => new Sequential(
                        () => new Term(),
                        () => new Alternative("Operator",
                            () => new Token("Addition", "+"),
                            () => new Token("Subtract", "-")
                        )
                    )
                ),
                () => new Term()
            )
        ) { }
    }
}