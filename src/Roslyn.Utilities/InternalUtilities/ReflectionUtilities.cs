using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Roslyn.Utilities
{
    public static class ReflectionUtilities
    {
        private static readonly Type Missing = typeof(void);

        public static Type TryGetType(string assemblyQualifiedName)
        {
            try
            {
                return Type.GetType(assemblyQualifiedName, false);
            }
            catch
            {
                return null;
            }
        }

        public static Type TryGetType(ref Type lazyType, string assemblyQualifiedName)
        {
            if (lazyType == null)
            {
                lazyType = TryGetType(assemblyQualifiedName) ?? Missing;
            }

            return lazyType == Missing ? null : lazyType;
        }

        public static Type GetTypeFromEither(string contractName, string desktopName)
        {
            Type type = TryGetType(contractName) ?? TryGetType(desktopName);

            return type;
        }

        public static Type GetTypeFromEither(ref Type lazyType, string contractName, string desktopName)
        {
            if (lazyType == null)
            {
                lazyType = GetTypeFromEither(contractName, desktopName) ?? Missing;
            }

            return lazyType == Missing ? null : lazyType;
        }

        public static T FindItem<T>(IEnumerable<T> collection, params Type[] paramTypes)
            where T : MethodBase
        {
            foreach (T current in collection)
            {
                ParameterInfo[] p = current.GetParameters();
                if (p.Length != paramTypes.Length)
                {
                    continue;
                }

                bool allMatch = true;
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (p[i].ParameterType != paramTypes[i])
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return current;
                }
            }

            return null;
        }

        public static MethodInfo GetDeclaredMethod(this TypeInfo typeInfo, string name, params Type[] paramTypes)
        {
            return FindItem(typeInfo.GetDeclaredMethods(name), paramTypes);
        }

        public static ConstructorInfo GetDeclaredConstructor(this TypeInfo typeInfo, params Type[] paramTypes)
        {
            return FindItem(typeInfo.DeclaredConstructors, paramTypes);
        }

        public static T CreateDelegate<T>(this MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return default;
            }

            return (T) (object) methodInfo.CreateDelegate(typeof(T));
        }

        public static T InvokeConstructor<T>(this ConstructorInfo constructorInfo, params object[] args)
        {
            if (constructorInfo == null)
            {
                return default;
            }

            try
            {
                return (T) constructorInfo.Invoke(args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                return default;
            }
        }

        public static object InvokeConstructor(this ConstructorInfo constructorInfo, params object[] args)
        {
            return constructorInfo.InvokeConstructor<object>(args);
        }

        public static T Invoke<T>(this MethodInfo methodInfo, object obj, params object[] args)
        {
            return (T) methodInfo.Invoke(obj, args);
        }
    }
}
