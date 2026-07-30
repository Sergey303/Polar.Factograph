using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class OntologyValidationServiceTests
{
    private const string O = "http://fogid.net/o/";
    private const string EntityRoot = O + "sys-obj";

    [Fact]
    public void Validate_ReportsBrokenHierarchyAndPropertyReferences()
    {
        OntologyTerm[] terms =
        [
            Class(EntityRoot, label: "Сущность", isAbstract: true),
            Class(O + "person", label: "Персона", parent: EntityRoot),
            Class(O + "broken", label: null, parent: O + "missing"),
            Property(
                O + "related",
                OntologyTermKind.ObjectProperty,
                domains: [O + "person"],
                ranges: [O + "missing-range"]),
            Property(
                O + "comment",
                OntologyTermKind.DatatypeProperty,
                domains: [],
                ranges: [])
        ];

        OntologyValidationReport report = new OntologyValidationService().Validate(terms);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_parent_class" && issue.TermId == O + "broken");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_range_class" && issue.TermId == O + "related");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_label" && issue.TermId == O + "broken");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_domain" && issue.TermId == O + "comment");
        Assert.Contains(report.Issues, issue =>
            issue.Code == "missing_range" && issue.TermId == O + "comment");
        Assert.True(report.ErrorCount >= 2);
        Assert.True(report.WarningCount >= 3);
    }

    [Fact]
    public void Validate_AcceptsAbstractRangeWithConcreteEntityDescendant()
    {
        OntologyTerm[] terms =
        [
            Class(EntityRoot, label: "Сущность", isAbstract: true),
            Class(
                O + "organization",
                label: "Организация",
                parent: EntityRoot,
                isAbstract: true),
            Class(
                O + "institute",
                label: "Институт",
                parent: O + "organization"),
            Class(O + "person", label: "Персона", parent: EntityRoot),
            Property(
                O + "works-at",
                OntologyTermKind.ObjectProperty,
                domains: [O + "person"],
                ranges: [O + "organization"],
                inverseLabel: "сотрудники")
        ];

        OntologyValidationReport report = new OntologyValidationService().Validate(terms);

        Assert.DoesNotContain(report.Issues, issue =>
            issue.Code == "no_concrete_entity_target");
        Assert.True(report.IsValid);
    }

    [Fact]
    public void Validate_ReportsResourceRangeWithoutPickerTarget()
    {
        OntologyTerm[] terms =
        [
            Class(EntityRoot, label: "Сущность", isAbstract: true),
            Class(O + "person", label: "Персона", parent: EntityRoot),
            Class(O + "relation", label: "Отношение"),
            Property(
                O + "relation-link",
                OntologyTermKind.ObjectProperty,
                domains: [O + "person"],
                ranges: [O + "relation"],
                inverseLabel: "обратная связь")
        ];

        OntologyValidationReport report = new OntologyValidationService().Validate(terms);

        OntologyValidationIssue issue = Assert.Single(report.Issues, value =>
            value.Code == "no_concrete_entity_target");
        Assert.Equal(OntologyValidationSeverities.Error, issue.Severity);
        Assert.Equal(O + "relation-link", issue.TermId);
    }

    [Fact]
    public void Validate_ReportsCyclicClassHierarchyWithoutBuildingCatalog()
    {
        OntologyTerm[] terms =
        [
            Class(EntityRoot, label: "Сущность", isAbstract: true),
            Class(O + "first", label: "Первый", parent: O + "second"),
            Class(O + "second", label: "Второй", parent: O + "first")
        ];

        OntologyValidationReport report = new OntologyValidationService().Validate(terms);

        OntologyValidationIssue issue = Assert.Single(report.Issues, value =>
            value.Code == "cyclic_class_hierarchy");
        Assert.Equal(OntologyValidationSeverities.Error, issue.Severity);
        Assert.Contains(O + "first", issue.Message, StringComparison.Ordinal);
        Assert.Contains(O + "second", issue.Message, StringComparison.Ordinal);
    }

    private static OntologyTerm Class(
        string id,
        string? label,
        string? parent = null,
        bool isAbstract = false) => new(
        id,
        OntologyTermKind.Class,
        label is null ? [] : [new OntologyLocalizedText(label, "ru")],
        [],
        Priority: null,
        ParentClassId: parent,
        Domains: [],
        Ranges: [],
        EnumerationStates: [])
    {
        IsAbstract = isAbstract
    };

    private static OntologyTerm Property(
        string id,
        OntologyTermKind kind,
        IReadOnlyList<string> domains,
        IReadOnlyList<string> ranges,
        string? inverseLabel = null) => new(
        id,
        kind,
        [new OntologyLocalizedText(id, "ru")],
        inverseLabel is null
            ? []
            : [new OntologyLocalizedText(inverseLabel, "ru")],
        Priority: null,
        ParentClassId: null,
        Domains: domains,
        Ranges: ranges,
        EnumerationStates: []);
}
