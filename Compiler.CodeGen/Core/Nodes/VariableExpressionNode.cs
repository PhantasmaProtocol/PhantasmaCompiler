using System.Collections.Generic;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class VariableExpressionNode : ExpressionNode
    {
        public string identifier;

        public DeclarationNode decl; // can resolved later

        public VariableExpressionNode(CompilerNode owner) : base(owner)
        {
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.identifier;
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            if (this.decl == null)
            {
                this.decl = ResolveIdentifier(this.identifier);
            }

            var varLocation = compiler.varMap[this.decl.identifier];

            var temp = new List<Instruction>();
            temp.Add(new Instruction() { source = this, target = compiler.AllocRegister(), varName = varLocation, op = Instruction.Opcode.Assign});
            return temp;
        }
    }
}