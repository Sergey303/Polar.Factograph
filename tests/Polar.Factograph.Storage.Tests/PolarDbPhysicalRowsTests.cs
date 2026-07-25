using System.Reflection;
using Xunit;

namespace Polar.Factograph.Storage.Tests;

public sealed class PolarDbPhysicalRowsTests
{
    private static readonly HashSet<Type> SupportedAutomaticTypes =
    [
        typeof(int),
        typeof(long),
        typeof(Guid),
        typeof(string),
        typeof(bool)
    ];

    [Fact]
    public void PhysicalRows_UseOnlyDbSetSupportedAutomaticTypes()
    {
        AssertSupportedConstructorTypes(typeof(PolarDbResourceHeadRow));
        AssertSupportedConstructorTypes(typeof(PolarDbTripleRow));
    }

    [Fact]
    public void ResourceHead_RoundTripsThroughPhysicalRow()
    {
        ResourceHead logical = new(
            "resource-1",
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "cassette-a",
            "/data/a.fog",
            new DateTimeOffset(2026, 7, 25, 10, 30, 0, TimeSpan.FromHours(3)),
            IsDeleted: false);

        PolarDbResourceHeadRow physical = PolarDbRowMapper.ToPhysical(logical);
        ResourceHead restored = PolarDbRowMapper.ToLogical(physical);

        Assert.Equal(logical.ResourceId, restored.ResourceId);
        Assert.Equal(logical.CurrentSourceRecordId, restored.CurrentSourceRecordId);
        Assert.Equal(logical.SourceCassetteId, restored.SourceCassetteId);
        Assert.Equal(logical.SourceFogPath, restored.SourceFogPath);
        Assert.Equal(logical.ModifiedAt.UtcDateTime, restored.ModifiedAt.UtcDateTime);
        Assert.False(restored.IsDeleted);
    }

    [Fact]
    public void Triple_RoundTripsAndBuildsStableCompositeKeys()
    {
        TripleRow logical = new(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "subject",
            "predicate",
            TripleObjectKind.Literal,
            "value",
            Language: null,
            DataType: "datatype",
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "cassette-a",
            "/data/a.fog",
            new DateTimeOffset(2026, 7, 25, 8, 0, 0, TimeSpan.Zero));

        PolarDbTripleRow first = PolarDbRowMapper.ToPhysical(logical);
        PolarDbTripleRow second = PolarDbRowMapper.ToPhysical(logical);
        TripleRow restored = PolarDbRowMapper.ToLogical(first);

        Assert.Equal(first, second);
        Assert.Equal("7:subject9:predicate", first.SubjectPredicateKey);
        Assert.Equal("9:predicate1:25:value", first.PredicateObjectKey);
        Assert.Null(restored.Language);
        Assert.Equal("datatype", restored.DataType);
        Assert.Equal(logical.ModifiedAt, restored.ModifiedAt);
    }

    [Fact]
    public void CompositeKeys_DoNotCollideWhenPartsHaveDifferentBoundaries()
    {
        string first = PolarDbCompositeKey.Create("ab", "c");
        string second = PolarDbCompositeKey.Create("a", "bc");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void InvalidStoredObjectKind_IsRejected()
    {
        PolarDbTripleRow physical = new(
            Guid.NewGuid(),
            "subject",
            "predicate",
            ObjectKind: 999,
            "value",
            string.Empty,
            string.Empty,
            Guid.NewGuid(),
            "cassette-a",
            "/data/a.fog",
            DateTime.UnixEpoch.Ticks,
            PolarDbCompositeKey.Create("subject", "predicate"),
            PolarDbCompositeKey.Create("predicate", "999", "value"));

        Assert.Throws<InvalidDataException>(() => PolarDbRowMapper.ToLogical(physical));
    }

    private static void AssertSupportedConstructorTypes(Type rowType)
    {
        ConstructorInfo constructor = Assert.Single(rowType.GetConstructors());

        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            Assert.Contains(parameter.ParameterType, SupportedAutomaticTypes);
        }
    }
}
