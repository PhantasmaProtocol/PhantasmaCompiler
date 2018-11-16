using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantasma.CodeGen.Core;
using Phantasma.CodeGen.Core.Nodes;
using Phantasma.Cryptography;
using Phantasma.Numerics;
using Phantasma.VM;
using System;

namespace Phantasma.Tests
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

    [TestClass]
    public class SemanticTests
    {
        private object CompileAndExecute(ModuleNode tree)
        {
            var compiler = new Compiler();
            var instructions = compiler.Execute(tree);

            var generator = new ByteCodeGenerator(tree, instructions);

            var vm = new TestVM(generator.Script);
            vm.Stack.Push(new VMObject().SetValue(2));
            vm.Stack.Push(new VMObject().SetValue(3));
            vm.Stack.Push(new VMObject().SetValue("add"));
            vm.Execute();

            var result = vm.Stack.Pop();
            return result.ToObject();
        }

        [TestMethod]
        public void TestReturnTypeValidationSuccess()
        {
            var module = new ModuleNode();
            var @class = new ClassNode(module);
            var method = new MethodNode(@class);
            method.returnType = new TypeNode(method, TypeKind.String);

            var returnSt = new ReturnNode(method);
            var literal = new LiteralExpressionNode(returnSt);
            literal.kind = LiteralKind.String;
            literal.value = "hello";
            returnSt.expr = literal;
            method.body = returnSt;

            Assert.IsTrue(module.Validate());

            var result = CompileAndExecute(module) as string;
            Assert.IsTrue(result == "hello");
        }

        [TestMethod]
        public void TestReturnTypeValidationFailure()
        {
            var module = new ModuleNode();
            var @class = new ClassNode(module);
            var method = new MethodNode(@class);
            method.returnType = new TypeNode(method, TypeKind.String);

            var returnSt = new ReturnNode(method);
            var literal = new LiteralExpressionNode(returnSt);
            literal.kind = LiteralKind.Integer;
            literal.value = 1;
            returnSt.expr = literal;
            method.body = returnSt;

            Assert.IsFalse(module.Validate());
        }

        [TestMethod]
        public void TestBinaryExpressionValidationSuccess()
        {
            var module = new ModuleNode();
            var @class = new ClassNode(module);
            var method = new MethodNode(@class);
            method.returnType = new TypeNode(method, TypeKind.String);

            var returnSt = new ReturnNode(method);

            var binaryExpr = new BinaryExpressionNode(returnSt);

            var literalA = new LiteralExpressionNode(binaryExpr);
            literalA.kind = LiteralKind.Integer;
            literalA.value = 1;

            var literalB = new LiteralExpressionNode(binaryExpr);
            literalB.kind = LiteralKind.Integer;
            literalB.value = 3;

            binaryExpr.left = literalA;
            binaryExpr.right = literalB;
            binaryExpr.op = "+";

            returnSt.expr = binaryExpr;
            method.body = returnSt;

            Assert.IsFalse(module.Validate());

            var result = CompileAndExecute(module) as BigInteger;
            Assert.IsTrue(result == 4);
        }

        [TestMethod]
        public void TestBinaryExpressionValidationFailure()
        {
            var module = new ModuleNode();
            var @class = new ClassNode(module);
            var method = new MethodNode(@class);
            method.returnType = new TypeNode(method, TypeKind.String);

            var returnSt = new ReturnNode(method);

            var binaryExpr = new BinaryExpressionNode(returnSt);

            var literalA = new LiteralExpressionNode(binaryExpr);
            literalA.kind = LiteralKind.Integer;
            literalA.value = 1;

            var literalB = new LiteralExpressionNode(binaryExpr);
            literalB.kind = LiteralKind.String;
            literalB.value = "A";

            binaryExpr.left = literalA;
            binaryExpr.right = literalB;
            binaryExpr.op = "+";

            returnSt.expr = binaryExpr;
            method.body = returnSt;

            Assert.IsFalse(module.Validate());
        }

    }
}
