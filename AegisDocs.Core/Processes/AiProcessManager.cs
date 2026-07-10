using AegisDocs.Core.Interfaces;
using System.Diagnostics;

namespace AegisDocs.Core.Processes;

public class AiProcessManager : IAiProcessManager
{
    private Process? _currentProcess;

    public void KillOldProcesses(string processName)
    {
        Debug.WriteLine($"[ProcessManager] Очистка старых процессов {processName}...");
        foreach (var proc in Process.GetProcessesByName(processName))
        {
            try { proc.Kill(); Debug.WriteLine($"[ProcessManager] Убит процесс {proc.Id}"); } catch { }
        }
    }

    public void StartProcess(string exePath)
    {
        Debug.WriteLine("[ProcessManager] Запуск нового сервера...");
        _currentProcess = new Process
        {
            StartInfo = new ProcessStartInfo { FileName = exePath, UseShellExecute = true }
        };
        _currentProcess.Start();
    }

    public void StopProcess()
    {
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            try { _currentProcess.Kill(); } catch { }
        }
    }
}
