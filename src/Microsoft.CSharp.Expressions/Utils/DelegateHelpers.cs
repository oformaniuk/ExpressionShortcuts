// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Reflection;

#if !FEATURE_DYNAMIC_DELEGATE

#endif

namespace System.Dynamic.Utils
{
    internal static class DelegateHelpers
    {
#if !FEATURE_DYNAMIC_DELEGATE

        private static readonly CacheDict<Type, MethodInfo> s_thunks = new CacheDict<Type, MethodInfo>(256);
        private static readonly MethodInfo s_FuncInvoke = typeof(Func<object?[], object?>).GetMethod("Invoke")!;
        private static readonly MethodInfo s_ArrayEmpty = typeof(Array).GetMethod(nameof(ArrayEx.Empty))!.MakeGenericMethod(typeof(object));
        private static readonly MethodInfo[] s_ActionThunks = GetActionThunks();
        private static readonly MethodInfo[] s_FuncThunks = GetFuncThunks();

        public static void ActionThunk(Func<object?[], object?> handler)
        {
            handler(ArrayEx.Empty<object?>());
        }

        public static void ActionThunk1<T1>(Func<object?[], object?> handler, T1 t1)
        {
            handler(new object?[]{t1});
        }

        public static void ActionThunk2<T1, T2>(Func<object?[], object?> handler, T1 t1, T2 t2)
        {
            handler(new object?[]{t1, t2});
        }

        public static TReturn FuncThunk<TReturn>(Func<object?[], object> handler)
        {
            return (TReturn)handler(ArrayEx.Empty<object>());
        }

        public static TReturn FuncThunk1<T1, TReturn>(Func<object?[], object> handler, T1 t1)
        {
            return (TReturn)handler(new object?[]{t1});
        }

        public static TReturn FuncThunk2<T1, T2, TReturn>(Func<object?[], object> handler, T1 t1, T2 t2)
        {
            return (TReturn)handler(new object?[]{t1, t2});
        }

        private static MethodInfo[] GetActionThunks()
        {
            Type delHelpers = typeof(DelegateHelpers);
            return new MethodInfo[]{delHelpers.GetMethod("ActionThunk")!,
                                    delHelpers.GetMethod("ActionThunk1")!,
                                    delHelpers.GetMethod("ActionThunk2")!};
        }

        private static MethodInfo[] GetFuncThunks()
        {
            Type delHelpers = typeof(DelegateHelpers);
            return new MethodInfo[]{delHelpers.GetMethod("FuncThunk")!,
                                    delHelpers.GetMethod("FuncThunk1")!,
                                    delHelpers.GetMethod("FuncThunk2")!};
        }

        private static MethodInfo? GetCSharpThunk(Type returnType, bool hasReturnValue, ParameterInfo[] parameters)
        {
            try
            {
                if (parameters.Length > 2)
                {
                    return null; // Don't use C# thunks for more than 2 parameters
                }

                if (returnType.IsByRef || returnType.IsPointer)
                {
                    return null; // Don't use C# thunks for types that cannot be generic arguments
                }

                foreach (ParameterInfo parameter in parameters)
                {
                    Type parameterType = parameter.ParameterType;
                    if  (parameterType.IsByRef || parameterType.IsPointer)
                    {
                        return null; // Don't use C# thunks for types that cannot be generic arguments
                    }
                }

                int thunkTypeArgCount = parameters.Length;
                if (hasReturnValue)
                    thunkTypeArgCount++;

                Type[] thunkTypeArgs = thunkTypeArgCount == 0 ? Type.EmptyTypes : new Type[thunkTypeArgCount];
                for (int i = 0; i < parameters.Length; i++)
                {
                    thunkTypeArgs[i] = parameters[i].ParameterType;
                }

                MethodInfo uninstantiatedMethod;

                if (hasReturnValue)
                {
                    thunkTypeArgs[thunkTypeArgs.Length - 1] = returnType;
                    uninstantiatedMethod = s_FuncThunks[parameters.Length];
                }
                else
                {
                    uninstantiatedMethod = s_ActionThunks[parameters.Length];
                }

                return (thunkTypeArgs.Length > 0) ?
                    uninstantiatedMethod.MakeGenericMethod(thunkTypeArgs) :
                    uninstantiatedMethod;
            }
            catch
            {
                // If unable to instantiate thunk, fall back to dynamic method creation
                // This is expected to happen for cases such as function pointer types as arguments
                // Or new forms of types added to the typesystem in the future that aren't compatible with
                // generics
                return null;
            }
        }

#endif
    }
}
