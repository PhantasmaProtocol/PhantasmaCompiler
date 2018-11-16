using System.Collections.Generic;

namespace Phantasma.CodeGen.Core.Nodes
{
    public abstract class StatementNode : CompilerNode
    {
        public StatementNode(CompilerNode owner) : base(owner)
        {
        }

        public abstract List<Instruction> Emit(Compiler compiler);
    }
}