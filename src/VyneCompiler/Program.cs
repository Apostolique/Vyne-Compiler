using System;
using System.IO;
using Newtonsoft.Json;
using VyneCompiler.Parsers;

namespace VyneCompiler {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Started Vyne compiler.");

            string inputFile = "VyneSource/HelloWorld.vyne";
            string outputFile = "VyneSource/HelloWorld.json";
            Core.Setup(inputFile);

            Sequential p = new Sequential(
                () => new Sequential(
                    () => new Factor(),
                    () => new Repeat("FactorOperator", () =>
                        new Sequential(
                            () => new Alternative("Operator",
                                () => new Token("Multiply", "*"),
                                () => new Token("Divide", "/"),
                                () => new Token("Modulo", "%")
                            ),
                            () => new Factor()
                        )
                    )
                )
            );
            for (int i = 0; !Core.IsEndReached(i); i++) {
                if (!p.ValidateNext(Core.GetCharAt(i))) {
                    break;
                }
                p.Add(Core.GetCharAt(i));
            }
            Console.WriteLine("IsValid: " + p.IsValid());

            string output = JsonConvert.SerializeObject(p.ToJson(), Formatting.Indented);
            Console.WriteLine(output);

            using(StreamWriter writer = new StreamWriter(outputFile)) {
                writer.Write(output);
            }
            Console.WriteLine("Done.");
        }
    }
}