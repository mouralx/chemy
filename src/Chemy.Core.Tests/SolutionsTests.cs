using Chemy.Core.Solutions;
using Xunit;

namespace Chemy.Core.Tests;

public class SolutionsTests
{
    [Fact]
    public void CalculateStrongAcidPh_0Point1M_CalculatesPhOne()
    {
        var result = SolutionsEngine.CalculateStrongAcidPh(0.1);

        Assert.Equal(1.0, result.Ph, precision: 2);
        Assert.Equal(13.0, result.Poh, precision: 2);
        Assert.True(result.IsAcidic);
        Assert.False(result.IsBasic);
    }

    [Fact]
    public void CalculateBufferPh_EqualConcentrations_EqualsPka()
    {
        // Henderson-Hasselbalch: pH = pKa + log(1) = pKa
        var buffer = SolutionsEngine.CalculateBufferPh(pka: 4.76, acidConcentrationMolar: 0.1, conjugateBaseConcentrationMolar: 0.1);

        Assert.Equal(4.76, buffer.Ph, precision: 2);
    }

    [Fact]
    public void CalculateStrongAcidPh_UltraDilute_DoesNotExceedSeven()
    {
        // 1.0e-8 M HCl gives pH ~ 6.98 due to Kw water autodissociation, NOT pH 8.0
        var result = SolutionsEngine.CalculateStrongAcidPh(1.0e-8);
        Assert.True(result.Ph < 7.0);
        Assert.Equal(6.98, result.Ph, precision: 2);
    }
}
