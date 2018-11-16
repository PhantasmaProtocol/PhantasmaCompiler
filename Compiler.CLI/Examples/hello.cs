using Phantasma.SmartContract.Core;
using System;

namespace HelloWorld
{
    public class MyContract : Contract
    {
        public static int Add(string operation, int a, int b)
        {
			return a + b;
		}
		
        public static int Subtract(string operation, int a, int b){
            return a - b;
		}
		
		public static int Add(string operation, int a, int b) {
                return a * b;
        }
    }
}