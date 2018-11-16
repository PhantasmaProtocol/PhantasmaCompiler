using Phantasma.CodeGen.Core.Nodes;
using System.Collections.Generic;

namespace Phantasma.CodeGen.Core
{
    public class Instruction
    {
        public enum Opcode
        {
            Label,
            Return,
            Assign,
            Pop,
            Push,
            Add,
            Sub,
            Mul,
            Div,
            Mod,
            Shr,
            Shl,
            Negate,
            Equals,
            LessThan,
            GreaterThan,
            LessOrEqualThan,
            GreaterOrEqualThan,
            Or,
            And,
            Not,
            Inc,
            Dec,
            Jump,
            JumpIfTrue,
            JumpIfFalse
        }

        public CompilerNode source;
        public string target;
        public LiteralExpressionNode literal;
        public string varName; // HACK: Fix me later
        public Instruction a;
        public Instruction b;
        public Opcode op;

        public override string ToString()
        {
            string s = target;

            if (op == Opcode.Label)
            {
                return "@"+s;
            }

            if (op == Opcode.Jump)
            {
                return $"goto {b}";
            }

            if (op == Opcode.JumpIfFalse)
            {
                return $"if !{a.target} goto {b}";
            }

            if (op == Opcode.JumpIfTrue)
            {
                return $"if {a.target} goto {b}";
            }

            if (op == Opcode.Pop)
            {
                return $"pop {target}";
            }

            if (op == Opcode.Push)
            {
                return $"push {target}";
            }

            if (op == Opcode.Return)
            {
                return $"ret {a.target}";
            }

            if (op == Opcode.Assign && literal != null)
            {
                if (literal.kind == LiteralKind.String)
                {
                    return s + $" := \"{literal.value}\"";
                }
                return s + $" := {literal.value}";
            }

            if (op == Opcode.Assign && varName != null)
            {
                return s + $" := {varName}";
            }

            string symbol;
            switch (op)
            {
                case Opcode.Add: symbol = "+"; break;
                case Opcode.Sub: symbol = "-"; break;
                case Opcode.Mul: symbol = "*"; break;
                case Opcode.Div: symbol = "/"; break;
                case Opcode.Mod: symbol = "%"; break;
                case Opcode.Not: symbol = "!"; break;
                case Opcode.Inc: symbol = "++"; break;
                case Opcode.Dec: symbol = "--"; break;
                case Opcode.Equals: symbol = "=="; break;
                case Opcode.LessOrEqualThan: symbol = "<="; break;
                case Opcode.GreaterOrEqualThan: symbol = ">="; break;
                case Opcode.LessThan: symbol = "<"; break;
                case Opcode.GreaterThan: symbol = ">"; break;
                default: symbol = null; break;
            }

            if (b != null)
            {
                s += $" := {a.target} {symbol} {b.target}";
            }
            else
            if (a != null)
            {
                if (op == Opcode.Assign)
                {
                    s += $" := {a.target}";
                }
                else
                if (symbol != null)
                {
                    if (symbol == "-")
                    {
                        op = Opcode.Negate;
                    }

                    s += $" := {symbol}{a.target}";
                }
                else
                {
                    s += $" := {op}{a.target}";
                }
            }
            else
            {
                s += $" := {symbol}()";
            }

            return s;
        }
    }

    public class Compiler
    {
        private int registerIndex;
        private int labelIndex;

        public Dictionary<string, string> varMap = new Dictionary<string, string>();

        public string AllocRegister()
        {
            var temp = "t"+registerIndex.ToString();
            registerIndex++;
            return temp;
        }


        public string AllocLabel()
        {
            var temp = "p"+labelIndex.ToString();
            labelIndex++;
            return temp;
        }

        private void ProcessBlock(List<Instruction> instructions, BlockNode block)
        {
            foreach (var st in block.statements)
            {
                var list = st.Emit(this);

                foreach (var item in list)
                {
                    instructions.Add(item);
                }
            }
        }

        public List<Instruction> Execute(ModuleNode node)
        {
            var instructions = new List<Instruction>();

            labelIndex = 0;
            registerIndex = 0;
            foreach (var entry in node.classes)
            {
                foreach (var method in entry.methods)
                {
                    var temp = method.Emit(this);
                    instructions.AddRange(temp);
                }
            }

            return instructions;
        }
    }
}
