using Chemy.Core.Environmental;
using Xunit;

namespace Chemy.Core.Tests;

public class EcoCleanTests
{
    [Fact]
    public void SolveDegradationCascade_Pfas_GeneratesCascade()
    {
        var result = EcoCleanEngine.SolveDegradationCascade("PFOA C8HF15O2");

        Assert.NotNull(result);
        Assert.NotEmpty(result.DegradationCascade);
        Assert.Contains("PFAS", result.PollutantClass);
        Assert.Contains("not calculated", result.PossibleEndProducts);
        Assert.Contains("Does not calculate", result.MethodInfo.Warnings[0]);
    }
}
