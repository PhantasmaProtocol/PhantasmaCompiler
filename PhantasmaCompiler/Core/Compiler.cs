using System.Collections.Generic;

namespace Phantasma.Codegen.Core
{
    public class Instruction
    {
        public enum Opcode
        {
            Label,
            Return,
            Assign,
            Add,
            Sub,
            Mul,
            Div,
            Mod,
            Shr,
            Shl,
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
        public string name;
        public LiteralExpressionNode literal;
        public DeclarationNode variable;
        public Instruction a;
        public Instruction b;
        public Opcode op;

        public override string ToString()
        {
            string s = name;

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
                return $"if !{a.name} goto {b}";
            }

            if (op == Opcode.JumpIfTrue)
            {
                return $"if {a.name} goto {b}";
            }

            if (op == Opcode.Return)
            {
                return $"ret {a.name}";
            }

            if (op == Opcode.Assign && literal != null)
            {
                if (literal.kind == LiteralKind.String)
                {
                    return s + $" := \"{literal.value}\"";
                }
                return s + $" := {literal.value}";
            }

            if (op == Opcode.Assign && variable != null)
            {
                return s + $" := [{variable.identifier}]";
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
                s += $" := {a.name} {symbol} {b.name}";
            }
            else
            if (a != null)
            {
                if (op == Opcode.Assign)
                {
                    s += $" := {a.name}";
                }
                else
                if (symbol != null)
                {
                    s += $" := {symbol}{a.name}";
                }
                else
                {
                    s += $" := {op}{a.name}";
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

        public string AllocRegister()
        {
            registerIndex++;
            return "t"+registerIndex.ToString();
        }


        public string AllocLabel()
        {
            labelIndex++;
            return "p"+labelIndex.ToString();
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
                    var temp = method.body.Emit(this);
                    instructions.AddRange(temp);
                }
            }

            return instructions;
        }
    }
}
