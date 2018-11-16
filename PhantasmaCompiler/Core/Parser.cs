using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Numerics;

namespace Phantasma.CodeGen.Core
{
    public class ParserException: Exception
    {
        public enum Kind
        {
            EndOfStream,
            UnexpectedToken,
            ExpectedToken,
            ExpectedIdentifier,
            ExpectedLiteral,
            ExpectedOperator,
            ExpectedKeyword,
            DuplicatedLabel
        }

        public readonly Token token;
        public readonly Kind kind;

        public ParserException(Token token, Kind kind)
        {
            this.token = token;
            this.kind = kind;
        }
    }

    public static class Parser
    {
        private static HashSet<string> ParseOptionals(List<Token> tokens, ref int index, HashSet<string> keywords)
        {
            var result = new HashSet<string>();
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var token = tokens[index];

                if (keywords.Contains(token.text))
                {
                    result.Add(token.text);
                    index++;
                }
                else
                {
                    return result;
                }
            } while (true);
        }

        private static bool ExpectOptional(this List<Token> tokens, ref int index, string value)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            var token = tokens[index];
            if (token.text == value)
            {
                index++;
                return true;
            }

            return false;
        }

        private static bool ExpectOptional(this List<Token> tokens, ref int index, Token.Kind kind)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            var token = tokens[index];
            if (token.kind == kind)
            {
                index++;
                return true;
            }

            return false;
        }

        private static void ExpectDelimiter(this List<Token> tokens, ref int index, string value)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            var token = tokens[index];
            if (token.kind != Token.Kind.Delimiter || token.text != value)
            {
                throw new ParserException(token, ParserException.Kind.ExpectedToken);
            }

            index++;
        }

        private static void ExpectKeyword(this List<Token> tokens, ref int index, string value)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            var token = tokens[index];
            if (token.kind != Token.Kind.Keyword || token.text != value)
            {
                throw new ParserException(token, ParserException.Kind.ExpectedKeyword);
            }

            index++;
        }

        private static string ExpectValue(this List<Token> tokens, ref int index, Token.Kind kind, ParserException.Kind exception)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            var token = tokens[index];
            if (token.kind != kind)
            {
                throw new ParserException(token, exception);
            }

            index++;
            return token.text;
        }

        private static string ExpectIdentifier(this List<Token> tokens, ref int index, bool allowPath)
        {
            var result = tokens.ExpectValue(ref index, Token.Kind.Identifier, ParserException.Kind.ExpectedIdentifier);

            if (!allowPath && result.Contains("."))
            {
                throw new ParserException(tokens[index - 1], ParserException.Kind.ExpectedIdentifier);
            }

            return result;
        }

        private static object ExpectLiteral(this List<Token> tokens, ref int index, out LiteralKind kind)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            var token = tokens[index];
            index++;

            switch (token.kind)
            {
                case Token.Kind.Integer:
                    {
                        kind = LiteralKind.Integer;
                        var val = BigInteger.Parse(token.text);
                        return val;
                    }

                case Token.Kind.Float:
                    {
                        kind = LiteralKind.Float;
                        var val = decimal.Parse(token.text);
                        return val;
                    }

                case Token.Kind.Boolean:
                    {
                        kind = LiteralKind.Integer;
                        var val = token.text.ToLower() == "true";
                        return val;
                    }

                case Token.Kind.String:
                    {
                        kind = LiteralKind.String;
                        return token.text;
                    }

                default:
                    {
                        throw new ParserException(token, ParserException.Kind.ExpectedLiteral);
                    }
            }
               
        }

        private static string ExpectOperator(this List<Token> tokens, ref int index)
        {
            return tokens.ExpectValue(ref index, Token.Kind.Operator, ParserException.Kind.ExpectedOperator);
        }

        public static ModuleNode Execute(List<Token> tokens)
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
                    node.reference = tokens.ExpectIdentifier(ref index, true);
                    tokens.ExpectDelimiter(ref index, ";");
                }
                else
                if (token.text == "namespace")
                {
                    var namespaceID = tokens.ExpectIdentifier(ref index, true);

                    tokens.ExpectDelimiter(ref index, "{");

                    ParseNamespaceContent(tokens, ref index, module);

                    tokens.ExpectDelimiter(ref index, "}");
                }
                else
                {
                    throw new ParserException(token, ParserException.Kind.UnexpectedToken);
                }

            }

            return module;
        }

        private static Visibility ParseVisibility(this List<Token> tokens, ref int index, Visibility defaultVisibility = Visibility.Internal)
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

        private static void ParseNamespaceContent(List<Token> tokens, ref int index, ModuleNode module)
        {
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var classNode = new ClassNode(module);
                classNode.visibility = ParseVisibility(tokens, ref index);

                var attrs = ParseOptionals(tokens, ref index, new HashSet<string>() { "abstract", "static" });

                classNode.isAbstract = attrs.Contains("abstract");
                classNode.isStatic = attrs.Contains("static");

                tokens.ExpectKeyword(ref index, "class");

                classNode.name = tokens.ExpectIdentifier(ref index, false);

                if (tokens[index].text == ":")
                {
                    index++;
                    classNode.parent = tokens.ExpectIdentifier(ref index, true);
                }

                tokens.ExpectDelimiter(ref index, "{");
                ParseClassContent(tokens, ref index, classNode);
                tokens.ExpectDelimiter(ref index, "}");

            } while (tokens[index].text != "}");
        }

        private static void ParseClassContent(List<Token> tokens, ref int index, ClassNode classNode)
        {
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var visibility = ParseVisibility(tokens, ref index);

                var attrs = ParseOptionals(tokens, ref index, new HashSet<string>() { "abstract", "static", "virtual" });

                var method = new MethodNode(classNode);

                method.returnType = tokens.ExpectIdentifier(ref index, true);
                method.visibility = visibility;

                method.name = tokens.ExpectIdentifier(ref index, false);

                if (tokens.ExpectOptional(ref index, ":"))
                {
                    index++;
                    classNode.parent = tokens.ExpectIdentifier(ref index, true);
                }

                tokens.ExpectDelimiter(ref index, "(");
                ParseMethodArguments(tokens, ref index, method);
                tokens.ExpectDelimiter(ref index, ")");

                if (method.isAbstract)
                {
                    method.body = null;
                    tokens.ExpectDelimiter(ref index, ";");
                }
                else
                {
                    method.body = ParseStatement(tokens, ref index, method);
                }

            } while (tokens[index].text != "}");

        }

        private static void ParseMethodArguments(List<Token> tokens, ref int index, MethodNode method)
        {
            int count = 0;
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                if (count > 0)
                {
                    tokens.ExpectDelimiter(ref index, ",");
                }

                var arg = new ArgumentNode(method);

                var decl = new DeclarationNode(arg);
                decl.typeName = tokens.ExpectIdentifier(ref index, true);
                decl.identifier = tokens.ExpectIdentifier(ref index, false);

                count++;
            } while (tokens[index].text != ")");
        }

        private static StatementNode ParseStatement(List<Token> tokens, ref int index, CompilerNode owner)
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
                    decl.identifier = tokens.ExpectIdentifier(ref index, false);

                    if (tokens.ExpectOptional(ref index, "="))
                    {
                        var node = new AssignmentNode(owner);
                        node.identifier = decl.identifier;
                        node.expr = ParseExpression(tokens, ref index, block);
                        statement = node;
                    }

                    tokens.ExpectDelimiter(ref index, ";");
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

                                tokens.ExpectDelimiter(ref index, ";");

                                statement = node;
                                break;
                            }

                        case "if":
                            {
                                index++;

                                tokens.ExpectDelimiter(ref index, "(");

                                var node = new IfNode(block);
                                node.expr = ParseExpression(tokens, ref index, block);

                                tokens.ExpectDelimiter(ref index, ")");

                                node.trueBranch = ParseStatement(tokens, ref index, block);

                                if (tokens.ExpectOptional(ref index, "else"))
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

                                tokens.ExpectDelimiter(ref index, "(");
                                node.expr = ParseExpression(tokens, ref index, node);
                                tokens.ExpectDelimiter(ref index, ")");

                                tokens.ExpectDelimiter(ref index, "{");

                                var keys = new HashSet<string>();
                                do
                                {
                                    if (tokens[index].text == "}")
                                    {
                                        break;
                                    }


                                    if (tokens.ExpectOptional(ref index, "default"))
                                    {
                                        tokens.ExpectDelimiter(ref index, ":");
                                        var st = ParseStatement(tokens, ref index, node);
                                        node.defaultBranch = st;
                                    }
                                    else
                                    {
                                        tokens.ExpectKeyword(ref index, "case");

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

                                        tokens.ExpectDelimiter(ref index, ":");
                                        var st = ParseStatement(tokens, ref index, node);
                                        node.cases[lit] = st;
                                        keys.Add(key);
                                    }
                                }
                                while (true);

                                tokens.ExpectDelimiter(ref index, "}");
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

        private static int GetOperatorPrecedence(string op)
        {
            switch (op)
            {
                case "||":
                case "&&":
                    return 0;

                case "==":
                case "!=":
                    return 1;

                case "<":
                case ">":
                case "<=":
                case ">=":
                    return 2;

                case "<<":
                case ">>":
                    return 3;

                case "+":
                case "-":
                    return 4;

                case "*":
                case "/":
                case "%":
                    return 5;

                case "!":
                    return 6;

                default: throw new Exception("Invalid operator");
            }
        }

        private static ExpressionNode ParseExpression(List<Token> tokens, ref int index, CompilerNode owner, int precedence = -1)
        {
            if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

            ExpressionNode term;

            if (tokens[index].text == "(")
            {
                index++;
                term = ParseExpression(tokens, ref index, owner);
                tokens.ExpectDelimiter(ref index, ")");
            }
            else
            if (Lexer.IsLiteral(tokens[index].kind))
            {
                LiteralKind litKind;

                var node = new LiteralExpressionNode(owner);
                node.value = tokens.ExpectLiteral(ref index, out litKind);
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
                node.identifier = tokens.ExpectIdentifier(ref index, false);
                term = node;
            }


            while (tokens[index].kind == Token.Kind.Operator)
            {
                var p = GetOperatorPrecedence(tokens[index].text);

                if (precedence < 0 || p > precedence)
                {
                    var expr = new BinaryExpressionNode(owner);

                    expr.left = term;

                    expr.op = tokens.ExpectOperator(ref index);

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
