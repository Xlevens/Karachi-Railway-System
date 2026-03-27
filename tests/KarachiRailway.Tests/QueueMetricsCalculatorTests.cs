using KarachiRailway.Simulation.Engine;
using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Tests;

public class QueueMetricsCalculatorTests
{
    [Fact]
    public void Compute_MM1_MatchesMM1Calculator()
    {
        var expected = MM1Calculator.Compute(8, 10);
        var actual = QueueMetricsCalculator.Compute(QueueModelType.MM1, 8, 10, 1, 1);

        Assert.Equal(expected.Rho, actual.Rho, 10);
        Assert.Equal(expected.Lq, actual.Lq, 10);
        Assert.Equal(expected.L, actual.L, 10);
        Assert.Equal(expected.Wq, actual.Wq, 10);
        Assert.Equal(expected.W, actual.W, 10);
    }

    [Fact]
    public void Compute_MG1_Stable_ReturnsFiniteValues()
    {
        var metrics = QueueMetricsCalculator.Compute(QueueModelType.MG1, 6, 10, serviceCv: 1.4, arrivalCv: 1.0);

        Assert.True(metrics.Rho < 1.0);
        Assert.False(double.IsNaN(metrics.Wq));
        Assert.True(metrics.Wq >= 0);
    }

    [Fact]
    public void Compute_GG1_Stable_ReturnsFiniteValues()
    {
        var metrics = QueueMetricsCalculator.Compute(QueueModelType.GG1, 5, 9, serviceCv: 0.7, arrivalCv: 1.3);

        Assert.True(metrics.Rho < 1.0);
        Assert.False(double.IsNaN(metrics.L));
        Assert.True(metrics.L >= 0);
    }
}
