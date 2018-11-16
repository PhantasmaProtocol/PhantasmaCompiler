using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class CallNode: StatementNode
    {
        public CallNode(CompilerNode owner) : base(owner)
        {
        }

        public override IEnumerable<CompilerNode> Nodes => Enumerable.Empty<CompilerNode>();

        public override List<Instruction> Emit(Compiler compiler)
        {
            throw new System.NotImplementedException();
        }
    }
}
