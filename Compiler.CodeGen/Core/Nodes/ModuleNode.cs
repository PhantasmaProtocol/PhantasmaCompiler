using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class ModuleNode : CompilerNode
    {
        public List<ImportNode> imports = new List<ImportNode>();
        public List<ClassNode> classes = new List<ClassNode>();

        public ModuleNode() : base(null)
        {
        }

        public override IEnumerable<CompilerNode> Nodes =>  imports.Concat<CompilerNode>(classes);
    }
}