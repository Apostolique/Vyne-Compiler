using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

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

            Parsers.MultilineComment p = new Parsers.MultilineComment();
            for (int i = 0; i < content.Length; i++) {
                if (!p.ValidateNext(content[i])) {
                    break;
                }
                p.Add(content[i]);
            }

            string output = JsonConvert.SerializeObject(p, Formatting.Indented);
            Console.WriteLine(output);
            Console.WriteLine("IsValid: " + p.IsValid());

            using(StreamWriter writer = new StreamWriter(outputFile)) {
                writer.Write(output);
            }
            Console.WriteLine("Done.");
        }
    }
}