using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using NUnit.Framework;

namespace VyneCompiler.Parsers {
    /// <summary>
    /// Base class for parsers.
    /// It's up to the parser's parent to clean up white space and comments after it.
    /// </summary>
    public abstract class Parser {
        public string Text {
            get;
            set;
        } = "";

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
    public class Token : Parser {
        public Token(string name, string sequence) {
            _name = name;
            _sequence = sequence.ToArray();
        }

        public override void Add(char c) {
            if (_discard != null) {
                _discard.Add(c);
            } else {
                Text += c;
            }
        }
        protected override bool validateNext(char c) {
            if (_discard != null) {
                if (!_discard.ValidateNext(c)) {
                    _discard = null;
                }
            }
            if (_discard != null) {
                return true;
            }

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
        private Whitespace _discard = new Whitespace();
    }
    public class Alternative : Parser {
        public Alternative(params KeyValuePair<string, Parser>[] parsers) {
            Parsers = new Dictionary<string, Parser>();
            foreach (KeyValuePair<string, Parser> kp in parsers) {
                Parsers.Add(kp.Key, kp.Value);
            }
        }

        public Dictionary<string, Parser> Parsers;

        public override void Add(char c) {
            foreach (KeyValuePair<string, Parser> kp in Parsers) {
                if (kp.Value.CachedValidNext) {
                    kp.Value.Add(c);
                }
            }
        }
        protected override bool validateNext(char c) {
            bool isValidNext = false;

            foreach (KeyValuePair<string, Parser> kp in Parsers) {
                if (kp.Value.CachedValidNext) {
                    kp.Value.ValidateNext(c);
                    isValidNext = isValidNext || kp.Value.CachedValidNext;
                }
            }
            return isValidNext;
        }
        protected override bool isValid() {
            bool isValid = false;

            List<string> keys = Parsers.Keys.ToList();
            foreach (string key in keys) {
                if (!Parsers[key].IsValid()) {
                    Parsers.Remove(key);
                } else {
                    isValid = true;
                }
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            var json = new ExpandoObject() as IDictionary<string, Object>;

            foreach (KeyValuePair<string, Parser> kp in Parsers) {
                json.Add(kp.Key, kp.Value.ToJson());
            }

            return (ExpandoObject)json;
        }
    }
    public class Sequential : Parser {
        public Sequential(params Parser[] parsers) {
            Parsers = parsers;
        }

        public Parser[] Parsers;

