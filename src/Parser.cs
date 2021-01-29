using System;

namespace VyneCompiler.P {
    public abstract class Parser {
        public abstract (A.AST AST, int Index)? Parse(int index);
    }
    public class LineComment : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            if ("//" == Source.GetStringAt(index, 2)) {
                string lineComment = "";

                index += 2;
                while (true) {
                    index += 1;
                    if ('\n' == Source.GetCharAt(index)) {
                        break;
                    } else if ("\r\n" == Source.GetStringAt(index, 2)) {
                        index += 1;
                        break;
                    }

                    lineComment += Source.GetCharAt(index);
                }

                return (new A.LineComment(lineComment), index);
            }
            return null;
        }
    }
    public class Multiply : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            if (Source.GetCharAt(index) == '*') {
                return (new A.MultiplyChar(), index + 1);
            }

            return null;
        }
    }
    public class Divide : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            if (Source.GetCharAt(index) == '/') {
                return (new A.DivideChar(), index + 1);
            }

            return null;
        }
    }
    public class Modulo : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            if (Source.GetCharAt(index) == '%') {
                return (new A.ModuloChar(), index + 1);
            }

            return null;
        }
    }
    public class Add : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            if (Source.GetCharAt(index) == '+') {
                return (new A.AddChar(), index + 1);
            }

            return null;
        }
    }
    public class Substract : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            if (Source.GetCharAt(index) == '-') {
                return (new A.SubstractChar(), index + 1);
            }

            return null;
        }
    }
    public class Id : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            char current = Source.GetCharAt(index);
            if (Char.IsLetter(current)) {
                index += 1;
                string id = $"{current}";

                while (true) {
                    current = Source.GetCharAt(index);
                    if (Char.IsLetterOrDigit(current)) {
                        index += 1;
                        id += current;
                    } else {
                        break;
                    }
                }

                return (new A.Id(id), index);
            }
            return null;
        }
    }
    public class Number : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            char current = Source.GetCharAt(index);
            if (Char.IsNumber(current)) {
                index += 1;
                string number = $"{current}";

                while (true) {
                    current = Source.GetCharAt(index);
                    if (Char.IsNumber(current)) {
                        index += 1;
                        number += current;
                    } else {
                        break;
                    }
                }

                return (new A.Number(Int32.Parse(number)), index);
            }
            return null;
        }
    }
    public class Atom : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            return null;
        }
    }
    public class Factor : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            var number = new Number();
            var result = number.Parse(index);
            if (result != null) {
                return (new A.Factor(result.Value.AST), result.Value.Index);
            }

            var id = new Id();
            result = id.Parse(index);
            if (result != null) {
                return (new A.Factor(result.Value.AST), result.Value.Index);
            }

            return null;
        }
    }
    public class TermOperator : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            var multiply = new Multiply();
            var result = multiply.Parse(index);
            if (result != null) {
                return result;
            }

            var divide = new Divide();
            result = divide.Parse(index);
            if (result != null) {
                return result;
            }

            var modulo = new Modulo();
            result = modulo.Parse(index);
            if (result != null) {
                return result;
            }

            return null;
        }
    }
    public class ExpressionOperator : Parser {
        public override (A.AST AST, int Index)? Parse(int index) {
            var add = new Add();
            var result = add.Parse(index);
            if (result != null) {
                return result;
            }

            var substract = new Substract();
            result = substract.Parse(index);
            if (result != null) {
                return result;
            }

            return null;
        }
    }
    public class Term : Parser {
        public Term() { }

        public override (A.AST AST, int Index)? Parse(int index) {
            var factor = new Factor();
            var result = factor.Parse(index);
            if (result != null) {
                return Parse(result.Value.Index, result.Value.AST);
            }

            return null;
        }
        private (A.AST AST, int Index)? Parse(int index, A.AST factor1) {
            // 1. Parse operator
            // 2. Parse second factor
            // 3. Combine factor1 and factor2 into an ast node
            // 4. Use that new node recursively

            var termOperator = new TermOperator();
            var termResult = termOperator.Parse(index);
            if (termResult != null) {
                var factor = new Factor();
                var factor2 = factor.Parse(termResult.Value.Index);
                if (factor2 != null) {
                    switch (termResult.Value.AST) {
                        case A.MultiplyChar:
                            return Parse(factor2.Value.Index, new A.Multiply(factor1, factor2.Value.AST));
                        case A.DivideChar:
                            return Parse(factor2.Value.Index, new A.Divide(factor1, factor2.Value.AST));
                        case A.ModuloChar:
                            return Parse(factor2.Value.Index, new A.Modulo(factor1, factor2.Value.AST));
                    }
                }
            }

            return (factor1, index);
        }
    }
    public class Expression : Parser {
        public Expression() { }

        public override (A.AST AST, int Index)? Parse(int index) {
            var term = new Term();
            var result = term.Parse(index);
            if (result != null) {
                return Parse(result.Value.Index, result.Value.AST);
            }

            return null;
        }
        private (A.AST AST, int Index)? Parse(int index, A.AST term1) {
            // 1. Parse operator
            // 2. Parse second factor
            // 3. Combine factor1 and factor2 into an ast node
            // 4. Use that new node recursively

            var expressionOperator = new ExpressionOperator();
            var expressionResult = expressionOperator.Parse(index);
            if (expressionResult != null) {
                var term = new Term();
                var term2 = term.Parse(expressionResult.Value.Index);
                if (term2 != null) {
                    switch (expressionResult.Value.AST) {
                        case A.AddChar:
                            return Parse(term2.Value.Index, new A.Add(term1, term2.Value.AST));
                        case A.SubstractChar:
                            return Parse(term2.Value.Index, new A.Substract(term1, term2.Value.AST));
                    }
                }
            }

            return (term1, index);
        }
    }
}
