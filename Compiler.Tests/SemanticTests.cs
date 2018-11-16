using Microsoft.VisualStudio.TestTools.UnitTesting;
using Phantasma.CodeGen.Core;
using Phantasma.CodeGen.Core.Nodes;

namespace Phantasma.Tests
{
    [TestClass]
    public class SemanticTests
    {
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

    }
}
