using System;
using System.Collections.Generic;

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

        public override IEnumerable<CompilerNode> Nodes
        {
            get
            {
                yield return decl;
                yield break;
            }
        }
    }
}