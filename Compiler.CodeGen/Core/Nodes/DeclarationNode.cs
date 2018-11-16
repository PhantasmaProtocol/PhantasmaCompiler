using System;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class DeclarationNode : CompilerNode
    {
        public string identifier;
        public string typeName;

        public DeclarationNode(CompilerNode owner) : base(owner)
        {
            if (owner is BlockNode)
            {
                ((BlockNode)owner).declarations.Add(this);
            }
            else
            if (owner is ArgumentNode)
            {
                ((ArgumentNode)owner).decl = this;
            }
            else
            {
                throw new Exception("Invalid owner");
            }
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.identifier+"/"+this.typeName;
        }

    }
}