using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace VyneCompiler.Parsers {
    public abstract class Parser {
        public string Text {
            get;
            set;
        } = "";

        public abstract bool ValidateNext(char c);
        public abstract bool IsValid();

        public virtual void Add(char c) {
            Text += c;
        }
    }
    public class Identifier : Parser {
        public override bool ValidateNext(char c) {
            if (c == '_') {
                return true;
            } else if (Text.Length == 0) {
                return char.IsLetter(c);
            } else {
                return char.IsLetterOrDigit(c);
            }
        }
        public override bool IsValid() {
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
    }
    public class Integer : Parser {
        public override bool ValidateNext(char c) {
            return char.IsDigit(c);
        }
        public override bool IsValid() {
            // Perhaps it will make sense to check if the value is within a Signed 32-bit integer.
            // That is, until we get a real type system.
            return Text.Length > 0;
        }
    }
    public class LineComment : Parser {
        public override bool ValidateNext(char c) {
            if (Text.Length <= 1) {
                return c == '/';
            }
            return Text.Last() != '\n';
        }
        public override bool IsValid() {
            return Enumerable.SequenceEqual(Text.Take(2), _opening);
        }

        private char[] _opening = new char[] {'/', '/'};
    }
    public class MultilineComment : Parser {
        public override bool ValidateNext(char c) {
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
        public override bool IsValid() {
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

        private int _nestingLevel = 0;
        private int _lastCharOpen = -1;
        private int _lastCharClosed = -1;
        private char[] _opening = new char[] {'/', '*'};
        private char[] _closing = new char[] {'*', '/'};
        private char[] _breakOut = new char[] {'*', '/', '/'};
    }

    // Tests

    [TestFixture]
    public class ParserTests {
        [TestCase("hello")]
        [TestCase("hello123")]
        [TestCase("_hello")]
        [TestCase("_hello_world")]
        [TestCase("_123")]
        public void Valid_Identifier(string content) {
            Assert.IsTrue(Test_Identifier(content));
        }
        [TestCase("")]
        [TestCase("123")]
        [TestCase("hello world")]
        [TestCase(" hello")]
        [TestCase("/*+-")]
        public void Invalid_Identifier(string content) {
            Assert.IsFalse(Test_Identifier(content));
        }

        [TestCase("1")]
        [TestCase("12")]
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
        [TestCase("/**///")]
        [TestCase("/*/")]
        [TestCase("/*//")]
        [TestCase("/*/*//")]
        public void Invalid_MultilineComment(string content) {
            Assert.IsFalse(Test_MultilineComment(content));
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