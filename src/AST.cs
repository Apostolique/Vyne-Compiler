using System;
using System.Linq;

namespace VyneCompiler.A {
    public abstract class AST {
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();
    }

    public class Integer : AST {
        public Integer(int value) {
            Value = value;
        }

        public int Value { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Integer)) return false;

            Integer o = (Integer)obj;
            return Value == o.Value;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Value);
        }
    }

    public class Id : AST {
        public Id(string value) {
            Value = value;
        }

        public string Value { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Id)) return false;

            Id o = (Id)obj;
            return Value == o.Value;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Value);
        }
    }

    public class Not : AST {
        public Not(string term) {
            Term = term;
        }

        public string Term { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Not)) return false;

            Not o = (Not)obj;
            return Term == o.Term;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Term);
        }
    }

    public class Equal : AST {
        public Equal(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left { get; set; }
        public AST Right { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Equal)) return false;

            Equal o = (Equal)obj;
            return Left == o.Left && Right == o.Right;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right);
        }
    }

    public class NotEqual : AST {
        public NotEqual(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left { get; set; }
        public AST Right { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is NotEqual)) return false;

            NotEqual o = (NotEqual)obj;
            return Left == o.Left && Right == o.Right;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right);
        }
    }

    public class Add : AST {
        public Add(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left { get; set; }
        public AST Right { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Add)) return false;

            Add o = (Add)obj;
            return Left == o.Left && Right == o.Right;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right);
        }
    }

    public class Subtract : AST {
        public Subtract(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left { get; set; }
        public AST Right { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Subtract)) return false;

            Subtract o = (Subtract)obj;
            return Left == o.Left && Right == o.Right;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right);
        }
    }

    public class Multiply : AST {
        public Multiply(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left { get; set; }
        public AST Right { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Multiply)) return false;

            Multiply o = (Multiply)obj;
            return Left == o.Left && Right == o.Right;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right);
        }
    }

    public class Divide : AST {
        public Divide(AST left, AST right) {
            Left = left;
            Right = right;
        }

        public AST Left { get; set; }
        public AST Right { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Divide)) return false;

            Divide o = (Divide)obj;
            return Left == o.Left && Right == o.Right;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right);
        }
    }

    public class Call : AST {
        public Call(string callee, AST[] args) {
            Callee = callee;
            Args = args;
        }

        public string Callee { get; set; }
        public AST[] Args { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Call)) return false;

            Call o = (Call)obj;
            return Callee == o.Callee && Enumerable.SequenceEqual(Args, o.Args);
        }

        public override int GetHashCode() {
            HashCode hash = new HashCode();
            hash.Add(Callee);
            for (int i = 0; i < Args.Length; i++) {
                hash.Add(Args[i]);
            }
            return hash.ToHashCode();
        }
    }

    public class Return : AST {
        public Return(AST term) {
            Term = term;
        }

        public AST Term { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Return)) return false;

            Return o = (Return)obj;
            return Term == o.Term;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Term);
        }
    }

    public class Block : AST {
        public Block(AST[] statements) {
            Statements = statements;
        }

        public AST[] Statements { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Block)) return false;

            Block o = (Block)obj;
            return Statements == o.Statements && Enumerable.SequenceEqual(Statements, o.Statements);
        }

        public override int GetHashCode() {
            HashCode hash = new HashCode();
            for (int i = 0; i < Statements.Length; i++) {
                hash.Add(Statements[i]);
            }
            return hash.ToHashCode();
        }
    }

    public class If : AST {
        public If(AST conditional, AST consequence, AST alternative) {
            Conditional = conditional;
            Consequence = consequence;
            Alternative = alternative;
        }

        public AST Conditional { get; set; }
        public AST Consequence { get; set; }
        public AST Alternative { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is If)) return false;

            If o = (If)obj;
            return Conditional == o.Conditional && Consequence == o.Consequence && Alternative == o.Alternative;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Consequence, Consequence, Alternative);
        }
    }

    public class FunctionDefinition : AST {
        public FunctionDefinition(string name, string[] parameters, AST body) {
            Name = name;
            Parameters = parameters;
            Body = body;
        }

        public string Name { get; set; }
        public string[] Parameters { get; set; }
        public AST Body { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is FunctionDefinition)) return false;

            FunctionDefinition o = (FunctionDefinition)obj;
            return Name == o.Name && Enumerable.SequenceEqual(Parameters, o.Parameters) && Body == o.Body;
        }

        public override int GetHashCode() {
            HashCode hash = new HashCode();
            hash.Add(Name);
            for (int i = 0; i < Parameters.Length; i++) {
                hash.Add(Parameters[i]);
            }
            hash.Add(Body);
            return hash.ToHashCode();
        }
    }

    public class Let : AST {
        public Let(string name, AST value) {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public AST Value { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Let)) return false;

            Let o = (Let)obj;
            return Name == o.Name && Value == o.Value;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Name, Value);
        }
    }

    public class Assign : AST {
        public Assign(string name, AST value) {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public AST Value { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is Let)) return false;

            Let o = (Let)obj;
            return Name == o.Name && Value == o.Value;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Name, Value);
        }
    }

    public class While : AST {
        public While(AST conditional, AST body) {
            Conditional = conditional;
            Body = body;
        }

        public AST Conditional { get; set; }
        public AST Body { get; set; }

        public override bool Equals(object? obj) {
            if (!(obj is While)) return false;

            While o = (While)obj;
            return Conditional == o.Conditional && Body == o.Body;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Conditional, Body);
        }
    }
}
