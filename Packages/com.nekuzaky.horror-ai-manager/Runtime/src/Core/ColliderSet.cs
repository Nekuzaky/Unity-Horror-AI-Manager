using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    internal sealed class ColliderSet
    {
        #region Public

        public int Count => _count;

        public void Refresh(Component root)
        {
            _count = 0;
            if (root == null)
                return;
            root.GetComponentsInChildren(true, Buffer);
            if (_ids.Length < Buffer.Count)
                _ids = new EntityId[Buffer.Count];
            for (var i = 0; i < Buffer.Count; i++)
                _ids[_count++] = Buffer[i].GetEntityId();
            Buffer.Clear();
        }

        public bool Contains(EntityId id)
        {
            for (var i = 0; i < _count; i++)
            {
                if (_ids[i] == id)
                    return true;
            }
            return false;
        }

        #endregion


        #region Private and Protected

        private static readonly List<Collider> Buffer = new(16);
        private EntityId[] _ids = new EntityId[4];
        private int _count;

        #endregion
    }
}
