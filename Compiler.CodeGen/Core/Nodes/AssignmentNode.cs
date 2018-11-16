using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class AssignmentNode : StatementNode
    {
        public string identifier;
        public ExpressionNode expr;

        public AssignmentNode(CompilerNode owner) : base(owner)
        {
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            expr.Visit(visitor, level + 1);
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.identifier;
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = expr.Emit(compiler);
            temp.Add(new Instruction() { source = this, target = this.identifier, a = temp.Last(), op = Instruction.Opcode.Assign});
            return temp;
        }
    }
}