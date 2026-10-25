using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    public enum PatrolMode
    {
        Loop,
        PingPong,
        Once
    }

    [Serializable]
    public struct PatrolWaypoint
    {
        public Transform point;
        [Min(0f)] public float wait;

        public PatrolWaypoint(Transform point, float wait)
        {
            this.point = point;
            this.wait = wait;
        }
    }

    [AddComponentMenu("Horror AI/Patrol Route")]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/components.md#patrol-route")]
    public sealed class PatrolRoute : MonoBehaviour
    {
        #region Public

        public PatrolMode mode = PatrolMode.Loop;
        public List<PatrolWaypoint> waypoints = new();

        public int Count => waypoints != null ? waypoints.Count : 0;

        public Vector3 GetPosition(int index)
        {
            if (index < 0 || index >= Count)
                return transform.position;
            var point = waypoints[index].point;
            return point != null ? point.position : transform.position;
        }

        public float GetWait(int index) => index >= 0 && index < Count ? Mathf.Max(0f, waypoints[index].wait) : 0f;

        public static int Advance(int index, int count, PatrolMode mode, ref int direction)
        {
            if (count <= 1)
                return 0;
            if (direction == 0)
                direction = 1;
            switch (mode)
            {
                case PatrolMode.Loop:
                    return (index + 1) % count;
                case PatrolMode.Once:
                    return Mathf.Min(index + 1, count - 1);
                default:
                    var next = index + direction;
                    if (next < 0 || next >= count)
                    {
                        direction = -direction;
                        next = index + direction;
                    }
                    return Mathf.Clamp(next, 0, count - 1);
            }
        }

        public Transform AddWaypoint(Vector3 position, float wait = 1f)
        {
            var point = new GameObject("Waypoint " + (Count + 1)).transform;
            point.SetParent(transform, false);
            point.position = position;
            waypoints ??= new List<PatrolWaypoint>();
            waypoints.Add(new PatrolWaypoint(point, wait));
            return point;
        }

        #endregion
    }
}
