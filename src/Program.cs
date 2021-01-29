using System;
using CommandLine;

namespace VyneCompiler {
    class Program {
        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o => {
                Source.Setup(o.Source);

                P.Parser p = new P.Expression();
                var result = p.Parse(0);

                if (result != null) {
                    PrintValue(result.Value.AST);
                } else {
                    Print("Failed to parse :/");
                }
            });
        }

        public static void PrintValue(A.AST? ast, int indent = 0) {
            switch (ast) {
                case A.LineComment lc:
                    Print($"Line Comment: {lc.Value}", indent);
                    break;
                case A.Number num:
                    Print($"Number: {num.Value}", indent);
                    break;
                case A.Id id:
                    Print($"Number: {id.Value}", indent);
                    break;
                case A.Factor f:
                    Print($"Factor:", indent);
                    PrintValue(f.Value, indent + 3);
                    break;
                case A.Multiply f:
                    Print($"Multiply:", indent);
                    PrintValue(f.Left, indent + 3);
                    PrintValue(f.Right, indent + 3);
                    break;
                case A.Divide f:
                    Print($"Divide:", indent);
                    PrintValue(f.Left, indent + 3);
                    PrintValue(f.Right, indent + 3);
                    break;
                case A.Modulo f:
                    Print($"Modulo:", indent);
                    PrintValue(f.Left, indent + 3);
                    PrintValue(f.Right, indent + 3);
                    break;
                case A.Add a:
                    Print($"Add:", indent);
                    PrintValue(a.Left, indent + 3);
                    PrintValue(a.Right, indent + 3);
                    break;
                case A.Substract s:
                    Print($"Substract:", indent);
                    PrintValue(s.Left, indent + 3);
                    PrintValue(s.Right, indent + 3);
                    break;
                default:
                    Print($"Failed to parse :/", indent);
                    break;
            }
        }
        public static void Print(string text, int indent = 0) {
            Console.WriteLine("".PadLeft(indent) + text);
        }

        public class Options {
            [Option('s', "source", Required = true, HelpText = "Source code to compile.")]
            public string Source {
                get;
                set;
            }
        }
    }
}
