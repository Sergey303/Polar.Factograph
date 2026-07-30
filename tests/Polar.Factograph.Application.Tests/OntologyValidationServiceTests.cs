using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class OntologyValidationServiceTests
{
    [Fact]
    public void Validate_ReportsBrokenHierarchyAndPropertyReferences()
    {
        OntologyCatalog catalog = new(
        [
            Class("entity", label: "Сущность", isAbstract: true),
            Class("person", label: "Персона", parent: "entity", isEntity: true),
            Class("broken", label: null, parent: "missing"),
            Property(
                "related",
                OntologyTermKind.ObjectProperty,
                domains: ["person"],
                ranges: ["missing-range"]),
            Property(
                "comment",
                OntologyTermKind.DatatypeProperty,
                domains: [],
                ranges: [])
        ]);

        OntologyValidationReport report = new OntologyValidationService().Validate(catalog);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_parent_class" && issue.TermId == "broken");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_range_class" && issue.TermId == "related");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_label" && issue.TermId == "broken");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_domain" && issue.TermId == "comment");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_range" && issue.TermId == "comment");
        Assert.True(report.ErrorCount >= 2);
        Assert.True(report.WarningCount >= 3);
    }

    [Fact]
    public void Validate_AcceptsAbstractRangeWithConcreteEntityDescendant()
    {
        OntologyCatalog catalog = new(
        [
            Class("entity", label: "Сущность", isAbstract: true),
            Class(
                "organization",
                label: "Организация",
                parent: "entity",
                isAbstract: true),
            Class(
                "institute",
                label: "Институт",
                parent: "organization",
                isEntity: true),
            Class("person", label: "Персона", parent: "entity", isEntity: true),
            Property(
                "works-at",
                OntologyTermKind.ObjectProperty,
                domains: ["person"],
                ranges: ["organization"],
                inverseLabel: "сотрудники")
        ]);

        OntologyValidationReport report = new OntologyValidationService().Validate(catalog);

        Assert.DoesNotContain(report.Issues, issue =>
            issue.Code == "no_concrete_entity_target");
        Assert.True(report.IsValid);
    }

    [Fact]
    public void Validate_ReportsResourceRangeWithoutPickerTarget()
    {
        OntologyCatalog catalog = new(
        [
            Class("entity", label: "Сущность", isAbstract: true),
            Class("person", label: "Персона", parent: "entity", isEntity: true),
            Class("relation", label: "Отношение"),
            Property(
                "relation-link",
                OntologyTermKind.ObjectProperty,
                domains: ["person"],
                ranges: ["relation"],
                inverseLabel: "обратная связь")
        ]);

        OntologyValidationReport report = new OntologyValidationService().Validate(catalog);

        OntologyValidationIssue issue = Assert.Single(report.Issues, value =>
            value.Code == "no_concrete_entity_target");
        Assert.Equal(OntologyValidationSeverities.Error, issue.Severity);
        Assert.Equal("relation-link", issue.TermId);
    }

    private static OntologyTerm Class(
        string id,
        string? label,
        string? parent = null,
        bool isAbstract = false,
        bool isEntity = false) => new()
    {
        Id = id,
        Kind = OntologyTermKind.Class,
        Labels = label is null ? [] : [new LocalizedText("ru", label)],
        ParentClassId = parent,
        IsAbstract = isAbstract,
        IsEntityType = isEntity
    };

    private static OntologyTerm Property(
        string id,
        OntologyTermKind kind,
        IReadOnlyList<string> domains,
        IReadOnlyList<string> ranges,
        string? inverseLabel = null) => new()
    {
        Id = id,
        Kind = kind,
        Labels = [new LocalizedText("ru", id)],
        InverseLabels = inverseLabel is null
            ? []
            : [new LocalizedText("ru", inverseLabel)],
        Domains = domains,
        Ranges = ranges
    };
}
