using System.Diagnostics;
using System.Text;
using Metapod.Core;

namespace Metapod.Infrastructure;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string executable,IReadOnlyList<string> arguments,string directory,string? input,TimeSpan timeout,CancellationToken token,IReadOnlyDictionary<string,string?>? environment = null)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory=directory,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8 };
        foreach(var arg in arguments) info.ArgumentList.Add(arg);
        info.Environment.Remove("CLAUDECODE");
        if(environment is not null) foreach(var pair in environment)
        {
            if(pair.Value is null) info.Environment.Remove(pair.Key);
            else info.Environment[pair.Key]=pair.Value;
        }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token); linked.CancelAfter(timeout);
        using var process = new Process { StartInfo=info };
        if(!process.Start()) throw new IOException("Não foi possível iniciar o CLI.");
        using var registration = linked.Token.Register(()=> { try { if(!process.HasExited) process.Kill(true); } catch(InvalidOperationException){} });
        var stdout=ReadBounded(process.StandardOutput,process,linked.Token);
        var stderr=ReadBounded(process.StandardError,process,linked.Token);
        try
        {
            if(input is not null) await process.StandardInput.WriteAsync(input.AsMemory(),linked.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(linked.Token);
            await Task.WhenAll(stdout,stderr);
            return new(process.ExitCode,await stdout,await stderr);
        }
        finally
        {
            try { if(!process.HasExited) process.Kill(true); } catch(InvalidOperationException) { }
            try { await Task.WhenAll(stdout,stderr); } catch(Exception e) when(e is IOException or OperationCanceledException or InvalidOperationException) { }
        }
    }
    private static async Task<string> ReadBounded(StreamReader reader,Process process,CancellationToken token)
    {
        var output=new StringBuilder(); var buffer=new char[4096]; int count;
        while((count=await reader.ReadAsync(buffer.AsMemory(),token))>0)
        {
            if(output.Length+count>1024*1024) { try { process.Kill(true); } catch(InvalidOperationException){} throw new IOException("Saída do processo excedeu 1 MB."); }
            output.Append(buffer,0,count);
        }
        return output.ToString();
    }
}
