using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.CodeGen.Core;

namespace Phantasma.CodeGen.Languages
{
    public class SolidityProcessor : LanguageProcessor
    {
        protected string[] _keywords = new string[]{
            "return",  "public", "private", "external", "internal", "pure", "view", "payable", "constant", "anonymous", "indexed",
            "pragma", "solidity", "contract", "function", "struct", "if", "else", "while", "do", "returns"
        };

        public override Lexer Lexer => _lexer;
        public override Parser Parser => _parser;
        public override string Description => "Solidity";

        private Lexer _lexer;
        private Parser _parser;

        public SolidityProcessor()
        {
            _lexer = new DefaultLexer(_keywords);
            _parser = new SolidityParser();
        }
    }

    public class SolidityParser : Parser
    {
        public override ModuleNode Execute(List<Token> tokens)
        {
            int index = 0;

            var module = new ModuleNode();
            while (index < tokens.Count)
            {
                var token = tokens[index];
                index++;

                if (token.text == "pragma")
                {
                    ExpectKeyword(tokens, ref index, "solidity");
                    ExpectOperator(tokens, ref index);
                    ExpectValue(tokens, ref index, Token.Kind.Invalid, ParserException.Kind.UnexpectedToken); // TODO this is a hack
                    ExpectDelimiter(tokens, ref index, ";");
                }
                else
                if (token.text == "contract")
                {
                    var name = ExpectIdentifier(tokens, ref index, true);

                    ExpectDelimiter(tokens, ref index, "{");

                    ParseContractContent(tokens, ref index, name, module);

                    ExpectDelimiter(tokens, ref index, "}");
                }
                else
                {
                    throw new ParserException(token, ParserException.Kind.UnexpectedToken);
                }

            }

            return module;
        }

        private void ParseContractContent(List<Token> tokens, ref int index, string name, ModuleNode module)
        {
            var classNode = new ClassNode(module);
            classNode.name = name;
            classNode.visibility = Visibility.Public;
            classNode.isAbstract = false;
            classNode.isStatic = false;

            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var token = tokens[index];

                if (token.text == "function")
                {
                    index++;
                    ParseMethodContent(tokens, ref index, classNode, module);
                }
                else
                {
                    throw new ParserException(token, ParserException.Kind.UnexpectedToken);
                }

            } while (tokens[index].text != "}");

        }

        private void ParseMethodContent(List<Token> tokens, ref int index, ClassNode classNode, ModuleNode module)
        {
            var method = new MethodNode(classNode);

            method.name = ExpectIdentifier(tokens, ref index, false);

            ExpectDelimiter(tokens, ref index, "(");
            ParseMethodArguments(tokens, ref index, method);
            ExpectDelimiter(tokens, ref index, ")");

            var attrs = ParseOptionals(tokens, ref index, new HashSet<string>() { "public", "private", "internal", "external", "pure", "constant", "view" });

            if (tokens[index].text == "returns")
            {
                index++;

                ExpectDelimiter(tokens, ref index, "(");
                method.returnType = ExpectIdentifier(tokens, ref index, true);
                ExpectDelimiter(tokens, ref index, ")");
            }
            else
            {
                method.returnType = "void";
            }


            if (attrs.Contains("private"))
            {
                method.visibility = Visibility.Private;
            }
            else
            if (attrs.Contains("internal"))
            {
                method.visibility = Visibility.Internal;
            }
            else
            {
                method.visibility = Visibility.Public;
            }

            method.body = ParseStatement(tokens, ref index, method);
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
