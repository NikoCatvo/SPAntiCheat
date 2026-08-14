using System.Collections.Generic;

namespace NitroShield
{
    internal class RateTracker
    {
        private readonly Dictionary<int, Queue<float>> _events = new();

        public int Record(int ownerId, float now, float windowSeconds)
        {
            if (!_events.TryGetValue(ownerId, out var q))
            {
                q = new Queue<float>();
                _events[ownerId] = q;
            }

            q.Enqueue(now);
            while (q.Count > 0 && now - q.Peek() > windowSeconds)
                q.Dequeue();

            return q.Count;
        }

        public void Reset(int ownerId) => _events.Remove(ownerId);
        public void ResetAll() => _events.Clear();
    }
}
