namespace AegisDocs.Core.Interfaces;

public interface IIpcClient : IDisposable
{
    Task ConnectAsync(string pipeName, int timeoutMs);
    Task<string> SendAndReceiveAsync(string message, CancellationToken cancellationToken);
    void SendDisconnectSignal();
}
