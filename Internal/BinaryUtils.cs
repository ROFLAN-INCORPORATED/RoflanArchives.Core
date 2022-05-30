using System;

namespace RoflanArchive.Core.Internal
{
    internal class BinaryUtils
    {
        public static byte Get7BitEncodedIntLength(
            int value)
        {
            return value switch
            {
                < 0b1_0000000 => 1,
                < 0b01_0000000_0000000 => 2,
                < 0b001_0000000_0000000_0000000 => 3,
                < 0b0001_0000000_0000000_0000000_0000000 => 4,
                _ => 5
            };
        }
    }
}
