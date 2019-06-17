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

            Sequential p = new Sequential(new Integer(), new Identifier(), new Integer());
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