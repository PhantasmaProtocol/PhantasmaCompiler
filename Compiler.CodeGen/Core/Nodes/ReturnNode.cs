using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class ReturnNode : StatementNode
    {
        public ExpressionNode expr;

        public ReturnNode(CompilerNode owner) : base(owner)
        {
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = expr.Emit(compiler);
            temp.Add(new Instruction() { source = this, target = "ret", a = temp.Last(), op = Instruction.Opcode.Return });
            return temp;
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            expr.Visit(visitor, level + 1);
        }
    }
}