        public override void Add(char c) {
            if (Parsers.Length > 0) {
                if (Parsers[0].CachedValidNext) {
                    Parsers[0].Add(c);
                }
                for (int i = 1; i < Parsers.Length; i++) {
                    if (!Parsers[i - 1].CachedValidNext && Parsers[i].CachedValidNext) {
                        Parsers[i].Add(c);
                    }
                }
            }
        }
        protected override bool validateNext(char c) {
            if (Parsers.Length > 0) {
                if (Parsers[0].CachedValidNext) {
                    Parsers[0].ValidateNext(c);
                }
                for (int i = 1; i < Parsers.Length; i++) {
                    if (!Parsers[i - 1].CachedValidNext && Parsers[i].CachedValidNext) {
                        Parsers[i].ValidateNext(c);
                    }
                }
                return Parsers.Last().CachedValidNext;
            }
            return false;
        }
        protected override bool isValid() {
            bool isValid = true;

            for (int i = 0; i < Parsers.Length; i++) {
                isValid = isValid && Parsers[i].IsValid();
            }

            return isValid;
        }
        public override ExpandoObject ToJson() {
            dynamic sequence = new ExpandoObject();

            List<ExpandoObject> parsers = new List<ExpandoObject>();
            for (int i = 0; i < Parsers.Length; i++) {
                parsers.Add(Parsers[i].ToJson());
            }
            sequence.Sequence = parsers;

            return sequence;
        }
    }
    public class Identifier : Parser {
        public override void Add(char c) {
            if (_discard != null) {
                _discard.Add(c);
            } else {
                Text += c;
            }
        }
        protected override bool validateNext(char c) {
            if (_discard != null) {
                if (!_discard.ValidateNext(c)) {
                    _discard = null;
                }
            }
            if (_discard != null) {
                return true;
            }

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

        private Whitespace _discard = new Whitespace();
    }
    public class Integer : Parser {
        public override void Add(char c) {
            if (_discard != null) {
                _discard.Add(c);
            } else {
                Text += c;
            }
        }
        protected override bool validateNext(char c) {
            if (_discard != null) {
                if (!_discard.ValidateNext(c)) {
                    _discard = null;
                }
            }
            if (_discard != null) {
                return true;
            }

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

        private Whitespace _discard = new Whitespace();
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

    // Tests

    [TestFixture]
    public class ParserTests {
        [TestCase("hello")]
        [TestCase("hello123")]
        [TestCase("_hello")]
        [TestCase("_hello_world")]
        [TestCase("_123")]
        [TestCase("    hello")]
        public void Valid_Identifier(string content) {
            Assert.IsTrue(Test_Identifier(content));
        }

        [TestCase("")]
        [TestCase("123")]
        [TestCase("hello world")]
        [TestCase("/*+-")]
        public void Invalid_Identifier(string content) {
            Assert.IsFalse(Test_Identifier(content));
        }

        [TestCase("1")]
        [TestCase("12")]
        [TestCase("    12")]
        public void Valid_Integer(string content) {
            Assert.IsTrue(Test_Integer(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        public void Invalid_Integer(string content) {
            Assert.IsFalse(Test_Integer(content));
        }

        [TestCase("// Hello")]
        [TestCase("// Hello\n")]
        [TestCase("// Hello\r\n")]
        public void Valid_LineComment(string content) {
            Assert.IsTrue(Test_LineComment(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        public void Invalid_LineComment(string content) {
            Assert.IsFalse(Test_LineComment(content));
        }

        [TestCase("/**/")]
        [TestCase("/**//")]
        [TestCase("/* Hello */")]
        [TestCase("/* Hello\n   World */")]
        [TestCase("/* Hello /* World */*/")]
        [TestCase("/* Hello /* World *//")]
        [TestCase("/* /* /* /* /* /* /* *//")]
        [TestCase("/*/*/*/**/*/*/*/")]
        [TestCase("/*/*/*/**//")]
        public void Valid_MultilineComment(string content) {
            Assert.IsTrue(Test_MultilineComment(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        [TestCase("*/")]
        [TestCase("*//")]
        [TestCase("/**///")]
        [TestCase("/*/")]
        [TestCase("/*//")]
        [TestCase("/*/*//")]
        public void Invalid_MultilineComment(string content) {
            Assert.IsFalse(Test_MultilineComment(content));
        }

        [TestCase("    ")]
        [TestCase("\t")]
        [TestCase("\n")]
        [TestCase("\r\n")]
        public void Valid_Whitespace(string content) {
            Assert.IsTrue(Test_Whitespace(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        public void Invalid_Whitespace(string content) {
            Assert.IsFalse(Test_Whitespace(content));
        }

        [TestCase("/*Hello*/123")]
        public void Valid_Sequential(string content) {
            Assert.IsTrue(Test_Sequential(content));
        }

        [TestCase("123")]
        [TestCase("hello")]
        public void Valid_Parallel(string content) {
            Assert.IsTrue(Test_Parallel(content));
        }

        private bool Test_Identifier(string content) {
            Identifier p = new Identifier();
            return Test_Parser(p, content);
        }
        private bool Test_Integer(string content) {
            Integer p = new Integer();
            return Test_Parser(p, content);
        }
        private bool Test_LineComment(string content) {
            LineComment p = new LineComment();
            return Test_Parser(p, content);
        }
        private bool Test_MultilineComment(string content) {
            MultilineComment p = new MultilineComment();
            return Test_Parser(p, content);
        }
        private bool Test_Whitespace(string content) {
            Whitespace p = new Whitespace();
            return Test_Parser(p, content);
        }
        private bool Test_Sequential(string content) {
            Sequential p = new Sequential(new MultilineComment(), new Integer());
            return Test_Parser(p, content);
        }
        private bool Test_Parallel(string content) {
            Alternative p = new Alternative(new KeyValuePair<string, Parser>("Identifer", new Identifier()), new KeyValuePair<string, Parser>("Integer", new Integer()));
            return Test_Parser(p, content);
        }
        private bool Test_Parser(Parser p, string content) {
            foreach (char c in content) {
                if (!p.ValidateNext(c)) {
                    return false;
                }
                p.Add(c);
            }
            return p.IsValid();
        }
    }
}