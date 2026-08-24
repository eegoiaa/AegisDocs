namespace AegisDocs.Core.Interfaces;

public interface IIpcClient
{
    Task<string> SendAndReceiveAsync(string message, CancellationToken cancellationToken = default);
}
