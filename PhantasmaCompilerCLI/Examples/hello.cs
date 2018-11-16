using Phantasma.SmartContract.Core;
using System;

namespace HelloWorld
{
    public class MyContract : Contract
    {
        public static int Main(string operation, int a, int b)
        {
            switch (operation) {
                case "add": return a + b;
                case "sub": return a - b;
                case "mul": return a * b;
                default: return -1;
            }
        }
    }
}