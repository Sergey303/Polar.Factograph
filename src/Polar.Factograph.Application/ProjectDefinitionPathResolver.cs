using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectDefinitionPathResolver
{
    public static ProjectDefinition Resolve(ProjectDefinition project, string baseDirectory) =>
        project with
        {
            Ontology = project.Ontology with
            {
                Path = ResolvePath(baseDirectory, project.Ontology.Path)
            },
            Index = project.Index with
            {
                Path = ResolvePath(baseDirectory, project.Index.Path)
            },
            Cassettes = project.Cassettes
                .Select(cassette => cassette with
                {
                    Path = ResolvePath(baseDirectory, cassette.Path)
                })
                .ToArray()
        };

    private static string ResolvePath(string baseDirectory, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDataException("A configured filesystem path is empty.");
        }

        return Path.IsPathRooted(path) || IsWindowsDrivePath(path)
            ? path
            : Path.GetFullPath(path, baseDirectory);
    }

    private static bool IsWindowsDrivePath(string path) =>
        path.Length >= 3 &&
        char.IsLetter(path[0]) &&
        path[1] == ':' &&
        (path[2] == '/' || path[2] == '\\');
}
