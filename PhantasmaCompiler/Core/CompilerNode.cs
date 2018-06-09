using System;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.CodeGen.Core
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

    public class ClassNode : CompilerNode
    {
        public string name;
        public string parent;
        public bool isAbstract;
        public bool isStatic;
        public Visibility visibility;

        public List<MethodNode> methods = new List<MethodNode>();

        public ClassNode(ModuleNode owner) : base(owner)
        {
            owner.classes.Add(this);
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);

            foreach (var method in methods) method.Visit(visitor, level + 1);
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.name;
        }
    }

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

    }

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

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            decl.Visit(visitor, level + 1);
        }
    }

    #region STATEMENT
    public abstract class StatementNode : CompilerNode
    {
        public StatementNode(CompilerNode owner) : base(owner)
        {
        }

        public abstract List<Instruction> Emit(Compiler compiler);
    }

    public class AssignmentNode : StatementNode
    {
        public string identifier;
        public ExpressionNode expr;

        public AssignmentNode(CompilerNode owner) : base(owner)
        {
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            expr.Visit(visitor, level + 1);
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.identifier;
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = expr.Emit(compiler);
            temp.Add(new Instruction() { source = this, name = this.identifier, a = temp.Last(), op = Instruction.Opcode.Assign});
            return temp;
        }
    }

    public class ReturnNode : StatementNode
    {
        public ExpressionNode expr;

        public ReturnNode(CompilerNode owner) : base(owner)
        {
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = expr.Emit(compiler);
            temp.Add(new Instruction() { source = this, name = "ret", a = temp.Last(), op = Instruction.Opcode.Return });
            return temp;
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            expr.Visit(visitor, level + 1);
        }
    }

    public class IfNode : StatementNode
    {
        public ExpressionNode expr;
        public StatementNode trueBranch;
        public StatementNode falseBranch;

        public IfNode(BlockNode owner) : base(owner)
        {
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            expr.Visit(visitor, level + 1);
            trueBranch.Visit(visitor, level + 1);
            if (falseBranch != null) falseBranch.Visit(visitor, level + 1);
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = this.expr.Emit(compiler);

            var first = this.trueBranch.Emit(compiler);
            Instruction end = new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.Label };
            Instruction middle = null;

            if (falseBranch != null)
            {
                middle = new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.Label };
                var second = this.falseBranch.Emit(compiler);

                temp.Add(new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.JumpIfTrue, a = first.Last(), b = middle });
                temp.AddRange(second);

                temp.Add(new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.Jump, b = end});
                temp.Add(middle);
                temp.AddRange(first);
            }
            else
            {
                temp.Add(new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.JumpIfFalse, a = first.Last(), b = end });
                temp.AddRange(first);
            }

            temp.Add(end);

            return temp;
        }
    }

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
            Instruction end = new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.Label };

            Instruction next = new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.Label };

            var first = this.expr.Emit(compiler);
            temp.AddRange(first);

            var id = new Instruction() { source = this, name = compiler.AllocRegister(), op = Instruction.Opcode.Assign, a = first.Last()};
            temp.Add(id);

            foreach (var entry in cases)
            {
                var lit = new Instruction() { source = this, name = compiler.AllocRegister(), op = Instruction.Opcode.Assign, literal = entry.Key };
                temp.Add(lit);

                var cmp = new Instruction() { source = this, name = compiler.AllocRegister(), op = Instruction.Opcode.Equals, a = id, b = lit };
                temp.Add(cmp);
                temp.Add(new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.JumpIfFalse, a = cmp, b = next });
                var body = entry.Value.Emit(compiler);
                temp.AddRange(body);
                temp.Add(new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.Jump, b = end });
                temp.Add(next);
                next = new Instruction() { source = this, name = compiler.AllocLabel(), op = Instruction.Opcode.Label };
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

    public class WhileNode : StatementNode
    {
        public ExpressionNode expr;
        public StatementNode body;

        public WhileNode(BlockNode owner) : base(owner)
        {
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            expr.Visit(visitor, level + 1);
            body.Visit(visitor, level + 1);
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region EXPRESSIONS
    public abstract class ExpressionNode : CompilerNode
    {
        public ExpressionNode(CompilerNode owner) : base(owner)
        {
        }

        public abstract List<Instruction> Emit(Compiler compiler);
    }

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

            var temp = new List<Instruction>();
            temp.Add(new Instruction() { source = this, name = compiler.AllocRegister(), variable = this.decl, op = Instruction.Opcode.Assign});
            return temp;
        }
    }

    public class LiteralExpressionNode : ExpressionNode
    {
        public object value;
        public LiteralKind kind;

        public LiteralExpressionNode(CompilerNode owner) : base(owner)
        {
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.value;
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var temp = new List<Instruction>();
            temp.Add(new Instruction() { source = this, name = compiler.AllocRegister(), literal = this, op = Instruction.Opcode.Assign });
            return temp;
        }
    }

    public class UnaryExpressionNode : ExpressionNode
    {
        public string op;
        public ExpressionNode term;

        public UnaryExpressionNode(CompilerNode owner) : base(owner)
        {
        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.op;
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            term.Visit(visitor, level + 1);
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            Instruction.Opcode opcode;
            switch (this.op)
            {
                case "+": opcode = Instruction.Opcode.Add; break;
                case "-": opcode = Instruction.Opcode.Sub; break;
                case "!": opcode = Instruction.Opcode.Not; break;
                case "++": opcode = Instruction.Opcode.Inc; break;
                case "--": opcode = Instruction.Opcode.Dec; break;
                default: throw new ArgumentException("Invalid opcode: " + op);
            }

            var temp = this.term.Emit(compiler);
            temp.Add(new Instruction() { source = this, name = compiler.AllocRegister(), a = temp.Last(), op = opcode });
            return temp;
        }
    }

    public class BinaryExpressionNode : ExpressionNode
    {
        public string op;
        public ExpressionNode left;
        public ExpressionNode right;

        public BinaryExpressionNode(CompilerNode owner) : base(owner)
        {

        }

        public override string ToString()
        {
            return base.ToString() + "=>" + this.op;
        }

        public override void Visit(Action<CompilerNode, int> visitor, int level = 0)
        {
            base.Visit(visitor, level);
            left.Visit(visitor, level + 1);
            right.Visit(visitor, level + 1);
        }

        public override List<Instruction> Emit(Compiler compiler)
        {
            var left = this.left.Emit(compiler);
            var right = this.right.Emit(compiler);

            Instruction.Opcode opcode;
            switch (this.op)
            {
                case "+": opcode = Instruction.Opcode.Add; break;
                case "-": opcode = Instruction.Opcode.Sub; break;
                case "*": opcode = Instruction.Opcode.Mul; break;
                case "/": opcode = Instruction.Opcode.Div; break;
                case "%": opcode = Instruction.Opcode.Mod; break;
                default: throw new ArgumentException("Invalid opcode: " + op);
            }

            var temp = new List<Instruction>();
            temp.AddRange(left);
            temp.AddRange(right);
            temp.Add(new Instruction() { source = this, name = compiler.AllocRegister(), a = left.Last(), op = opcode, b = right.Last() });

            return temp;
        }
    }

    #endregion

}