using System;
using System.Collections.Generic;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class ModuleNode : CompilerNode
    {
        public List<ImportNode> imports = new List<ImportNode>();
        public List<ClassNode> classes = new List<ClassNode>();

        public ModuleNode() : base(null)
        {
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);

            foreach (var import in imports) import.Visit(visitor, level + 1);
            foreach (var @class in classes) @class.Visit(visitor, level + 1);
        }
    }
}