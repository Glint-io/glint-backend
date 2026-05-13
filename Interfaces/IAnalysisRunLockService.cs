namespace glint_backend.Interfaces;

public interface IAnalysisRunLockService
{
    bool TryAcquire(string key, out IAnalysisRunLease lease);
}

public interface IAnalysisRunLease : IDisposable
{
}