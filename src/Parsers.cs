using System.Linq;

namespace VyneCompiler.Parsers {
    public abstract class Parser {
        public string Text {
            get;
            set;
        } = "";

        public abstract bool PossibleNext(char c);

        public virtual void Add(char c) {
            Text += c;
        }
    }
    public class Identifier : Parser {
        public Identifier() { }

        public override bool PossibleNext(char c) {
            if (c == '_') {
                return true;
            } else if (Text.Length == 0) {
                return char.IsLetter(c);
            } else {
                return char.IsLetterOrDigit(c);
            }
        }
    }
    public class Integer : Parser {
        public Integer() { }

        public override bool PossibleNext(char c) {
            return char.IsDigit(c);
        }
    }
    public class LineComment : Parser {
        public LineComment() { }

        public override bool PossibleNext(char c) {
            if (Text.Length <= 2) {
                return c == '/';
            }
            return Text.Last() != '\n';
        }
    }
    public class MultilineComment : Parser {
        public MultilineComment() { }

        public override bool PossibleNext(char c) {
            if (Text.Length == 0) {
                return c == '/';
            } else if (Text.Length == 1) {
                return c == '*';
            } else if (Text.Length >= 4 && Enumerable.SequenceEqual(Text.TakeLast(2).Append(c), new char[] {'*', '/', '/'})) {
                return true;
            } else if (Text.Length >= 4 && _nestingLevel == 0) {
                return false;
            }
            return true;
        }
        public override void Add(char c) {
            Text += c;

            if (Text.Length >= 2 && Enumerable.SequenceEqual(Text.TakeLast(2), new char[] {'/', '*'})) {
                _nestingLevel++;
            } else if (Text.Length >= 5 && Enumerable.SequenceEqual(Text.TakeLast(3), new char[] {'*', '/', '/'})) {
                _nestingLevel = 0;
            } else if (Text.Length >= 4 && Enumerable.SequenceEqual(Text.TakeLast(2), new char[] {'*', '/'})) {
                _nestingLevel--;
            }
        }

        private int _nestingLevel = 0;
    }
}