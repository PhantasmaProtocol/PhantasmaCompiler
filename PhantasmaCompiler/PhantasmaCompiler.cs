using Phantasma.VM;
using LunarParser;
using LunarParser.JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Phantasma.Codegen.Core;

namespace Phantasma.Codegen
{
    public class PhantasmaInstruction
    {
        public OpCode opcode;
        public byte[] data;
        public string label;
        public uint offset;
        public PhantasmaInstruction target;
        public Instruction source;

        public PhantasmaInstruction(Instruction source, OpCode opcode, byte[] data = null)
        {
            this.source = source;
            this.opcode = opcode;
            this.data = data;
            this.label = null;
        }

        public PhantasmaInstruction(Instruction source, OpCode opcode, string label) : this(source, opcode)
        {
            this.label = label;
        }

        public override string ToString()
        {
            var s = this.opcode.ToString();

            if (this.opcode > OpCode.PUSHBYTES1 && this.opcode < OpCode.PUSHBYTES75)
            {
                int n = 1 + ((int)(this.opcode) - (int)OpCode.PUSHBYTES1);
                s = "PUSHBYTES" + n;
            }
            if (data != null)
            {
                s += " " + data.BytesToHex();
            }
            return s;
        }
    }

    public class PhantasmaCompiler
    {
        private List<string> stack = new List<string>();
        private Dictionary<string, string> registers = new Dictionary<string, string>();
        private HashSet<string> important = new HashSet<string>();

        private ModuleNode tree;

        private List<PhantasmaInstruction> _output = new List<PhantasmaInstruction>();
        public IEnumerable<PhantasmaInstruction> Instructions => _output;

        private byte[] _script;

        public PhantasmaCompiler(ModuleNode tree, List<Instruction> instructions)
        {
            this.tree = tree;

            MethodNode main = FindMethod("Main");

            foreach (var arg in main.arguments)
            {
                stack.Insert(0, arg.decl.identifier);
                important.Add(arg.decl.identifier);
            }
            
            foreach (var i in instructions)
            {
                TranslateInstruction(i);
            }

            var targets = new Dictionary<string, PhantasmaInstruction>();

            int n = 0;
            while (n<_output.Count)
            {
                var i = _output[n]; n++;

                if (i.opcode == OpCode.NOP && i.label != null)
                {
                    PhantasmaInstruction t;

                    if (n < _output.Count)
                    {
                        t = _output[n];
                        _output.RemoveAt(n - 1);
                    }
                    else
                    {
                        t = i;
                    }

                    targets[i.label] = t;
                }
            }

            uint ofs = 0;
            foreach (var i in _output)
            {
                switch (i.opcode)
                {
                    case OpCode.JMP:
                    case OpCode.JMPIF:
                    case OpCode.JMPIFNOT:
                        {
                            i.target = targets[i.label];
                            i.data = new byte[2];
                            break;
                        }
                }

                i.offset = ofs;
                ofs++;
                if (i.data != null)
                {
                    ofs += (uint)i.data.Length;
                }
            }

            foreach (var i in _output)
            {
                switch (i.opcode)
                {
                    case OpCode.JMP:
                    case OpCode.JMPIF:
                    case OpCode.JMPIFNOT:
                        {
                            short offset = (short)(i.target.offset - i.offset);
                            i.data = BitConverter.GetBytes(offset);
                            break;
                        }
                }
            }

            _script = GetScript();
        }

