using System.Diagnostics;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Previews;

internal sealed record ExternalPreviewProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);

internal static class ExternalPreviewProcess
{
    public static async Task<ExternalPreviewProcessResult> RunAsync(
        PreviewWorkerOptions options,
        PreviewOutputPlan plan,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = options.Executable!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in options.PrefixArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(plan.OriginalPath);
        foreach (PreviewOutputFile output in plan.Outputs)
        {
            startInfo.ArgumentList.Add(output.TemporaryPath);
        }
        foreach (PreviewOutputFile output in plan.Outputs)
        {
            startInfo.ArgumentList.Add(output.Width.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        using Process process = new() { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new PreviewRenderingException("The preview renderer did not start.", true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new PreviewRenderingException("The preview renderer could not be started.", true, exception);
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.RenderTimeoutSeconds));
        Task<string> standardOutput = BoundedProcessOutput.ReadAsync(process.StandardOutput, timeout.Token);
        Task<string> standardError = BoundedProcessOutput.ReadAsync(process.StandardError, timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return new ExternalPreviewProcessResult(
                process.ExitCode,
                await standardOutput,
                await standardError);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Kill(process);
            await ObserveAsync(standardOutput, standardError);
            throw new PreviewRenderingException("The preview renderer timed out.", true);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            await ObserveAsync(standardOutput, standardError);
            throw;
        }
    }

    private static void Kill(Process process)
    {
        try { process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }

    private static async Task ObserveAsync(params Task<string>[] tasks)
    {
        try { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { }
    }
}
