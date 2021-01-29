using System.IO;

namespace VyneCompiler {
    public static class Source {
        public static void Setup(string inputFile) {
            using StreamReader sr = new StreamReader(inputFile);
            _source = sr.ReadToEnd();
        }
        public static char GetCharAt(int index) {
            if (index < _source.Length) {
                return _source[index];
            }
            return '\0';
        }
        public static string GetStringAt(int index, int length) {
            if (index + length <= _source.Length) {
                return _source.Substring(index, length);
            }
            return string.Empty;
        }
        public static bool IsEndReached(int index) {
            return index > _source.Length;
        }
        public static bool IsEndReached(int index, int length) {
            return index + length > _source.Length;
        }

        private static string _source;
    }
}
