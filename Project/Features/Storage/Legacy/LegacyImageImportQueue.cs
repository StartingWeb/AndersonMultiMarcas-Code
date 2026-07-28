using System.Threading.Channels;

namespace Project.Features.Storage.Legacy;

public sealed class LegacyImageImportQueue
{
    private readonly Channel<int> channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask QueueAsync(int jobId, CancellationToken ct)
        => channel.Writer.WriteAsync(jobId, ct);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct)
        => channel.Reader.ReadAllAsync(ct);
}
