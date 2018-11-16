using System;

namespace Phantasma.CodeGen.Core.Nodes
{
    public enum Visibility
    {
        Private,
        Protected,
        Internal,
        Public
    }

    public abstract class CompilerNode
    {
        public readonly CompilerNode Owner;

        public CompilerNode(CompilerNode owner)
        {
            if (owner == null && !(this is ModuleNode))
            {
                throw new Exception("Owner cannot be null");
            }
            this.Owner = owner;
        }

        public virtual void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            visitor(this, level);
        }

        public override string ToString()
        {
            return this.GetType().Name.Replace("Node", "");
        }

        public virtual DeclarationNode ResolveIdentifier(string identifier)
        {
            if (this.Owner != null)
            {
                return this.Owner.ResolveIdentifier(identifier);
            }
            else
            {
                throw new Exception("Identifier could not be resolved: " + identifier);
            }
        }
    }
}