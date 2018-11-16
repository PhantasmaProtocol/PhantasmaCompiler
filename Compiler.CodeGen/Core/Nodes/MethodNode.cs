using System;
using System.Collections.Generic;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class MethodNode : CompilerNode
    {
        public string name;
        public bool isStatic;
        public bool isAbstract;
        public bool isVirtual;
        public Visibility visibility;

        public string returnType;

        public List<ArgumentNode> arguments = new List<ArgumentNode>();

        public StatementNode body;

        public MethodNode(ClassNode owner) : base(owner)
        {
            owner.methods.Add(this);
        }

        public override DeclarationNode ResolveIdentifier(string identifier)
        {
            foreach (var arg in arguments)
            {
                if (arg.decl.identifier == identifier)
                {
                    return arg.decl;
                }
            }

            return base.ResolveIdentifier(identifier);
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);

            foreach (var arg in arguments) arg.Visit(visitor, level + 1);
            body.Visit(visitor, level + 1);
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.name;
        }

        public List<Instruction> Emit(Compiler compiler)
        {
            var result = new List<Instruction>();

            foreach (var arg in arguments)
            {
                var reg = compiler.AllocRegister();
                compiler.varMap[arg.decl.identifier] = reg;
                result.Add(new Instruction() { source = this, target = reg, op = Instruction.Opcode.Pop });
            }

            var temp = body.Emit(compiler);
            result.AddRange(temp);
            return result;
        }
    }
}