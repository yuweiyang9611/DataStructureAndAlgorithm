using DataStructureAndAlgorithm.Scenarios.CityDelivery;

namespace DataStructureAndAlgorithm.Test.IntegratedProjectsTest;

/// <summary>固定输入边界、截止时间和发布顺序，避免仅检查最大派单数掩盖契约回归。</summary>
public sealed class CityDeliveryBoundaryTests
{
    private static CityDeliveryScenario Scenario() => new(
        0, DateTimeOffset.UnixEpoch,
        [new("A"), new("B")],
        [new("A", "B", 1, 1)],
        [new("V", "A", 1, [])],
        [new("O", "B", 1, DateTimeOffset.UnixEpoch.AddMinutes(5))]);

    [Fact]
    public void NumericBoundariesRejectInvalidValuesAndAcceptZeroWhereDocumented()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CityDeliveryPlanner(0));
        var planner = new CityDeliveryPlanner(1);
        var source = Scenario();
        Assert.Single(planner.Plan(source).Assignments);
        Assert.Throws<ArgumentOutOfRangeException>(() => planner.Plan(source with { MapVersion = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => planner.Plan(source with { Vehicles = [new("V", "A", -1, [])] }));
        Assert.Empty(planner.Plan(source with { Vehicles = [new("V", "A", 0, [])] }).Assignments);
        foreach (var volume in new[] { -1, 0 })
            Assert.Throws<ArgumentOutOfRangeException>(() => planner.Plan(source with { Orders = [source.Orders[0] with { Volume = volume }] }));
        Assert.Throws<ArgumentOutOfRangeException>(() => planner.Plan(source with { Orders = [source.Orders[0] with { Urgency = -1 }] }));
        foreach (var cost in new[] { -1d, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => planner.Plan(source with { Roads = [new("A", "B", 1, cost)] }));
        var zero = new CityDeliveryPlanner().Plan(source with { Roads = [new("B", "A", 0, 0)] });
        Assert.Equal(0, Assert.Single(zero.Assignments).TravelMinutes);
        Assert.Equal(new MaintenanceConnection("A", "B", 0), Assert.Single(zero.MaintenanceForest.Connections));
    }

    [Fact]
    public void IdentifiersAndReferencesAreValidatedBeforePlanning()
    {
        var planner = new CityDeliveryPlanner();
        var source = Scenario();
        foreach (var id in new[] { "", " ", " A", "A " })
        {
            Assert.Throws<ArgumentException>(() => planner.Plan(source with { Locations = [new(id)] }));
            Assert.Throws<ArgumentException>(() => planner.Plan(source with { Vehicles = [source.Vehicles[0] with { Skills = [id] }] }));
            Assert.Throws<ArgumentException>(() => planner.Plan(source with { Orders = [source.Orders[0] with { RequiredSkill = id }] }));
        }
        Assert.Equal("source", Assert.Throws<ArgumentException>(() => planner.Plan(source with { Vehicles = [source.Vehicles[0], source.Vehicles[0]] })).ParamName);
        Assert.Equal("source", Assert.Throws<ArgumentException>(() => planner.Plan(source with { Orders = [source.Orders[0], source.Orders[0]] })).ParamName);
        Assert.Equal("source", Assert.Throws<ArgumentException>(() => planner.Plan(source with { Vehicles = [source.Vehicles[0] with { StartLocationId = "missing" }] })).ParamName);
        Assert.Equal("source", Assert.Throws<ArgumentException>(() => planner.Plan(source with { Orders = [source.Orders[0] with { DestinationId = "missing" }] })).ParamName);
        foreach (var road in new[] { new DeliveryRoad("A", "A", 1, 1), new("missing", "B", 1, 1), new("A", "missing", 1, 1) })
            Assert.Equal("road", Assert.Throws<ArgumentException>(() => planner.Plan(source with { Roads = [road] })).ParamName);
        Assert.Throws<ArgumentException>(() => planner.Plan(source with { Roads = [source.Roads[0], source.Roads[0]] }));
        Assert.Throws<ArgumentException>(() => planner.Plan(source with { Roads = [source.Roads[0], new("B", "A", 2, 2)] }));
    }

    [Fact]
    public void DeadlineUsesArrivalTimeAndDisconnectedPairsSkipRouteSearch()
    {
        var source = Scenario();
        var late = new CityDeliveryPlanner().Plan(source with { Roads = [new("A", "B", 6, 1)] });
        Assert.Empty(late.Assignments);
        Assert.Single(late.UnassignedOrders);
        var exact = new CityDeliveryPlanner().Plan(source with { Roads = [new("A", "B", 5, 1)] });
        Assert.Equal(source.Orders[0].Deadline, Assert.Single(exact.Assignments).EstimatedArrival);
        var disconnected = new CityDeliveryPlanner().Plan(source with { Roads = [] });
        Assert.Empty(disconnected.Assignments);
        Assert.Equal(0, disconnected.RouteCacheMisses);
        Assert.Equal(2, disconnected.MaintenanceForest.ComponentCount);
    }

    [Fact]
    public void PublishedAssignmentsFollowDeadlineThenUrgencyThenIdentifier()
    {
        var source = Scenario();
        var time = source.PlanningTime;
        var plan = new CityDeliveryPlanner().Plan(source with
        {
            Vehicles = [new("V4", "A", 1, []), new("V2", "A", 1, []), new("V1", "A", 1, []), new("V3", "A", 1, [])],
            Orders =
            [
                new("late", "B", 1, time.AddMinutes(20), 99),
                new("z", "B", 1, time.AddMinutes(10), 1),
                new("urgent", "B", 1, time.AddMinutes(10), 2),
                new("a", "B", 1, time.AddMinutes(10), 1)
            ]
        });
        Assert.Equal(new[] { "urgent", "a", "z", "late" }, plan.Assignments.Select(item => item.OrderId));
    }
}
