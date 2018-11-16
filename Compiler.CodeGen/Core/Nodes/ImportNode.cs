namespace Phantasma.CodeGen.Core.Nodes
{
    public class ImportNode : CompilerNode
    {
        public string reference;

        public ImportNode(ModuleNode owner) : base(owner)
        {
            owner.imports.Add(this);
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.reference;
        }
    }
}