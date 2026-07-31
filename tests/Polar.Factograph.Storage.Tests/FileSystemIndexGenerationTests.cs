using Xunit;

namespace Polar.Factograph.Storage.Tests;

public sealed class FileSystemIndexGenerationTests
{
    [Fact]
    public async Task CommitAsync_PublishesCompletedGeneration()
    {
        await using TemporaryDirectory directory = TemporaryDirectory.Create();
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await using FileSystemIndexGeneration generation = FileSystemIndexGeneration.Begin(directory.Path, id);

        await File.WriteAllTextAsync(Path.Combine(generation.StagingPath, "data.bin"), "ready");
        await generation.CommitAsync();

        Assert.False(Directory.Exists(generation.StagingPath));
        Assert.True(Directory.Exists(generation.FinalPath));
        Assert.Equal(
            generation.FinalPath,
            FileSystemIndexGeneration.GetCurrentGenerationPath(directory.Path));
    }

    [Fact]
    public async Task DisposeAsync_RemovesUncommittedStagingGeneration()
    {
        await using TemporaryDirectory directory = TemporaryDirectory.Create();
        string stagingPath;

        await using (FileSystemIndexGeneration generation = FileSystemIndexGeneration.Begin(directory.Path))
        {
            stagingPath = generation.StagingPath;
            await File.WriteAllTextAsync(Path.Combine(stagingPath, "partial.bin"), "partial");
        }

        Assert.False(Directory.Exists(stagingPath));
        Assert.Null(FileSystemIndexGeneration.GetCurrentGenerationPath(directory.Path));
    }

    [Fact]
    public async Task NewCommit_SwitchesCurrentPointerWithoutDeletingPreviousGeneration()
    {
        await using TemporaryDirectory directory = TemporaryDirectory.Create();

        string firstPath;
        await using (FileSystemIndexGeneration first = FileSystemIndexGeneration.Begin(
                         directory.Path,
                         Guid.Parse("22222222-2222-2222-2222-222222222222")))
        {
            firstPath = first.FinalPath;
            await first.CommitAsync();
        }

        string secondPath;
        await using (FileSystemIndexGeneration second = FileSystemIndexGeneration.Begin(
                         directory.Path,
                         Guid.Parse("33333333-3333-3333-3333-333333333333")))
        {
            secondPath = second.FinalPath;
            await second.CommitAsync();
        }

        Assert.True(Directory.Exists(firstPath));
        Assert.True(Directory.Exists(secondPath));
        Assert.Equal(secondPath, FileSystemIndexGeneration.GetCurrentGenerationPath(directory.Path));
    }

    [Fact]
    public async Task NewCommit_RetriesCurrentPointerWhileAnotherReaderTemporarilyBlocksReplacement()
    {
        await using TemporaryDirectory directory = TemporaryDirectory.Create();

        await using (FileSystemIndexGeneration first = FileSystemIndexGeneration.Begin(
                         directory.Path,
                         Guid.Parse("44444444-4444-4444-4444-444444444444")))
        {
            await first.CommitAsync();
        }

        string currentPath = Path.Combine(directory.Path, "CURRENT");
        using FileStream blockingReader = new(
            currentPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        await using FileSystemIndexGeneration second = FileSystemIndexGeneration.Begin(
            directory.Path,
            Guid.Parse("55555555-5555-5555-5555-555555555555"));
        Task commit = second.CommitAsync();

        await Task.Delay(150);
        blockingReader.Dispose();
        await commit;

        Assert.Equal(
            second.FinalPath,
            FileSystemIndexGeneration.GetCurrentGenerationPath(directory.Path));
    }

    private sealed class TemporaryDirectory : IAsyncDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "polar-factograph-index-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
