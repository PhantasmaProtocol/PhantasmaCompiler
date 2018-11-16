using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core
{
    public abstract class DefaultParser: Parser
    {
        protected abstract bool IsValidType(string token);

        protected virtual int GetOperatorPrecedence(string op)
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

        protected StatementNode ParseStatement(List<Token> tokens, ref int index, CompilerNode owner)
        {
            BlockNode block = null;
            do
            {
                if (index >= tokens.Count) throw new ParserException(tokens.Last(), ParserException.Kind.EndOfStream);

                var token = tokens[index];

                StatementNode statement = null;

                if (IsValidType(token.text))
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

                                BlockNode parent = (block != null) ? block : owner as BlockNode;
                                if (parent == null)
                                {
                                    throw new ParserException(token, ParserException.Kind.UnexpectedToken);
                                }

                                var node = new IfNode(parent);
                                node.expr = ParseExpression(tokens, ref index, parent);

                                ExpectDelimiter(tokens, ref index, ")");

                                node.trueBranch = ParseStatement(tokens, ref index, parent);

                                if (ExpectOptional(tokens, ref index, "else"))
                                {
                                    node.falseBranch = ParseStatement(tokens, ref index, parent);
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

        protected ExpressionNode ParseExpression(List<Token> tokens, ref int index, CompilerNode owner, int precedence = -1)
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
