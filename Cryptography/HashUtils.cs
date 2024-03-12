using System;
using System.Runtime.CompilerServices;

namespace RoflanArchives.Core.Cryptography;

internal static class HashUtils
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static unsafe bool SecureEqualsUnsafe<T>(
        ReadOnlySpan<T> left, ReadOnlySpan<T> right)
        where T : unmanaged
    {
        var minLength = Math.Min(
            left.Length, right.Length);
        var lengthDifference =
            left.Length - right.Length;

        var length = minLength - (minLength % sizeof(long));

        fixed (T* leftPointer = left)
        fixed (T* rightPointer = right)
        {
            long difference = lengthDifference;

            for (var i = 0; i < length; i += sizeof(long))
            {
                difference |=
                    *(long*)(leftPointer + i) - *(long*)(rightPointer + i);
            }

            for (var i = length; i < minLength; ++i)
            {
                difference |=
                    (long)(*(byte*)(leftPointer + i) - *(byte*)(rightPointer + i));
            }

            return difference == 0;
        }
    }
}
