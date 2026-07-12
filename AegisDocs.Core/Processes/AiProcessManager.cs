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

    public void StartProcess(string exePath, string modelPath)
    {
        Debug.WriteLine("[ProcessManager] Запуск нового сервера...");
        _currentProcess = new Process
        {
            StartInfo = new ProcessStartInfo 
            { 
                FileName = exePath,
                Arguments = $"\"{modelPath}\"",
                UseShellExecute = true 
            }
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
