using System;
using System.Collections.Generic;
using System.Numerics;
using Phantasma.CodeGen.Core;
using Phantasma.Utils;
using Phantasma.VM;

namespace Phantasma.CodeGen
{
    public class ByteCodeGenerator
    {
        private ModuleNode tree;

        private ScriptBuilder _output = new ScriptBuilder();
        private Dictionary<string, int> _offsets = new Dictionary<string, int>();
        private Dictionary<int, string> _jumps = new Dictionary<int, string>();

        private Dictionary<string, byte> _registerTable = new Dictionary<string, byte>();

        public byte[] Script { get; private set; }
        
        public ByteCodeGenerator(ModuleNode tree, List<Instruction> instructions)
        {
            this.tree = tree;

            /*MethodNode main = FindMethod("Main");

            foreach (var arg in main.arguments)
            {
                stack.Insert(0, arg.decl.identifier);
                important.Add(arg.decl.identifier);
            }*/
            
            foreach (var i in instructions)
            {
                TranslateInstruction(i);
            }

            foreach (var entry in _jumps)
            {
                if (!_offsets.ContainsKey(entry.Value))
                {
                    throw new Exception("Invalid jump offset");
                }

                var offset = _offsets[entry.Value]; 
                var targetOfs = entry.Key + 1; // skip the opcode byte
                _output.Patch(targetOfs, (ushort) offset);
            }

            /*foreach (var i in _output)
            {
                switch (i.Opcode)
                {
                    case Opcode.CALL:
                        {
                            i.data = BitConverter.GetBytes(i.target.offset);
                            break;
                        }

                    case Opcode.JMP:
                    case Opcode.JMPIF:
                    case Opcode.JMPNOT:
                        {
                            short offset = (short)(i.target.offset - i.offset);
                            i.data = BitConverter.GetBytes(offset);
                            break;
                        }
                }
            }*/

            Script = _output.ToScript();
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

        private byte FetchRegister(string name)
        {
            if (_registerTable.ContainsKey(name))
            {
                return _registerTable[name];
            }

            var register = (byte) _registerTable.Count;
            _registerTable[name] = register;
            return register;
        }

        private void InsertJump(Instruction i, Opcode Opcode)
        {
            var len = Opcode == Opcode.JMP ? 2 : 3;
            var data = new byte[len];

            // for conditional jumps, fetch the appropriate register for the conditional value
            if (Opcode != Opcode.JMP)
            {
                data[data.Length - 1] = FetchRegister(i.a.name);
            }

            var ofs = _output.Emit(Opcode, data);

            // store which label to jump to
            _jumps[ofs] = i.b.name;
        }

        private void InsertOp(Instruction i, Opcode Opcode)
        {
            if (i.b != null)
            {
                var a = FetchRegister(i.a.name);
                var b = FetchRegister(i.b.name);
                var dst = FetchRegister(i.name);

                _output.Emit(Opcode, new byte[] { a, b, dst });
            }
            else
            {
                var src = FetchRegister(i.a.name);
                var dst = FetchRegister(i.name);

                _output.Emit(Opcode, new byte[] { src, dst });
            }
        }

        public void TranslateInstruction(Instruction i)
        {
            switch (i.op)
            {
                case Instruction.Opcode.Label:
                    {
                        var ofs = _output.Emit(Opcode.NOP);
                        _offsets[i.name] = ofs;
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
                                        var reg = FetchRegister(i.name);
                                        _output.EmitLoad(reg, (string)i.literal.value);
                                        break;
                                    }

                                case LiteralKind.Boolean:
                                    {
                                        var reg = FetchRegister(i.name);
                                        _output.EmitLoad(reg, (bool)i.literal.value);
                                        break;

                                    }
                                case LiteralKind.Integer:
                                    {
                                        var reg = FetchRegister(i.name);
                                        _output.EmitLoad(reg, (BigInteger)i.literal.value);
                                        break;
                                    }

                                default: throw new Exception("Unsuported " + i.literal.kind);
                            }
                        }
                        else
                        if (i.variable != null)
                        {
                            var reg = FetchRegister(i.name);
                            _output.EmitLoad(reg, "var_load_missing!");
                        }
                        else
                        {
                            var src = FetchRegister(i.a.name);
                            var dst = FetchRegister(i.name);
                            _output.EmitMove(src, dst);
                        }
                        break;

                    }

                case Instruction.Opcode.Add: { InsertOp(i, Opcode.ADD); break; }
                case Instruction.Opcode.Sub: { InsertOp(i, Opcode.SUB); break; }
                case Instruction.Opcode.Mul: { InsertOp(i, Opcode.MUL); break; }
                case Instruction.Opcode.Div: { InsertOp(i, Opcode.DIV); break; }
                case Instruction.Opcode.Mod: { InsertOp(i, Opcode.MOD); break; }
                case Instruction.Opcode.Shr: { InsertOp(i, Opcode.SHR); break; }
                case Instruction.Opcode.Shl: { InsertOp(i, Opcode.SHL); break; }
                case Instruction.Opcode.Equals: { InsertOp(i, Opcode.EQUAL); break; }
                case Instruction.Opcode.LessThan: { InsertOp(i, Opcode.LT); break; }
                case Instruction.Opcode.GreaterThan: { InsertOp(i, Opcode.GT); break; }
                case Instruction.Opcode.LessOrEqualThan: { InsertOp(i, Opcode.LTE); break; }
                case Instruction.Opcode.GreaterOrEqualThan: { InsertOp(i, Opcode.GTE); break; }


                case Instruction.Opcode.Jump: InsertJump(i, Opcode.JMP); break;
                case Instruction.Opcode.JumpIfFalse: InsertJump(i, Opcode.JMPNOT); break;
                case Instruction.Opcode.JumpIfTrue: InsertJump(i, Opcode.JMPIF); break;

                case Instruction.Opcode.Return: _output.Emit(Opcode.RET); break;  // not correct?

                case Instruction.Opcode.Not:
                    {
                        var src = FetchRegister(i.a.name);
                        var dst = FetchRegister(i.name);
                        _output.Emit(Opcode.NOT, new byte[] { src, dst} ); break;
                    }
                    

                default: throw new Exception("Unsupported Opcode: "+ i.op);
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

        /*        private void ExportABI(string name)
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
                */
    }
}