        private byte[] GetScript()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var i in _output)
                    {
                        writer.Write((byte)i.opcode);
                        if (i.data != null)
                        {
                            writer.Write(i.data);
                        }
                    }

                }

                return stream.ToArray();
            }

        }

        private MethodNode FindMethod(string name)
        {
            foreach (var entry in tree.classes)
            {
                foreach (var method in entry.methods)
                {
                    if (method.name == name)
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        private int FindInStack(string name)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                var item = stack[i];
                if (item == name)
                {
                    return (stack.Count - 1) - i;
                }
            }

            return -1;
        }

        private void RequireArg(Instruction i, string reg)
        {
            var name = registers[reg];
            int index = FindInStack(name);

            if (index == 0 && !important.Contains(name))
            {
                return;
            }

            if (index != -1)
            {
                PushNumber(i, index);
                _output.Add(new PhantasmaInstruction(i, OpCode.PICK));

                stack.Add(reg);
            }
            else
            {
                throw new Exception("WHERE? " + reg);
            }
        }

        private void PopStack(int count)
        {
            while (count > 0)
            {
                stack.RemoveAt(stack.Count - 1);
                count--;
            }
        }

        private void PushNumber(Instruction i, BigInteger n)
        {
            if (n == -1)
            {
                _output.Add(new PhantasmaInstruction(i, OpCode.PUSHM1));
            }
            else
            if (n == 0)
            {
                _output.Add(new PhantasmaInstruction(i, OpCode.PUSH0));
            }
            else
            if (n > 0 && n <= 16)
            {
                var op = (OpCode)((int)OpCode.PUSH1 + ((int)n - 1));
                _output.Add(new PhantasmaInstruction(i, op));
            }
            else
            {
                byte[] data = n.ToByteArray();

                var op = (OpCode)((int)OpCode.PUSHBYTES1 + ((int)data.Length - 1));
                _output.Add(new PhantasmaInstruction(i, op, data));
            }            
        }

        private void InsertOp(Instruction i, OpCode opcode)
        {
            if (i.b != null)
            {
                RequireArg(i, i.b.name);
                RequireArg(i, i.a.name);
                _output.Add(new PhantasmaInstruction(i, opcode));
                PopStack(2);
            }
            else
            {
                RequireArg(i, i.a.name);
                _output.Add(new PhantasmaInstruction(i, opcode));
                PopStack(1);
            }
        }

        public void TranslateInstruction(Instruction i)
        {
            switch (i.op)
            {
                case Instruction.Opcode.Label:
                    {
                        _output.Add(new PhantasmaInstruction(i, OpCode.NOP, i.name));
                        break;
                    }

                case Instruction.Opcode.Assign:
                    {
                        if (i.literal != null)
                        {
                            switch (i.literal.kind)
                            {
                                case LiteralKind.String:
                                    {
                                        var val = (string)i.literal.value;
                                        var op = (OpCode)((int)OpCode.PUSHBYTES1 + (val.Length - 1));
                                        _output.Add(new PhantasmaInstruction(i, op, System.Text.Encoding.UTF8.GetBytes(val)));

                                        registers[i.name] = i.name;
                                        stack.Add(i.name);
                                        break;
                                    }

                                case LiteralKind.Boolean:
                                    {
                                        _output.Add(new PhantasmaInstruction(i, ((bool)i.literal.value) ? OpCode.PUSHT : OpCode.PUSHF));

                                        registers[i.name] = i.name;
                                        stack.Add(i.name);
                                        break;
                                        
                                    }
                                case LiteralKind.Integer:
                                    {
                                        BigInteger n = (BigInteger)i.literal.value;
                                        PushNumber(i, n);

                                        registers[i.name] = i.name;
                                        stack.Add(i.name);
                                        break;
                                    }

                                default: throw new Exception("Unsuported " + i.literal.kind);
                            }
                        }
                        else
                        if (i.variable != null)
                        {
                            registers[i.name] = i.variable.identifier;
                        }
                        else
                        {
                            registers[i.name] = registers[i.a.name];
                            //_output.Add(new PhantasmaInstruction(OpCode.NOP));
                        }
                        break;

                    }

                case Instruction.Opcode.Add: { InsertOp(i, OpCode.ADD); break; }
                case Instruction.Opcode.Sub: { InsertOp(i, OpCode.SUB); break; }
                case Instruction.Opcode.Mul: { InsertOp(i, OpCode.MUL); break; }
                case Instruction.Opcode.Div: { InsertOp(i, OpCode.DIV); break; }
                case Instruction.Opcode.Mod: { InsertOp(i, OpCode.MOD); break; }
                case Instruction.Opcode.Shr: { InsertOp(i, OpCode.SHR); break; }
                case Instruction.Opcode.Shl: { InsertOp(i, OpCode.SHL); break; }
                case Instruction.Opcode.Equals: { InsertOp(i, OpCode.EQUAL); break; }
                case Instruction.Opcode.LessThan: { InsertOp(i, OpCode.LT); break; }
                case Instruction.Opcode.GreaterThan: { InsertOp(i, OpCode.GT); break; }
                case Instruction.Opcode.LessOrEqualThan: { InsertOp(i, OpCode.LTE); break; }
                case Instruction.Opcode.GreaterOrEqualThan: { InsertOp(i, OpCode.GTE); break; }

                case Instruction.Opcode.Jump: _output.Add(new PhantasmaInstruction(i, OpCode.JMP, i.b.name)); break;
                case Instruction.Opcode.JumpIfFalse: _output.Add(new PhantasmaInstruction(i, OpCode.JMPIFNOT, i.b.name)); break;
                case Instruction.Opcode.JumpIfTrue: _output.Add(new PhantasmaInstruction(i, OpCode.JMPIF, i.b.name)); break;

                case Instruction.Opcode.Return: _output.Add(new PhantasmaInstruction(i, OpCode.RET)); break;  // not correct!

                case Instruction.Opcode.Not: _output.Add(new PhantasmaInstruction(i, OpCode.NOT)); break;

                default: throw new Exception("Unsupported opcode");
            }
        }

        private string ExportType(string name)
        {
            switch (name.ToLower())
            {
                case "byte[]": return "ByteArray";
                case "uint":
                case "int": return "Integer";
                default: return name;
            }
        }

        private void ExportABI(string name)
        {
            var root = DataNode.CreateObject();
            root.AddField("hash", "0xca960c410849c55ed7a172ebc0f14ac8151f3f08");
            root.AddField("entrypoint", "Main");

            var functions = DataNode.CreateArray("functions");
            var events = DataNode.CreateArray("events");

            foreach (var entry in tree.classes)
            {
                foreach (var method in entry.methods)
                {
                    if (method.visibility != Visibility.Public)
                    {
                        continue;
                    }

                    var node = DataNode.CreateObject();
                    functions.AddNode(node);

                    node.AddField("name", method.name);
                    node.AddField("returntype", ExportType(method.returnType));

                    var args = DataNode.CreateArray("parameters");
                    node.AddNode(args);

                    foreach (var argument in method.arguments)
                    {
                        var arg = DataNode.CreateObject();
                        arg.AddField("name", argument.decl.identifier);
                        arg.AddField("type", ExportType(argument.decl.typeName));
                        args.AddNode(arg);
                    }


                    functions.AddNode(node);
                }
            }

            if (functions.ChildCount > 0)
            {
                root.AddNode(functions);
            }

            if (events.ChildCount > 0)
            {
                root.AddNode(events);
            }

            var json = JSONWriter.WriteToString(root);
            File.WriteAllText(name + ".abi.json", json);
        }

        public void Export(string name)
        {
            File.WriteAllBytes(name+".avm", _script);

            ExportABI(name);
        }
    }
}
