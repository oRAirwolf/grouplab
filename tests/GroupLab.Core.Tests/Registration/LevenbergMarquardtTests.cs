using GroupLab.Core.Registration;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// <see cref="LevenbergMarquardt"/> at a boundary beyond which the model is undefined, the stall PHASE1-RESULTS.md M1.10
/// found in a joint fit of general developable surfaces.
/// </summary>
public class LevenbergMarquardtTests
{
    [Fact]
    public void AParameterOnAnUndefinedBoundaryDoesNotStallTheOthers()
    {
        // Parameter 0 starts at its minimum, on a boundary beyond which the residual function returns a penalty, as a
        // general surface whose rulings are about to cross does; parameter 1 is free and three away from its minimum.
        static double Residuals(double[] p, double[] r)
        {
            if (p[0] > 0)
            {
                r[0] = r[1] = 1e6;
                return 2e12;
            }

            r[0] = p[0];
            r[1] = p[1] - 3;
            return (r[0] * r[0]) + (r[1] * r[1]);
        }

        var result = LevenbergMarquardt.Minimise(Residuals, [0, 0], 2, [1e-6, 1e-6]);

        Assert.True(Math.Abs(result.Parameters[1] - 3) < 1e-6, $"parameter 1 ended at {result.Parameters[1]}");
        Assert.True(result.Parameters[0] <= 0);
    }

    [Fact]
    public void AParameterPushingIntoAnUndefinedBoundaryIsHeldAndTheOthersStillMove()
    {
        // Parameter 0 wants to reach 1 but the model is undefined beyond 0, so every step that moves it forward is
        // rejected; held for the step, it no longer blocks parameter 1.
        static double Residuals(double[] p, double[] r)
        {
            if (p[0] > 0)
            {
                r[0] = r[1] = 1e6;
                return 2e12;
            }

            r[0] = p[0] - 1;
            r[1] = p[1] - 3;
            return (r[0] * r[0]) + (r[1] * r[1]);
        }

        var result = LevenbergMarquardt.Minimise(Residuals, [0, 0], 2, [1e-6, 1e-6]);

        Assert.True(Math.Abs(result.Parameters[1] - 3) < 1e-6, $"parameter 1 ended at {result.Parameters[1]}");
        Assert.True(result.Parameters[0] <= 0);
    }
}
