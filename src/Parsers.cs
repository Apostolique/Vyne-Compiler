using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;

namespace VyneCompiler.Parsers {
    /// <summary>
    /// Base class for parsers.
    /// It's up to the parser's parent to clean up white space and comments after it.
    /// </summary>
    public abstract class Parser {
        [DefaultValue("")]
        public string Text {
            get;
            set;
        } = "";

        /// <summary>
        /// This must always be called before Add is called.
        /// </summary>
        /// <returns>Returns true when Add can be called.</returns>
        public abstract bool ValidateNext(char c);
        /// <summary>
        /// Called at the end to make sure the parse is still valid.
        /// </summary>
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

        private char[] _opening = new char[] { '/', '/' };
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
        private char[] _opening = new char[] { '/', '*' };
        private char[] _closing = new char[] { '*', '/' };
        private char[] _breakOut = new char[] { '*', '/', '/' };
    }
    public class Whitespace : Parser {
        public override bool ValidateNext(char c) {
            return char.IsWhiteSpace(c);
        }
        public override bool IsValid() {
            return Text.Length > 0;
        }
    }
    public class Operator : Parser {
        public Operator(string sequence) {
            _sequence = sequence.ToArray();
        }

        public override bool ValidateNext(char c) {
            return Text.Length < _sequence.Length && c == _sequence[Text.Length];
        }
        public override bool IsValid() {
            return Text.Length == _sequence.Length;
        }

        private char[] _sequence;
    }
    public class BinaryOperator<T> : Parser where T : Parser, new() {
        public BinaryOperator(string sequence) {
            Operator = new Operator(sequence);
        }

        public T Left = new T();
        public Operator Operator;
        public T Right = new T();

        public override bool ValidateNext(char c) {
            if (!_isLeftDone) {
                _isLeftDone = !Left.ValidateNext(c);
            }
            if (_isLeftDone && !_isDiscard1Done) {
                _isDiscard1Done = !_discard.ValidateNext(c);
            }
            if (_isDiscard1Done && !_isOperatorDone) {
                _isOperatorDone = !Operator.ValidateNext(c);
            }
            if (_isOperatorDone && !_isDiscard2Done) {
                _isDiscard2Done = !_discard.ValidateNext(c);
            }
            if (_isDiscard2Done && !_isRightDone) {
                _isRightDone = !Right.ValidateNext(c);
            }
            return !_isRightDone;
        }
        public override bool IsValid() {
            return Left.IsValid() && Operator.IsValid() && Right.IsValid();
        }
        public override void Add(char c) {
            if (!_isLeftDone) {
                Left.Add(c);
            }
            if (_isLeftDone && !_isDiscard1Done) {
                _discard.Add(c);
            }
            if (_isDiscard1Done && !_isOperatorDone) {
                Operator.Add(c);
            }
            if (_isOperatorDone && !_isDiscard2Done) {
                _discard.Add(c);
            }
            if (_isDiscard2Done && !_isRightDone) {
                Right.Add(c);
            }
        }

        private bool _isLeftDone = false;
        private bool _isDiscard1Done = false;
        private bool _isOperatorDone = false;
        private bool _isDiscard2Done = false;
        private bool _isRightDone = false;
        private Whitespace _discard = new Whitespace();
    }
    public class FactorOperator : Parser {
        public BinaryOperator<Factor> Multiply = new BinaryOperator<Factor>("*");
        public BinaryOperator<Factor> Divide = new BinaryOperator<Factor>("/");
        public BinaryOperator<Factor> Modulo = new BinaryOperator<Factor>("%");

        public override bool ValidateNext(char c) {
            if (_validNextMultiply) {
                _validNextMultiply = Multiply.ValidateNext(c);
            }
            if (_validNextDivide) {
                _validNextDivide = Divide.ValidateNext(c);
            }
            if (_validNextModulo) {
                _validNextModulo = Modulo.ValidateNext(c);
            }
            return _validNextMultiply || _validNextDivide || _validNextModulo;
        }
        public override bool IsValid() {
            bool validMultiply = false;
            bool validDivide = false;
            bool validModulo = false;
            if (Multiply != null) {
                validMultiply = Multiply.IsValid();
            }
            if (Divide != null) {
                validDivide = Divide.IsValid();
            }
            if (Modulo != null) {
                validModulo = Modulo.IsValid();
            }

            // This is done to have a nicer json output.
            if (!validMultiply) {
                Multiply = null;
            }
            if (!validDivide) {
                Divide = null;
            }
            if (!validModulo) {
                Modulo = null;
            }

            return validMultiply || validDivide || validModulo;
        }
        public override void Add(char c) {
            if (_validNextMultiply) {
                Multiply.Add(c);
            } else {
                Multiply = null;
            }
            if (_validNextDivide) {
                Divide.Add(c);
            } else {
                Divide = null;
            }
            if (_validNextModulo) {
                Modulo.Add(c);
            } else {
                Modulo = null;
            }
        }

        private bool _validNextMultiply = true;
        private bool _validNextDivide = true;
        private bool _validNextModulo = true;
    }
    public class TermOperator : Parser {
        public BinaryOperator<Term> Addition = new BinaryOperator<Term>("+");
        public BinaryOperator<Term> Subtract = new BinaryOperator<Term>("-");

