using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class UnaryExpressionNode : ExpressionNode
    {
        public string op;
        public ExpressionNode term;

        public UnaryExpressionNode(CompilerNode owner) : base(owner)
        {
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.op;
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            term.Visit(visitor, level + 1);
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            Instruction.Opcode opcode;
            switch (this.op)
            {
                case "+": opcode = Instruction.Opcode.Add; break;
                case "-": opcode = Instruction.Opcode.Sub; break;
                case "!": opcode = Instruction.Opcode.Not; break;
                case "++": opcode = Instruction.Opcode.Inc; break;
                case "--": opcode = Instruction.Opcode.Dec; break;
                default: throw new ArgumentException("Invalid opcode: " + op);
            }

            var temp = this.term.Emit(compiler);
            temp.Add(new Instruction() { source = this, target = compiler.AllocRegister(), a = temp.Last(), op = opcode });
            return temp;
        }
    }
}