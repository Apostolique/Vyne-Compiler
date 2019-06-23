using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using VyneCompiler.Parsers;

namespace VyneCompiler {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Started Vyne compiler.");

            string inputFile = "VyneSource/HelloWorld.vyne";
            string outputFile = "VyneSource/HelloWorld.json";
            string content;
            using(StreamReader sr = new StreamReader(inputFile, Encoding.UTF8)) {
                content = sr.ReadToEnd();
            }

            Sequential p = new Sequential(
                new Lazy<Parser>(() => new Repeat("FactorOperator", () =>
                    new Sequential(
                        new Lazy<Parser>(() => new Factor()), // Error, on the last loop this gets consumed before exiting the repeat.
                        new Lazy<Parser>(() => new Alternative("Operator",
                            new Lazy<Parser>(() => new Token("Multiply", "*")),
                            new Lazy<Parser>(() => new Token("Divide", "/")),
                            new Lazy<Parser>(() => new Token("Modulo", "%"))
                        ))
                    )
                )),
                new Lazy<Parser>(() => new Factor())
            );
            for (int i = 0; i < content.Length; i++) {
                if (!p.ValidateNext(content[i])) {
                    break;
                }
                p.Add(content[i]);
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