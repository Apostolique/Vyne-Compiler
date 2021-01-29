using System.Collections.Generic;

namespace VyneCompiler.A {
    public abstract class AST {
    }
    public class LineComment : AST {
        public LineComment(string value) {
            Value = value;
        }

        public string Value {
            get;
            set;
        }
    }
    public class MultiplyChar : AST { }
    public class DivideChar : AST { }
    public class ModuloChar : AST { }
    public class AddChar : AST { }
    public class SubstractChar : AST { }
    public class Number : AST {
        public Number(int value) {
            Value = value;
        }

        public int Value {
            get;
            set;
        }
    }
    public class Id : AST {
        public Id(string value) {
            Value = value;
        }

        public string Value {
            get;
            set;
        }
    }
    public class Factor : AST {
        public Factor(AST value) {
            Value = value;
        }

        public AST Value {
            get;
            set;
        }
    }
    public class Not : AST {
        public Not(AST term) {
            Term = term;
        }

        public AST Term {
            get;
            set;
        }
    }
    public class Equal : AST {
        public Equal(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left {
            get;
            set;
        }
        public AST Right {
            get;
            set;
        }
    }
    public class NotEqual : AST {
        public NotEqual(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left {
            get;
            set;
        }
        public AST Right {
            get;
            set;
        }
    }
    public class Add : AST {
        public Add(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left {
            get;
            set;
        }
        public AST Right {
            get;
            set;
        }
    }
    public class Substract : AST {
        public Substract(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left {
            get;
            set;
        }
        public AST Right {
            get;
            set;
        }
    }
    public class Multiply : AST {
        public Multiply(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left {
            get;
            set;
        }
        public AST Right {
            get;
            set;
        }
    }
    public class Divide : AST {
        public Divide(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left {
            get;
            set;
        }
        public AST Right {
            get;
            set;
        }
    }
    public class Modulo : AST {
        public Modulo(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left {
            get;
            set;
        }
        public AST Right {
            get;
            set;
        }
    }
    public class Call : AST {
        public Call(string callee, List<AST> args) {
            Callee = callee;
            Args = args;
        }

        public string Callee {
            get;
            set;
        }
        public List<AST> Args {
            get;
            set;
        }
    }
    public class Return : AST {
        public Return(AST term) {
            Term = term;
        }

        public AST Term {
            get;
            set;
        }
    }
    public class Block : AST {
        public Block(List<AST> statements) {
            Statements = statements;
        }

        public List<AST> Statements {
            get;
            set;
        }
    }
    public class If : AST {
        public If(AST conditional, AST consequence, AST alternative) {
            Conditional = conditional;
            Consequence = consequence;
            Alternative = alternative;
        }

        public AST Conditional {
            get;
            set;
        }
        public AST Consequence {
            get;
            set;
        }
        public AST Alternative {
            get;
            set;
        }
    }
    public class Function : AST {
        public Function(string name, List<string> parameters, AST body) {
            Name = name;
            Parameters = parameters;
            Body = body;
        }

        public string Name {
            get;
            set;
        }
        public List<string> Parameters {
            get;
            set;
        }
        public AST Body {
            get;
            set;
        }
    }
    public class Let : AST {
        public Let(string name, AST value) {
            Name = name;
            Value = value;
        }

        public string Name {
            get;
            set;
        }
        public AST Value {
            get;
            set;
        }
    }
    public class Assign : AST {
        public Assign(string name, AST value) {
            Name = name;
            Value = value;
        }

        public string Name {
            get;
            set;
        }
        public AST Value {
            get;
            set;
        }
    }
    public class While : AST {
        public While(AST conditional, AST body) {
            Conditional = conditional;
            Body = body;
        }

        public AST Conditional {
            get;
            set;
        }
        public AST Body {
            get;
            set;
        }
    }
}
