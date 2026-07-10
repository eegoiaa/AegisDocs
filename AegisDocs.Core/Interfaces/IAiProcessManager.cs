namespace AegisDocs.Core.Interfaces;

public interface IAiProcessManager
{
    void KillOldProcesses(string processName);
    void StartProcess(string exePath);
    void StopProcess();
}
