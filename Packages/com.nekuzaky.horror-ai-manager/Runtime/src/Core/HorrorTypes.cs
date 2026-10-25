using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    public enum HorrorState
    {
        Patrol,
        Alert,
        Investigate,
        Search,
        Chase,
        LostTrack
    }

    public enum TransitionReason
    {
        None,
        Started,
        SuspiciousSight,
        HeardNoise,
        CalmedDown,
        AlertOver,
        ReachedLastKnownPosition,
        InvestigationTimedOut,
        Spotted,
        Reacquired,
        LostSight,
        LostTrackOver,
        GaveUp,
        Scripted,
        CouldNotReach,
        Disabled
    }

    public enum MemorySource
    {
        None,
        Sight,
        Hearing,
        Scripted
    }

    public readonly struct HorrorTransition
    {
        public readonly HorrorState From;
        public readonly HorrorState To;
        public readonly TransitionReason Reason;
        public readonly float Time;

        public HorrorTransition(HorrorState from, HorrorState to, TransitionReason reason, float time)
        {
            From = from;
            To = to;
            Reason = reason;
            Time = time;
        }

        public string Explanation => Explain(Reason);

        public static string Explain(TransitionReason reason)
        {
            switch (reason)
            {
                case TransitionReason.Started: return "started patrolling";
                case TransitionReason.SuspiciousSight: return "glimpsed something";
                case TransitionReason.HeardNoise: return "heard a noise";
                case TransitionReason.CalmedDown: return "calmed down";
                case TransitionReason.AlertOver: return "went to check";
                case TransitionReason.ReachedLastKnownPosition: return "reached the last known position";
                case TransitionReason.InvestigationTimedOut: return "could not reach the spot in time";
                case TransitionReason.Spotted: return "spotted the target";
                case TransitionReason.Reacquired: return "saw the target again";
                case TransitionReason.LostSight: return "lost sight of the target";
                case TransitionReason.LostTrackOver: return "lost the trail";
                case TransitionReason.GaveUp: return "gave up the search";
                case TransitionReason.Scripted: return "told to by a script";
                case TransitionReason.CouldNotReach: return "could not reach the last known position";
                case TransitionReason.Disabled: return "was disabled";
                default: return "nothing yet";
            }
        }
    }

    public struct HorrorMemory
    {
        public Vector3 Position;
        public float Time;
        public float Certainty;
        public MemorySource Source;

        public bool HasMemory => Certainty > 0f;

        public void Remember(Vector3 position, float time, float certainty, MemorySource source)
        {
            Position = position;
            Time = time;
            Certainty = Mathf.Clamp01(Mathf.Max(certainty, Certainty));
            Source = source;
        }

        public void Decay(float rate, float deltaTime)
        {
            if (Certainty <= 0f)
                return;
            Certainty = Mathf.Max(0f, Certainty - rate * deltaTime);
            if (Certainty <= 0f)
                Source = MemorySource.None;
        }

        public void Forget()
        {
            Certainty = 0f;
            Source = MemorySource.None;
        }
    }

    public readonly struct HorrorNoise
    {
        public readonly Vector3 Position;
        public readonly float Loudness;
        public readonly GameObject Source;
        public readonly float Time;

        public HorrorNoise(Vector3 position, float loudness, GameObject source, float time)
        {
            Position = position;
            Loudness = loudness;
            Source = source;
            Time = time;
        }
    }

    public struct HorrorPerception
    {
        public HorrorTarget Target;
        public bool Sees;
        public float VisibleFraction;
        public float Distance;
        public float Exposure;
        public float GainRate;
        public float HeardLoudness;
        public Vector3 HeardPosition;
        public float HeardTime;
        public float LastUpdate;
    }
}
