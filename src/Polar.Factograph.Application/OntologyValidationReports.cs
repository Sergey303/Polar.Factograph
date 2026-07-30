namespace Polar.Factograph.Application;

public static class OntologyValidationReports
{
    private const string DocumentTermId = "$ontology";

    public static OntologyValidationReport Fatal(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        OntologyValidationIssue issue = new(
            OntologyValidationSeverities.Error,
            code,
            DocumentTermId,
            message);
        return new OntologyValidationReport(
            TermCount: 0,
            ErrorCount: 1,
            WarningCount: 0,
            Issues: [issue]);
    }
}
