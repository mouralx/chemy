using Chemy.Core;
using Chemy.Core.Spectroscopy;
using Xunit;

namespace Chemy.Core.Tests;

public class SpectroscopyTests
{
    [Fact]
    public void Predict_Aspirin_ReturnsNmrAndIrBands()
    {
        var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
        var prediction = SpectroscopyEngine.Predict(aspirin);

        Assert.NotNull(prediction);
        Assert.NotEmpty(prediction.H1NmrPeaks);
        Assert.NotEmpty(prediction.C13NmrPeaks);
        Assert.Equal(9, prediction.C13NmrPeaks.Sum(p => p.HydrogenCount));
        Assert.NotEmpty(prediction.IrBands);

        Assert.Contains(prediction.IrBands, b => b.FunctionalGroup.Contains("Carboxylic Acid"));
    }
}
