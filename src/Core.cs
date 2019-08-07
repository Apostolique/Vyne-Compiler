using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VyneCompiler.Parsers {
    public static class Core {
        public static void Setup(string inputFile) {
            using(StreamReader sr = new StreamReader(inputFile, Encoding.UTF8)) {
                _source = sr.ReadToEnd();
            }
        }
        public static char? GetCharAt(int index) {
            if (index < _source.Length) {
                return _source[index];
            }
            return null;
        }
        public static string GetStringAt(int index, int length) {
            if (index + length <= _source.Length) {
                return _source.Substring(index, length);
            }
            return string.Empty;
        }
        public static bool IsEndReached(int index) {
            return index >= _source.Length;
        }

        private static string _source;
    }
}