using Phantasma.CodeGen.Core;
using Phantasma.CodeGen.Tools;
using Phantasma.Utils;
using Phantasma.VM;
using System;
using System.IO;

namespace Phantasma.CodeGen
{
    public class TestVM : VirtualMachine
    {
        public TestVM(byte[] script) : base(script)
        {

        }

        public override bool ExecuteInterop(string method)
        {
            throw new NotImplementedException();
        }

        public override ExecutionContext LoadContext(byte[] key)
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var src = File.ReadAllText("../../Examples/hello.cs");

            var tokens = Lexer.Execute(src);

            /*
            Console.WriteLine("****TOKENS***");
            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }*/


            Console.WriteLine();
            Console.WriteLine("****TREE***");

            var tree = Parser.Execute(tokens);
            tree.Visit((x, level) => Console.WriteLine(new String('\t', level) + x.ToString()));

            var compiler = new Compiler();
            var instructions = compiler.Execute(tree);

            Console.WriteLine();
            Console.WriteLine("****INSTRUCTIONS***");
            foreach (var entry in instructions)
            {
                Console.WriteLine(entry);
            }

            Console.WriteLine();
            Console.WriteLine("****SCRIPT***");
            var generator = new ByteCodeGenerator(tree, instructions);
            Console.WriteLine(generator.Script.ByteToHex());

            Console.WriteLine();
            Console.WriteLine("****DISASSEMBLE***");
            var disasm = Disassembler.Execute(generator.Script);
            foreach (var entry in disasm)
            {
                Console.WriteLine(entry.ToString());
            }

            var vm = new TestVM(generator.Script);
            vm.stack.Push(new VMObject().SetValue(2));
            vm.stack.Push(new VMObject().SetValue(3));
            vm.stack.Push(new VMObject().SetValue("add"));
            vm.Execute();

            Console.WriteLine("****END***");
            Console.ReadKey();
        }
    }
}
