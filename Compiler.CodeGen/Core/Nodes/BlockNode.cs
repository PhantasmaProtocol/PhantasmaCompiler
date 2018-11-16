using System;
using System.Collections.Generic;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class BlockNode : StatementNode
    {
        public List<DeclarationNode> declarations = new List<DeclarationNode>();
        public List<StatementNode> statements = new List<StatementNode>();

        public BlockNode(CompilerNode owner) : base(owner)
        {
        }

        public override DeclarationNode ResolveIdentifier(string identifier)
        {
            foreach (var decl in declarations)
            {
                if (decl.identifier == identifier)
                {
                    return decl;
                }
            }

            return base.ResolveIdentifier(identifier);
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var list = new List<Instruction>();

            foreach (var st in statements)
            {
                var temp = st.Emit(compiler);
                list.AddRange(temp);
            }
            return list;
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);

            foreach (var decl in declarations) decl.Visit(visitor, level + 1);
            foreach (var st in statements) st.Visit(visitor, level + 1);
        }

    }
}