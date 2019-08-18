using System;
using NUnit.Framework;
using VyneCompiler.Parsers;

namespace VyneCompiler.Tests {
    public class ParserTests {
        [TestCase("hello")]
        [TestCase("hello123")]
        [TestCase("_hello")]
        [TestCase("_hello_world")]
        [TestCase("_123")]
        public void Valid_Identifier(string content) {
            Assert.IsTrue(Test_Identifier(content));
        }

        [TestCase("")]
        [TestCase("123")]
        [TestCase("hello world")]
        [TestCase("    hello")]
        [TestCase("/*+-")]
        public void Invalid_Identifier(string content) {
            Assert.IsFalse(Test_Identifier(content));
        }

        [TestCase("1")]
        [TestCase("12")]
        public void Valid_Integer(string content) {
            Assert.IsTrue(Test_Integer(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        [TestCase("    12")]
        public void Invalid_Integer(string content) {
            Assert.IsFalse(Test_Integer(content));
        }

        [TestCase("// Hello")]
        [TestCase("// Hello\n")]
        [TestCase("// Hello\r\n")]
        public void Valid_LineComment(string content) {
            Assert.IsTrue(Test_LineComment(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        public void Invalid_LineComment(string content) {
            Assert.IsFalse(Test_LineComment(content));
        }

        [TestCase("/**/")]
        [TestCase("/**//")]
        [TestCase("/* Hello */")]
        [TestCase("/* Hello\n   World */")]
        [TestCase("/* Hello /* World */*/")]
        [TestCase("/* Hello /* World *//")]
        [TestCase("/* /* /* /* /* /* /* *//")]
        [TestCase("/*/*/*/**/*/*/*/")]
        [TestCase("/*/*/*/**//")]
        public void Valid_MultilineComment(string content) {
            Assert.IsTrue(Test_MultilineComment(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        [TestCase("*/")]
        [TestCase("*//")]
        [TestCase("/**///")]
        [TestCase("/*/")]
        [TestCase("/*//")]
        [TestCase("/*/*//")]
        public void Invalid_MultilineComment(string content) {
            Assert.IsFalse(Test_MultilineComment(content));
        }

        [TestCase("    ")]
        [TestCase("\t")]
        [TestCase("\n")]
        [TestCase("\r\n")]
        public void Valid_Whitespace(string content) {
            Assert.IsTrue(Test_Whitespace(content));
        }

        [TestCase("")]
        [TestCase("hello")]
        public void Invalid_Whitespace(string content) {
            Assert.IsFalse(Test_Whitespace(content));
        }

        [TestCase("/*Hello*/123")]
        public void Valid_Sequential(string content) {
            Assert.IsTrue(Test_Sequential(content));
        }

        [TestCase("123")]
        [TestCase("hello")]
        public void Valid_Alternative(string content) {
            Assert.IsTrue(Test_Alternative(content));
        }

        [TestCase("1")]
        public void Valid_Single(string content) {
            Assert.IsTrue(Test_Single(content));
        }

        [TestCase("12")]
        public void Invalid_Single(string content) {
            Assert.IsFalse(Test_Single(content));
        }

        [TestCase("1")]
        [TestCase("12")]
        [TestCase("123")]
        [TestCase("hello")]
        public void Valid_Repeat(string content) {
            Assert.IsTrue(Test_Repeat(content));
        }

        private bool Test_Identifier(string content) {
            Identifier p = new Identifier();
            return Test_Parser(p, content);
        }
        private bool Test_Integer(string content) {
            Integer p = new Integer();
            return Test_Parser(p, content);
        }
        private bool Test_LineComment(string content) {
            LineComment p = new LineComment();
            return Test_Parser(p, content);
        }
        private bool Test_MultilineComment(string content) {
            MultilineComment p = new MultilineComment();
            return Test_Parser(p, content);
        }
        private bool Test_Whitespace(string content) {
            Whitespace p = new Whitespace();
            return Test_Parser(p, content);
        }
        private bool Test_Sequential(string content) {
            Sequential p = new Sequential(
                () => new MultilineComment(),
                () => new Integer()
            );
            return Test_Parser(p, content);
        }
        private bool Test_Alternative(string content) {
            Alternative p = new Alternative("Test", () => new Identifier(), () => new Integer());
            return Test_Parser(p, content);
        }
        private bool Test_Single(string content) {
            Parsers.Single p = new Parsers.Single();
            return Test_Parser(p, content);
        }
        private bool Test_Repeat(string content) {
            Repeat p = new Repeat("Test", () => new Parsers.Single());
            return Test_Parser(p, content);
        }
        private bool Test_Parser(Parser p, string content) {
            foreach (char c in content) {
                if (!p.ValidateNext(c)) {
                    return false;
                }
                p.Add(c);
            }
            return p.IsValid();
        }
    }
}