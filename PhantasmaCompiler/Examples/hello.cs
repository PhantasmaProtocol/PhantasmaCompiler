using Phantasma.SmartContract.Framework.Services.Phantasma;
using Phantasma.SmartContract.Framework.Services.System;
using Phantasma.SmartContract.Framework;
using System.Numerics;
using System;

namespace Phantasma.SmartContract
{
    public class Contract1 : Framework.SmartContract
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