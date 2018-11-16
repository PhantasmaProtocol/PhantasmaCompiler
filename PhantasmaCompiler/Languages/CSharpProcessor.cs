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

    public class CSharpParser: Parser 
    {
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

        private StatementNode ParseStatement(List<Token> tokens, ref int index, CompilerNode owner)
        {
            BlockNode block = null;
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var token = tokens[index];

                StatementNode statement = null;

                if (token.text == "var" /*|| IsValidType(token.text)*/)
                {
                    var decl = new DeclarationNode(block);
                    decl.typeName = token.text;
                    index++;
                    decl.identifier = ExpectIdentifier(tokens, ref index, false);

                    if (ExpectOptional(tokens, ref index, "="))
                    {
                        var node = new AssignmentNode(owner);
                        node.identifier = decl.identifier;
                        node.expr = ParseExpression(tokens, ref index, block);
                        statement = node;
                    }

                    ExpectDelimiter(tokens, ref index, ";");
                }
                else
                    switch (token.text)
                    {
                        case "{":
                            {
                                index++;
                                block = new BlockNode(owner);
                                owner = block;
                                break;
                            }

                        case "return":
                            {
                                index++;

                                var node = new ReturnNode(owner);
                                node.expr = ParseExpression(tokens, ref index, node);

                                ExpectDelimiter(tokens, ref index, ";");

                                statement = node;
                                break;
                            }

                        case "if":
                            {
                                index++;

                                ExpectDelimiter(tokens, ref index, "(");

                                var node = new IfNode(block);
                                node.expr = ParseExpression(tokens, ref index, block);

                                ExpectDelimiter(tokens, ref index, ")");

                                node.trueBranch = ParseStatement(tokens, ref index, block);

                                if (ExpectOptional(tokens, ref index, "else"))
                                {
                                    node.falseBranch = ParseStatement(tokens, ref index, block);
                                }

                                statement = node;
                                break;
                            }

                        case "switch":
                            {
                                index++;

                                var node = new SwitchNode(block);
                                statement = node;

                                ExpectDelimiter(tokens, ref index, "(");
                                node.expr = ParseExpression(tokens, ref index, node);
                                ExpectDelimiter(tokens, ref index, ")");

                                ExpectDelimiter(tokens, ref index, "{");

                                var keys = new HashSet<string>();
                                do
                                {
                                    if (tokens[index].text == "}")
                                    {
                                        break;
                                    }


                                    if (ExpectOptional(tokens, ref index, "default"))
                                    {
                                        ExpectDelimiter(tokens, ref index, ":");
                                        var st = ParseStatement(tokens, ref index, node);
                                        node.defaultBranch = st;
                                    }
                                    else
                                    {
                                        ExpectKeyword(tokens, ref index, "case");

                                        LiteralKind litKind;
                                        var val = ExpectLiteral(tokens, ref index, out litKind);

                                        var key = val.ToString();
                                        if (keys.Contains(key))
                                        {
                                            throw new ParserException(tokens[index], ParserException.Kind.DuplicatedLabel);
                                        }

                                        var lit = new LiteralExpressionNode(node);
                                        lit.kind = litKind;
                                        lit.value = val;

                                        ExpectDelimiter(tokens, ref index, ":");
                                        var st = ParseStatement(tokens, ref index, node);
                                        node.cases[lit] = st;
                                        keys.Add(key);
                                    }
                                }
                                while (true);

                                ExpectDelimiter(tokens, ref index, "}");
                                break;
                            }

                        default:
                            {
                                throw new ParserException(tokens[index], ParserException.Kind.UnexpectedToken);
                            }
                    }

                if (block == null)
                {
                    return statement;
                }
                else
                if (statement != null)
                {
                    block.statements.Add(statement);
                }

            } while (tokens[index].text != "}");

            index++;

            return block;
        }

        private ExpressionNode ParseExpression(List<Token> tokens, ref int index, CompilerNode owner, int precedence = -1)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            ExpressionNode term;

            if (tokens[index].text == "(")
            {
                index++;
                term = ParseExpression(tokens, ref index, owner);
                ExpectDelimiter(tokens, ref index, ")");
            }
            else
            if (Lexer.IsLiteral(tokens[index].kind))
            {
                LiteralKind litKind;

                var node = new LiteralExpressionNode(owner);
                node.value = ExpectLiteral(tokens, ref index, out litKind);
                node.kind = litKind;
                term = node;
            }
            else
            if (tokens[index].kind == Token.Kind.Operator)
            {
                var node = new UnaryExpressionNode(owner);
                node.op = tokens[index].text;
                index++;

                node.term = ParseExpression(tokens, ref index, node);
                term = node;
            }
            else
            {
                var node = new VariableExpressionNode(owner);
                node.identifier = ExpectIdentifier(tokens, ref index, false);
                term = node;
            }

            while (tokens[index].kind == Token.Kind.Operator)
            {
                var p = GetOperatorPrecedence(tokens[index].text);

                if (precedence < 0 || p > precedence)
                {
                    var expr = new BinaryExpressionNode(owner);

                    expr.left = term;

                    expr.op = ExpectOperator(tokens, ref index);

                    expr.right = ParseExpression(tokens, ref index, expr, p);

                    term = expr;
                }
                else
                {
                    break;
                }
            }

            return term;
        }

    }
}
