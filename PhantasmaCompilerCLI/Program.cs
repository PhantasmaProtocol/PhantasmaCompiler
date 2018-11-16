using Phantasma.CodeGen.Core;
using Phantasma.Cryptography;
using Phantasma.Numerics;
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

        public override ExecutionState ExecuteInterop(string method)
        {
            return ExecutionState.Fault;
        }

        public override ExecutionContext LoadContext(Address address)
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var targetFile = "../../Examples/hello.cs";
            var extension = Path.GetExtension(targetFile);

            var src = File.ReadAllText(targetFile);

            var language = LanguageProcessor.GetLanguage(extension);
            var processor = LanguageProcessor.GetProcessor(language);

            var tokens = processor.Lexer.Execute(src);

            /*
            Console.WriteLine("****TOKENS***");
            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }*/


            Console.WriteLine();
            Console.WriteLine("****TREE***");

            var tree = processor.Parser.Execute(tokens);
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
            Console.WriteLine(Base16.Encode(generator.Script));

            Console.WriteLine();
            Console.WriteLine("****DISASSEMBLE***");
            var disasm = new Disassembler(generator.Script);

            foreach (var entry in disasm.Instructions)
            {
                Console.WriteLine(entry.ToString());
            }

            var vm = new TestVM(generator.Script);
            vm.Stack.Push(new VMObject().SetValue(2));
            vm.Stack.Push(new VMObject().SetValue(3));
            vm.Stack.Push(new VMObject().SetValue("add"));
            vm.Execute();

            var result = vm.Stack.Pop();
            Console.WriteLine("RESULT = "+result.ToObject().ToString());

            Console.WriteLine("****END***");
            Console.ReadKey();
        }
    }
}
