using System.Collections.Generic;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class LiteralExpressionNode : ExpressionNode
    {
        public object value;
        public LiteralKind kind;

        public LiteralExpressionNode(CompilerNode owner) : base(owner)
        {
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.value;
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = new List<Instruction>();
            temp.Add(new Instruction() { source = this, target = compiler.AllocRegister(), literal = this, op = Instruction.Opcode.Assign });
            return temp;
        }
    }
}