using System.Runtime.CompilerServices;

namespace System.Dynamic.Utils
{
    internal static class ArrayEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Empty<T>()
        {
#if NET452 || NET451 || NET45
            return new T[0];
            #else
            return Array.Empty<T>();
#endif
        }
    }
}