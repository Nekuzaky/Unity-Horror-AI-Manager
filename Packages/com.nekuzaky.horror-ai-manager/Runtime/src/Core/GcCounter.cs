using Unity.Profiling;

namespace Nekuzaky.HorrorAI
{
    internal static class GcCounter
    {
        #region Public

        public const long Unavailable = -1L;

        public static bool Available => Start();

        public static long Bytes => Start() ? _bytes.CurrentValue : Unavailable;

        public static long Allocations => Start() && _allocations.Valid ? _allocations.Count : Unavailable;

        public static void Mark(out long bytes, out long allocations)
        {
            bytes = Bytes;
            allocations = Allocations;
        }

        public static long Since(long bytes, long allocations)
        {
            if (bytes < 0L || !Start())
                return Unavailable;
            var now = _bytes.CurrentValue;
            var grown = now >= bytes ? now - bytes : now;
            if (allocations < 0L || !_allocations.Valid)
                return grown;
            return _allocations.Count > allocations ? grown : 0L;
        }

        #endregion


        #region Private and Protected

        private static ProfilerRecorder _bytes;
        private static ProfilerRecorder _allocations;
        private static bool _started;

        private static bool Start()
        {
            if (_started)
                return _bytes.Valid;
            _started = true;
            _bytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _allocations = ProfilerRecorder.StartNew(new ProfilerCategory("GC"), "GC.Alloc", 1, ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            return _bytes.Valid;
        }

        #endregion
    }
}
