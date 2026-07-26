using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Previews;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ExternalProcessPreviewRendererTests
{
    [Fact]
    public async Task RenderAsync_PublishesAllPreviewVariants()
    {
        using PreviewRendererFixture fixture = PreviewRendererFixture.Create(copyOutputs: true);
        ExternalProcessPreviewRenderer renderer = fixture.CreateRenderer();

        await renderer.RenderAsync(fixture.Cassette, fixture.Request);

        foreach (string variant in new[] { "small", "medium", "normal" })
        {
            string path = Path.Combine(
                fixture.Root,
                "documents",
                variant,
                "0001",
                "0001.txt");
            Assert.Equal("preview source", await File.ReadAllTextAsync(path));
        }
    }

    [Fact]
    public async Task RenderAsync_WhenRequestWasSuperseded_DoesNotRunRenderer()
    {
        using PreviewRendererFixture fixture = PreviewRendererFixture.Create(copyOutputs: true);
        await File.WriteAllTextAsync(fixture.OriginalPath, "newer source");
        ExternalProcessPreviewRenderer renderer = fixture.CreateRenderer();

        await renderer.RenderAsync(fixture.Cassette, fixture.Request);

        Assert.False(Directory.Exists(Path.Combine(fixture.Root, "documents", "small")));
    }

    [Fact]
    public async Task RenderAsync_WhenRendererReportsUnsupported_IsPermanentFailure()
    {
        using PreviewRendererFixture fixture = PreviewRendererFixture.Create(copyOutputs: false);
        ExternalProcessPreviewRenderer renderer = fixture.CreateRenderer();

        PreviewRenderingException exception = await Assert.ThrowsAsync<PreviewRenderingException>(
            () => renderer.RenderAsync(fixture.Cassette, fixture.Request));

        Assert.False(exception.Retryable);
    }

    private sealed class PreviewRendererFixture : IDisposable
    {
        private PreviewRendererFixture(
            string root,
            string originalPath,
            CassetteDefinition cassette,
            CassettePreviewRequest request,
            PreviewWorkerOptions options)
        {
            Root = root;
            OriginalPath = originalPath;
            Cassette = cassette;
            Request = request;
            Options = options;
        }

        public string Root { get; }
        public string OriginalPath { get; }
        public CassetteDefinition Cassette { get; }
        public CassettePreviewRequest Request { get; }
        public PreviewWorkerOptions Options { get; }

        public static PreviewRendererFixture Create(bool copyOutputs)
        {
            string root = Path.Combine(Path.GetTempPath(), "polar-preview-renderer-tests", Guid.NewGuid().ToString("N"));
            string originalDirectory = Path.Combine(root, "originals", "0001");
            Directory.CreateDirectory(originalDirectory);
            string originalPath = Path.Combine(originalDirectory, "0001.txt");
            byte[] content = "preview source"u8.ToArray();
            File.WriteAllBytes(originalPath, content);
            string script = WriteScript(root, copyOutputs);
            CassetteDefinition cassette = new()
            {
                Id = "current",
                Name = "Cassette",
                Path = root,
                Enabled = true,
                AllowWrite = true
            };
            CassettePreviewRequest request = new(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow,
                cassette.Id,
                cassette.Name,
                "iiss://Cassette@iis.nsk.su/0001/0001",
                "0001",
                "0001",
                "0001.txt",
                content.Length,
                Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(),
                Replaced: false);
            PreviewWorkerOptions options = CreateOptions(script);
            return new PreviewRendererFixture(root, originalPath, cassette, request, options);
        }

        public ExternalProcessPreviewRenderer CreateRenderer() => new(
            Microsoft.Extensions.Options.Options.Create(Options),
            NullLogger<ExternalProcessPreviewRenderer>.Instance);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static PreviewWorkerOptions CreateOptions(string script) => new()
        {
            Enabled = true,
            Executable = OperatingSystem.IsWindows()
                ? Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe"
                : "/bin/sh",
            PrefixArguments = OperatingSystem.IsWindows()
                ? ["/d", "/c", script]
                : [script],
            OutputExtension = "txt",
            RenderTimeoutSeconds = 30
        };

        private static string WriteScript(string root, bool copyOutputs)
        {
            string path = Path.Combine(root, OperatingSystem.IsWindows() ? "renderer.cmd" : "renderer.sh");
            string content = copyOutputs
                ? OperatingSystem.IsWindows()
                    ? "@echo off\r\ncopy /Y \"%~1\" \"%~2\" >nul\r\ncopy /Y \"%~1\" \"%~3\" >nul\r\ncopy /Y \"%~1\" \"%~4\" >nul\r\nexit /b 0\r\n"
                    : "#!/bin/sh\ncp \"$1\" \"$2\"\ncp \"$1\" \"$3\"\ncp \"$1\" \"$4\"\nexit 0\n"
                : OperatingSystem.IsWindows()
                    ? "@echo off\r\nexit /b 64\r\n"
                    : "#!/bin/sh\nexit 64\n";
            File.WriteAllText(path, content);
            return path;
        }
    }
}
