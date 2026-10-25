using System;
using System.Reflection;
using UnityEngine.Events;

namespace Nekuzaky.HorrorAI
{
    internal static class EventPrimer
    {
        #region Public

        public static bool Supported => Prepare != null;

        public static void Prime(UnityEventBase unityEvent)
        {
            if (unityEvent == null || Prepare == null)
                return;
            try
            {
                Prepare.Invoke(unityEvent, null);
            }
            catch (TargetInvocationException)
            {
            }
        }

        #endregion


        #region Private and Protected

        private static readonly MethodInfo Prepare = typeof(UnityEventBase).GetMethod("PrepareInvoke",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);

        #endregion
    }
}
