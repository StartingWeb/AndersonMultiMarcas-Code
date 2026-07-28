using System.Collections.Concurrent;

namespace Project.Features.Storage.Legacy;

public sealed class LegacyImportCancellationRegistry
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> tokens = new();

    public CancellationTokenSource Register(int jobId, CancellationToken hostToken)
    {
        var next = CancellationTokenSource.CreateLinkedTokenSource(hostToken);
        var current = tokens.AddOrUpdate(
            jobId,
            next,
            (_, previous) =>
            {
                previous.Cancel();
                previous.Dispose();
                return next;
            });

        return current;
    }

    public void Cancel(int jobId)
    {
        if (tokens.TryGetValue(jobId, out var source))
        {
            source.Cancel();
        }
    }

    public void Unregister(int jobId)
    {
        if (tokens.TryRemove(jobId, out var source))
        {
            source.Dispose();
        }
    }
}
