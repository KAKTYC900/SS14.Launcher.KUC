using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.Launcher.Models.ServerStatus;

/// <summary>
/// Measures the round-trip time to a server by timing a TCP connect to its status (HTTP) port.
/// This is an approximation: if the server is behind a proxy/CDN you measure the distance to that proxy.
/// </summary>
public static class ServerPing
{
    private const int Attempts = 3;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    /// <returns>Best time in milliseconds, or null if the server could not be reached.</returns>
    public static async Task<int?> MeasureAsync(string address, CancellationToken cancel = default)
    {
        if (!UriHelper.TryParseSs14Uri(address, out var parsed))
            return null;

        Uri statusUri;
        try
        {
            statusUri = UriHelper.GetServerStatusAddress(parsed);
        }
        catch (Exception)
        {
            return null;
        }

        long best = long.MaxValue;
        for (var i = 0; i < Attempts; i++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
                cts.CancelAfter(Timeout);

                using var client = new TcpClient();
                var sw = Stopwatch.StartNew();
                await client.ConnectAsync(statusUri.Host, statusUri.Port, cts.Token);
                sw.Stop();

                best = Math.Min(best, sw.ElapsedMilliseconds);
            }
            catch (Exception e) when (e is SocketException or OperationCanceledException or ObjectDisposedException)
            {
                if (cancel.IsCancellationRequested)
                    return null;
            }
        }

        return best == long.MaxValue ? null : (int)best;
    }
}
