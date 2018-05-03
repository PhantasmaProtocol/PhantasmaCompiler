using System;

namespace Phantasma.Codegen.Core
{
    public static class Utility
    {
        public static string BytesToHex(this byte[] data)
        {
            string hex = BitConverter.ToString(data);
            return hex;
        }

    }
}
