using Phantasma.Codegen.Core;
using System;
using System.IO;

namespace Phantasma.Codegen
{
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
            Console.WriteLine("****OPCODES***");
            var phantasma = new PhantasmaCompiler(tree, instructions);
            Instruction anotation = null;
            foreach (var entry in phantasma.Instructions)
            {
                if (anotation != entry.source)
                {
                    anotation = entry.source;
                    Console.WriteLine("// " + anotation);
                }
                Console.WriteLine(entry);
            }

            phantasma.Export("output");

            Console.ReadKey();
        }
    }
}
