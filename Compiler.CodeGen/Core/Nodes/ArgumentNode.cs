using System;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class ArgumentNode : CompilerNode
    {
        public DeclarationNode decl;

        public ArgumentNode(MethodNode owner) : base(owner)
        {
            owner.arguments.Add(this);
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.decl.ToString();
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            decl.Visit(visitor, level + 1);
        }
    }
}