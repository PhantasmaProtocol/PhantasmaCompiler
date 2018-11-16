using System;
using System.Collections.Generic;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class WhileNode : StatementNode
    {
        public ExpressionNode expr;
        public StatementNode body;

        public WhileNode(BlockNode owner) : base(owner)
        {
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            expr.Visit(visitor, level + 1);
            body.Visit(visitor, level + 1);
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            throw new NotImplementedException();
        }
    }
}