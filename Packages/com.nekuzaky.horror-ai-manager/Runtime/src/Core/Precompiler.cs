using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nekuzaky.HorrorAI
{
    internal static class Precompiler
    {
        #region Public

        public static bool Done => _done;

        public static void Run()
        {
            if (_done)
                return;
            _done = true;
            var types = typeof(Precompiler).Assembly.GetTypes();
            for (var i = 0; i < types.Length; i++)
            {
                if (!types[i].ContainsGenericParameters)
                    Prepare(types[i]);
            }
        }

        #endregion


        #region Private and Protected

        private const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                                              BindingFlags.Public | BindingFlags.NonPublic;

        private static bool _done;

        private static void Prepare(Type type)
        {
            var methods = type.GetMethods(Declared);
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (method.IsAbstract || method.ContainsGenericParameters || method.DeclaringType != type)
                    continue;
                try
                {
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion
    }
}
