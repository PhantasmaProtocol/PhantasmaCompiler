using Phantasma.CodeGen.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Languages
{
    public class CSharpProcessor : LanguageProcessor
    {
        protected string[] _keywords = new string[]{
            "var", "using", "return",  "namespace", "public", "private", "protected",  "internal",
            "static", "virtual", "abstract", "class", "struct", "if", "else", "while", "do", "switch", "case"
        };

        public override Lexer Lexer => _lexer;
        public override Parser Parser => _parser;
        public override string Description => "C#";

        private Lexer _lexer;
        private Parser _parser;

        public CSharpProcessor()
        {
            _lexer = new DefaultLexer(_keywords);
            _parser = new CSharpParser();
        }
    }

    public class CSharpParser: DefaultParser
    {
        protected HashSet<string> _types = new HashSet<string>(new string[] { "int", "uint", "bool", "string" });

        protected override bool IsValidType(string token)
        {
            if (token == "var")
            {
                return true;
            }

            return _types.Contains(token);
        }

        public override ModuleNode Execute(List<Token> tokens)
        {
            int index = 0;

            var module = new ModuleNode();
            while (index < tokens.Count)
            {
                var token = tokens[index];
                index++;

                if (token.text == "using")
                {
                    var node = new ImportNode(module);
                    node.reference = ExpectIdentifier(tokens, ref index, true);
                    ExpectDelimiter(tokens, ref index, ";");
                }
                else
                if (token.text == "namespace")
                {
                    var namespaceID = ExpectIdentifier(tokens, ref index, true);

                    ExpectDelimiter(tokens, ref index, "{");

                    ParseNamespaceContent(tokens, ref index, module);

                    ExpectDelimiter(tokens, ref index, "}");
                }
                else
                {
                    throw new ParserException(token, ParserException.Kind.UnexpectedToken);
                }

            }

            return module;
        }

        private Visibility ParseVisibility(List<Token> tokens, ref int index, Visibility defaultVisibility = Visibility.Internal)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);
            var token = tokens[index];

            if (token.kind != Token.Kind.Keyword)
            {
                return defaultVisibility;
            }

            switch (token.text)
            {
                case "public": index++; return Visibility.Public;
                case "private": index++; return Visibility.Private;
                case "protected": index++; return Visibility.Protected;
                case "internal": index++; return Visibility.Internal;

                default: return defaultVisibility;
            }
        }

        private void ParseNamespaceContent(List<Token> tokens, ref int index, ModuleNode module)
        {
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var classNode = new ClassNode(module);
                classNode.visibility = ParseVisibility(tokens, ref index);

                var attrs = ParseOptionals(tokens, ref index, new HashSet<string>() { "abstract", "static" });

                classNode.isAbstract = attrs.Contains("abstract");
                classNode.isStatic = attrs.Contains("static");

                ExpectKeyword(tokens, ref index, "class");

                classNode.name = ExpectIdentifier(tokens, ref index, false);

                if (tokens[index].text == ":")
                {
                    index++;
                    classNode.parent = ExpectIdentifier(tokens, ref index, true);
                }

                ExpectDelimiter(tokens, ref index, "{");
                ParseClassContent(tokens, ref index, classNode);
                ExpectDelimiter(tokens, ref index, "}");

            } while (tokens[index].text != "}");
        }

        private void ParseClassContent(List<Token> tokens, ref int index, ClassNode classNode)
        {
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var visibility = ParseVisibility(tokens, ref index);

                var attrs = ParseOptionals(tokens, ref index, new HashSet<string>() { "abstract", "static", "virtual" });

                var method = new MethodNode(classNode);

                method.returnType = ExpectIdentifier(tokens, ref index, true);
                method.visibility = visibility;

                method.name = ExpectIdentifier(tokens, ref index, false);

                if (ExpectOptional(tokens, ref index, ":"))
                {
                    index++;
                    classNode.parent = ExpectIdentifier(tokens, ref index, true);
                }

                ExpectDelimiter(tokens, ref index, "(");
                ParseMethodArguments(tokens, ref index, method);
                ExpectDelimiter(tokens, ref index, ")");

                if (method.isAbstract)
                {
                    method.body = null;
                    ExpectDelimiter(tokens, ref index, ";");
                }
                else
                {
                    method.body = ParseStatement(tokens, ref index, method);
                }

            } while (tokens[index].text != "}");

        }

        private void ParseMethodArguments(List<Token> tokens, ref int index, MethodNode method)
        {
            int count = 0;
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                if (count > 0)
                {
                    ExpectDelimiter(tokens, ref index, ",");
                }

                var arg = new ArgumentNode(method);

                var decl = new DeclarationNode(arg);
                decl.typeName = ExpectIdentifier(tokens, ref index, true);
                decl.identifier = ExpectIdentifier(tokens, ref index, false);

                count++;
            } while (tokens[index].text != ")");
        }
    }
}
