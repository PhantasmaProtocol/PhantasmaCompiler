using Phantasma.Utils;
using Phantasma.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Phantasma.CodeGen.Tools
{
    public class BaseInstruction
    {
        public Opcode opcode;

        public override string ToString()
        {
            return opcode.ToString();
        }
    }

    public class JumpInstruction : BaseInstruction
    {
        public ushort offset;
        public byte register;

        public override string ToString()
        {
            var s = base.ToString() + $" @{offset}";

            if (opcode == Opcode.JMPIF || opcode == Opcode.NOT)
            {
                s += $" / r{register}";
            }

            return s;
        }
    }

    public class CallInstruction : BaseInstruction
    {
        public string method;

        public override string ToString()
        {
            return base.ToString() + " " + method;
        }
    }

    public class SingleRegisterInstruction : BaseInstruction
    {
        public byte register;

        public override string ToString()
        {
            return base.ToString() + $" r{register}";
        }
    }

    public class TwoRegisterInstruction : BaseInstruction
    {
        public byte source;
        public byte dest;

        public override string ToString()
        {
            return base.ToString() + $" r{source}, r{dest}";
        }
    }

    public class ThreeRegisterInstruction : BaseInstruction
    {
        public byte sourceA;
        public byte sourceB;
        public byte dest;

        public override string ToString()
        {
            return base.ToString() + $" r{sourceA}, r{sourceB}, r{dest}";
        }
    }

    public class LoadInstruction : BaseInstruction
    {
        public byte dest;
        public VMType type;
        public byte[] data;

        public override string ToString()
        {
            string str;

            switch (type)
            {
                case VMType.String: str = "\"" + Encoding.UTF8.GetString(data) + "\""; break;
                case VMType.Number: var n = new BigInteger(data); str = n.ToString(); break;
                case VMType.Bool: str = (data != null && data.Length > 0 && data[0]!=0) ? "true":"false"; break;
                default: str = "0x" + data.ByteToHex(); break;
            }
            
            return base.ToString() + $" r{dest}, {type}, {str}";
        }
    }

    public static class Disassembler
    {
        private static ulong ReadVar(this BinaryReader reader, ulong max)
        {
            byte n = reader.ReadByte();

            ulong val;

            switch (n)
            {
                case 0xFD: val = reader.ReadUInt16(); break;
                case 0xFE: val = reader.ReadUInt32(); break;
                case 0xFF: val = reader.ReadUInt64(); break;
                default: val = n; break;
            }

            if (val > max)
            {
                throw new Exception("Input exceed max");
            }

            return val;
        }

        public static IEnumerable<BaseInstruction> Execute(byte[] script)
        {
            using (var stream = new MemoryStream(script))
            {
                using (var reader = new BinaryReader(stream))
                {
                    return Execute(reader);
                }
            }
        }

        public static IEnumerable<BaseInstruction> Execute(BinaryReader reader)
        {
            var output = new List<BaseInstruction>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var opcode = (Opcode)reader.ReadByte();
                BaseInstruction i;

                switch (opcode)
                {
                    case Opcode.RET:
                    case Opcode.NOP:
                        {
                            i = new BaseInstruction();
                            break;
                        }

                    case Opcode.COPY:
                    case Opcode.MOVE:
                    case Opcode.SWAP:
                        {
                            var j = new TwoRegisterInstruction();
                            j.source = reader.ReadByte();
                            j.dest = reader.ReadByte();
                            i = j;
                            break;
                        }

                    case Opcode.LOAD:
                        {
                            var j = new LoadInstruction();
                            j.dest = reader.ReadByte();
                            j.type = (VMType)reader.ReadByte();
                            var len = reader.ReadVar(0xFFF);
                            j.data = reader.ReadBytes((int)len);
                            i = j;
                            break;
                        }

                    case Opcode.PUSH:
                    case Opcode.POP:
                        {
                            var j = new SingleRegisterInstruction();
                            j.register = reader.ReadByte();
                            i = j;
                            break;
                        }

                    case Opcode.EXTCALL:
                        {
                            var j = new CallInstruction();
                            var len = reader.ReadByte();
                            var bytes = reader.ReadBytes(len);
                            j.method = Encoding.UTF8.GetString(bytes);
                            i = j;
                            break;
                        }

                    case Opcode.CALL:
                    case Opcode.JMP:
                    case Opcode.JMPIF:
                    case Opcode.JMPNOT:
                        {
                            var j = new JumpInstruction();
                            j.offset = reader.ReadUInt16();

                            if (opcode == Opcode.JMPIF || opcode == Opcode.JMPNOT)
                            {
                                j.register = reader.ReadByte();
                            }

                            i = j;
                            break;
                        }

                    case Opcode.CAT:
                    case Opcode.SUBSTR:
                    case Opcode.LEFT:
                    case Opcode.RIGHT:
                        {
                            throw new NotImplementedException();
                        }

                    case Opcode.SIZE:
                        {
                            var j = new TwoRegisterInstruction();
                            j.source = reader.ReadByte();
                            j.dest = reader.ReadByte();
                            i = j;
                            break;
                        }

                    case Opcode.NOT:
                        {
                            var j = new SingleRegisterInstruction();
                            j.register = reader.ReadByte();
                            i = j;
                            break;
                        }

                    case Opcode.AND:
                    case Opcode.OR:
                    case Opcode.XOR:
                    case Opcode.EQUAL:
                    case Opcode.LT:
                    case Opcode.GT:
                        {
                            var j = new ThreeRegisterInstruction();
                            j.sourceA = reader.ReadByte();
                            j.sourceB = reader.ReadByte();
                            j.dest = reader.ReadByte();
                            i = j;

                            break;
                        }

                    case Opcode.INC:
                    case Opcode.DEC:
                        {
                            var j = new SingleRegisterInstruction();
                            j.register = reader.ReadByte();
                            i = j;

                            break;
                        }

                    case Opcode.SIGN:
                    case Opcode.NEGATE:
                    case Opcode.ABS:
                        {
                            var j = new TwoRegisterInstruction();
                            j.source = reader.ReadByte();
                            j.dest = reader.ReadByte();
                            i = j;
                            break;
                        }

                    case Opcode.ADD:
                    case Opcode.SUB:
                    case Opcode.MUL:
                    case Opcode.DIV:
                    case Opcode.MOD:
                    case Opcode.SHR:
                    case Opcode.SHL:
                    case Opcode.MIN:
                    case Opcode.MAX:
                        {
                            var j = new ThreeRegisterInstruction();
                            j.sourceA = reader.ReadByte();
                            j.sourceB = reader.ReadByte();
                            j.dest = reader.ReadByte();
                            i = j;

                            break;
                        }

                    case Opcode.PUT:
                    case Opcode.GET:
                        {
                            var j = new ThreeRegisterInstruction();
                            j.sourceA = reader.ReadByte();
                            j.sourceB = reader.ReadByte();
                            j.dest = reader.ReadByte();
                            i = j;

                            break;
                        }

                    default:
                        {
                            throw new Exception("Disassembling failed");
                        }
                }

                i.opcode = opcode;
                output.Add(i);

                Console.WriteLine(i);
            }
            return output;
        }
    }
}
