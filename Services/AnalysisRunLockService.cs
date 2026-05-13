using System.Collections.Concurrent;
using glint_backend.Interfaces;

namespace glint_backend.Services;

public sealed class AnalysisRunLockService : IAnalysisRunLockService
{
    private readonly ConcurrentDictionary<string, byte> _activeRuns = new();

    public bool TryAcquire(string key, out IAnalysisRunLease lease)
    {
        if (_activeRuns.TryAdd(key, 0))
        {
            lease = new AnalysisRunLease(this, key);
            return true;
        }

        lease = null!;
        return false;
    }

    private void Release(string key)
    {
        _activeRuns.TryRemove(key, out _);
    }

    private sealed class AnalysisRunLease(AnalysisRunLockService owner, string key) : IAnalysisRunLease
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            owner.Release(key);
        }
    }
}