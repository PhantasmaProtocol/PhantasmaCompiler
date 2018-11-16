using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core.Nodes
{
    public class SwitchNode : StatementNode
    {
        public ExpressionNode expr;
        public Dictionary<LiteralExpressionNode, StatementNode> cases = new Dictionary<LiteralExpressionNode, StatementNode>();
        public StatementNode defaultBranch;

        public SwitchNode(BlockNode owner) : base(owner)
        {
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);

            expr.Visit(visitor, level + 1);

            foreach (var entry in cases)
            {
                entry.Key.Visit(visitor, level + 2);
                entry.Value.Visit(visitor, level + 2);
            }
            if (defaultBranch != null)
            {
                defaultBranch.Visit(visitor, level + 1);
            }
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = new List<Instruction>();
            Instruction end = new Instruction() { source = this, target = compiler.AllocLabel(), op = Instruction.Opcode.Label };

            Instruction next = new Instruction() { source = this, target = compiler.AllocLabel(), op = Instruction.Opcode.Label };

            var first = this.expr.Emit(compiler);
            temp.AddRange(first);

            var id = new Instruction() { source = this, target = compiler.AllocRegister(), op = Instruction.Opcode.Assign, a = first.Last()};
            temp.Add(id);

            foreach (var entry in cases)
            {
                var lit = new Instruction() { source = this, target = compiler.AllocRegister(), op = Instruction.Opcode.Assign, literal = entry.Key };
                temp.Add(lit);

                var cmp = new Instruction() { source = this, target = compiler.AllocRegister(), op = Instruction.Opcode.Equals, a = id, b = lit };
                temp.Add(cmp);
                temp.Add(new Instruction() { source = this, target = compiler.AllocLabel(), op = Instruction.Opcode.JumpIfFalse, a = cmp, b = next });
                var body = entry.Value.Emit(compiler);
                temp.AddRange(body);
                temp.Add(new Instruction() { source = this, target = compiler.AllocLabel(), op = Instruction.Opcode.Jump, b = end });
                temp.Add(next);
                next = new Instruction() { source = this, target = compiler.AllocLabel(), op = Instruction.Opcode.Label };
            }

            if (defaultBranch != null)
            {
                var body = defaultBranch.Emit(compiler);
                temp.AddRange(body);
            }

            temp.Add(end);
            return temp;
        }
    }
}