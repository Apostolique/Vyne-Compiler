namespace VyneCompiler.Constructs {
    public class BinaryOperator<T> {
        public T Left;
        public T Right;
    }

    public class FactorOperator : BinaryOperator<Factor> { }
    public class Multiply : FactorOperator { }
    public class Divide : FactorOperator { }
    public class Modulo : FactorOperator { }

    public class TermOperator : BinaryOperator<Term> { }
    public class Add : TermOperator { }
    public class Substract : TermOperator { }

    public abstract class Factor { }
    public class FactorExpression : Factor {
        public Expression Expression;
    }
    public class FactorIdentifier : Factor {
        public string Name;
    }
    public class FactorInteger : Factor {
        public int Number;
    }

    public abstract class Term { }
    public class TermFactor : Term {
        public Factor Factor;
    }
    public class TermBinary : Term {
        public FactorOperator BinaryOperation;
    }

    public abstract class Expression { }
    public class ExpressionTerm : Expression {
        public Term Term;
    }
    public class ExpressionBinary : Expression {
        public TermOperator BinaryOperation;
    }
}