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

            Parsers.Expression p = new Parsers.Expression();
            for (int i = 0; i < content.Length; i++) {
                if (!p.ValidateNext(content[i])) {
                    break;
                }
                p.Add(content[i]);
            }
            Console.WriteLine("IsValid: " + p.IsValid());

            string output = JsonConvert.SerializeObject(p, Formatting.Indented, new JsonSerializerSettings {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            });
            Console.WriteLine(output);

            using(StreamWriter writer = new StreamWriter(outputFile)) {
                writer.Write(output);
            }
            Console.WriteLine("Done.");
        }
    }
}