        public override bool ValidateNext(char c) {
            if (_validNextAddition) {
                _validNextAddition = Addition.ValidateNext(c);
            }
            if (_validNextSubtract) {
                _validNextSubtract = Subtract.ValidateNext(c);
            }
            return _validNextAddition || _validNextSubtract;
        }
        public override bool IsValid() {
            bool validAddition = false;
            bool validSubtract = false;
            if (Addition != null) {
                validAddition = Addition.IsValid();
            }
            if (Subtract != null) {
                validSubtract = Subtract.IsValid();
            }

            // This is done to have a nicer json output.
            if (!validAddition) {
                Addition = null;
            }
            if (!validSubtract) {
                Subtract = null;
            }

            return validAddition || validSubtract;
        }
        public override void Add(char c) {
            if (_validNextAddition) {
                Addition.Add(c);
            } else {
                Addition = null;
            }
            if (_validNextSubtract) {
                Subtract.Add(c);
            } else {
                Subtract = null;
            }
        }

        private bool _validNextAddition = true;
        private bool _validNextSubtract = true;
    }
    public class FactorIdentifier : Parser {
        public Identifier Identifier = new Identifier();

        public override bool ValidateNext(char c) {
            return Identifier.ValidateNext(c);
        }
        public override bool IsValid() {
            return Identifier.IsValid();
        }
        public override void Add(char c) {
            Identifier.Add(c);
        }
    }
    public class Factor : Parser {
        // Note: This code is simplified. It will most likely not work in some cases.
        public Identifier Identifier = new Identifier();
        public Integer Integer = new Integer();
        //public Expression Expression = new Expression(); FIXME

        public override bool ValidateNext(char c) {
            if (_validNextIdentifier) {
                _validNextIdentifier = Identifier.ValidateNext(c);
            }
            if (_validNextInteger) {
                _validNextInteger = Integer.ValidateNext(c);
            }
            /*if (_validNextExpression) {
                _validNextExpression = Expression.ValidateNext(c);
            } FIXME*/
            return _validNextIdentifier || _validNextInteger/*|| _validNextExpression FIXME*/;
        }
        public override bool IsValid() {
            bool validIdentifier = false;
            bool validInteger = false;
            bool validExpression = false;
            if (Identifier != null) {
                validIdentifier = Identifier.IsValid();
            }
            if (Integer != null) {
                validInteger = Integer.IsValid();
            }
            /*if (Expression != null) {
                validExpression = Expression.IsValid();
            } FIXME*/

            // This is done to have a nicer json output.
            if (!validIdentifier) {
                Identifier = null;
            }
            if (!validInteger) {
                Integer = null;
            }
            /*if (!validExpression) {
                Expression = null;
            } FIXME*/

            return validIdentifier || validInteger || validExpression;
        }
        public override void Add(char c) {
            if (_validNextIdentifier) {
                Identifier.Add(c);
            } else {
                Identifier = null;
            }
            if (_validNextInteger) {
                Integer.Add(c);
            } else {
                Integer = null;
            }
            /*if (_validNextExpression) {
                Expression.Add(c);
            } else {
                Expression = null;
            } FIXME*/
        }

        private bool _validNextIdentifier = true;
        private bool _validNextInteger = true;
        //private bool _validNextExpression = true; FIXME
    }
    public class Term : Parser {
        public Factor Factor = new Factor();
        public FactorOperator FactorOperator = new FactorOperator();

        public override bool ValidateNext(char c) {
            if (_validNextFactor) {
                _validNextFactor = Factor.ValidateNext(c);
            }
            if (_validNextFactorOperator) {
                _validNextFactorOperator = FactorOperator.ValidateNext(c);
            }

            return _validNextFactor || _validNextFactorOperator;
        }
        public override bool IsValid() {
            bool validTermFactor = false;
            bool validTermBinary = false;
            if (Factor != null) {
                validTermFactor = Factor.IsValid();
            }
            if (FactorOperator != null) {
                validTermBinary = FactorOperator.IsValid();
            }

            // This is done to have a nicer json output.
            if (!validTermFactor) {
                Factor = null;
            }
            if (!validTermBinary) {
                FactorOperator = null;
            }

            return validTermFactor || validTermBinary;
        }
        public override void Add(char c) {
            if (_validNextFactor) {
                Factor.Add(c);
            } else {
                Factor = null;
            }
            if (_validNextFactorOperator) {
                FactorOperator.Add(c);
            } else {
                FactorOperator = null;
            }
        }

        private bool _validNextFactor = true;
        private bool _validNextFactorOperator = true;
    }
    public class Expression : Parser {
        public Term Term = new Term();
        public TermOperator TermOperator = new TermOperator();

        public override bool ValidateNext(char c) {
            if (_validNextTerm) {
                _validNextTerm = Term.ValidateNext(c);
            }
            if (_validNextTermOperator) {
                _validNextTermOperator = TermOperator.ValidateNext(c);
            }
            return _validNextTerm || _validNextTermOperator;
        }
        public override bool IsValid() {
            bool validTerm = false;
            bool validTermOperator = false;
            if (Term != null) {
                validTerm = Term.IsValid();
            }
            if (TermOperator != null) {
                validTermOperator = TermOperator.IsValid();
            }

            // This is done to have a nicer json output.
            if (!validTerm) {
                Term = null;
            }
            if (!validTermOperator) {
                TermOperator = null;
            }

            return validTerm || validTermOperator;
        }
        public override void Add(char c) {
            if (_validNextTerm) {
                Term.Add(c);
            } else {
                Term = null;
            }
            if (_validNextTermOperator) {
                TermOperator.Add(c);
            } else {
                TermOperator = null;
            }
        }

        private bool _validNextTerm = true;
        private bool _validNextTermOperator = true;
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
        [TestCase("*/")]
        [TestCase("*//")]
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