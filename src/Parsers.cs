using System.Linq;

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
        public Identifier() { }

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
        public Integer() { }

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
        public LineComment() { }

        public override bool ValidateNext(char c) {
            if (Text.Length <= 1) {
                return c == '/';
            }
            return Text.Last() != '\n';
        }
        public override bool IsValid() {
            return Text.Length >= 2 && Enumerable.SequenceEqual(Text.Take(2), _opening);
        }

        private char[] _opening = new char[] {'/', '/'};
    }
    public class MultilineComment : Parser {
        public MultilineComment() { }

        public override bool ValidateNext(char c) {
            if (Text.Length == 0) {
                return c == '/';
            } else if (Text.Length == 1) {
                return c == '*';
            } else if (Text.Length >= 4 && Enumerable.SequenceEqual(Text.TakeLast(2).Append(c), _breakOut)) {
                return true;
            } else if (Text.Length >= 4 && _nestingLevel == 0) {
                return false;
            }
            return true;
        }
        public override bool IsValid() {
            return Text.Length >= 4 && _nestingLevel <= 0;
        }
        public override void Add(char c) {
            Text += c;

            if (Text.Length >= 2 && Enumerable.SequenceEqual(Text.TakeLast(2), _opening)) {
                _nestingLevel++;
            } else if (Text.Length >= 5 && Enumerable.SequenceEqual(Text.TakeLast(3), _breakOut)) {
                _nestingLevel = 0;
            } else if (Text.Length >= 4 && Enumerable.SequenceEqual(Text.TakeLast(2), _closing)) {
                _nestingLevel--;
            }
        }

        private int _nestingLevel = 0;
        private char[] _opening = new char[] {'/', '*'};
        private char[] _closing = new char[] {'*', '/'};
        private char[] _breakOut = new char[] {'*', '/', '/'};
    }
}