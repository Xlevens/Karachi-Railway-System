using KarachiRailway.Simulation.Engine;
using KarachiRailway.Simulation.Models;

namespace KarachiRailway.Tests;

/// <summary>
/// Unit tests for the MM1 analytical calculator.
/// </summary>
public class MM1CalculatorTests
{
    [Fact]
    public void Compute_StableSystem_ReturnsPositiveMetrics()
    {
        var (rho, lq, l, wq, w) = MM1Calculator.Compute(lambda: 8, mu: 10);

        Assert.Equal(0.8, rho, precision: 10);
        Assert.True(lq > 0, "Lq should be positive");
        Assert.True(l  > 0, "L  should be positive");
        Assert.True(wq > 0, "Wq should be positive");
        Assert.True(w  > 0, "W  should be positive");
    }

    [Fact]
    public void Compute_KnownValues_MatchFormulas()
    {
        // λ=4, μ=5 → ρ=0.8, Lq = ρ²/(1-ρ) = 0.64/0.2 = 3.2
        var (rho, lq, l, wq, w) = MM1Calculator.Compute(4, 5);

        Assert.Equal(0.80, rho, precision: 10);
        Assert.Equal(3.2,  lq,  precision: 10);
        Assert.Equal(4.0,  l,   precision: 10);   // L = ρ/(1-ρ) = 4
        Assert.Equal(0.8,  wq,  precision: 10);   // Wq = Lq/λ = 3.2/4
        Assert.Equal(1.0,  w,   precision: 10);   // W  = L /λ  = 4/4
    }

    [Fact]
    public void Compute_UnstableSystem_ReturnsNaN()
    {
        var (rho, lq, _, _, _) = MM1Calculator.Compute(lambda: 10, mu: 8);

        Assert.True(rho > 1.0);
        Assert.True(double.IsNaN(lq));
    }

    [Fact]
    public void Compute_NegativeLambda_Throws()
    {
        Assert.Throws<ArgumentException>(() => MM1Calculator.Compute(-1, 10));
    }

    [Fact]
    public void Compute_ZeroMu_Throws()
    {
        Assert.Throws<ArgumentException>(() => MM1Calculator.Compute(5, 0));
    }

    [Fact]
    public void IsStable_ReturnsTrueWhenRhoLessThan1()
    {
        Assert.True(MM1Calculator.IsStable(5, 10));
        Assert.False(MM1Calculator.IsStable(10, 5));
        Assert.False(MM1Calculator.IsStable(10, 10));
    }
}
