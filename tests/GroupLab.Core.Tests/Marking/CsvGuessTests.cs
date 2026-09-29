using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 278 section 2, CSV B: GroupLab guesses which column is across, which is up and down, the unit, which way is up
/// and where the numbers are measured from, says why for each, and says so where it cannot guess.
/// </summary>
public class CsvGuessTests
{
    private const string FiveShots = "1,-0.21,0.34\n2,0.18,-0.12\n3,0.05,0.27\n4,-0.30,-0.08\n5,0.26,0.02\n";

    [Fact]
    public void HeadingsThatSayTheUnitAreRead()
    {
        var guess = CsvGuess.For(ShotCsv.Read("shot,x_in,y_in\n" + FiveShots));
        Assert.Equal(1, guess.Across.Value);
        Assert.Equal(2, guess.UpDown.Value);
        Assert.Equal(CoordinateUnit.Inch, guess.Unit.Value);
        Assert.Contains("x_in", guess.Unit.Words, StringComparison.Ordinal);
        Assert.True(guess.Complete);
    }

    [Theory]
    [InlineData("Horizontal (mm),Vertical (mm)", CoordinateUnit.Millimetre)]
    [InlineData("x cm,y cm", CoordinateUnit.Centimetre)]
    [InlineData("Windage MOA,Elevation MOA", CoordinateUnit.Moa)]
    [InlineData("x [mrad],y [mrad]", CoordinateUnit.Mil)]
    [InlineData("x mils,y mils", CoordinateUnit.Mil)]
    [InlineData("across inches,up inches", CoordinateUnit.Inch)]
    public void EveryUnitIsFoundInItsHeading(string headings, CoordinateUnit unit)
    {
        var guess = CsvGuess.For(ShotCsv.Read(headings + "\n1.2,0.4\n-0.3,0.9\n0.1,-1.1\n"));
        Assert.Equal(unit, guess.Unit.Value);
        Assert.Equal(0, guess.Across.Value);
        Assert.Equal(1, guess.UpDown.Value);
    }

    [Fact]
    public void TheFileNameCanSayTheUnit()
    {
        var guess = CsvGuess.For(ShotCsv.Read("x,y\n1.2,0.4\n-0.3,0.9\n0.1,-1.1\n"), "range day group_mm.csv");
        Assert.Equal(CoordinateUnit.Millimetre, guess.Unit.Value);
        Assert.Contains("file's name", guess.Unit.Words, StringComparison.Ordinal);
    }

    [Fact]
    public void LargeNumbersWithNoUnitAreTakenAsMillimetersAndSaySo()
    {
        var guess = CsvGuess.For(ShotCsv.Read("x,y\n-5.3,8.6\n4.6,-3.0\n1.3,6.9\n-7.6,-2.0\n6.6,0.5\n21.5,-1.2\n"));
        Assert.Equal(CoordinateUnit.Millimetre, guess.Unit.Value);
        Assert.Contains("Check it", guess.Unit.Words, StringComparison.Ordinal);
    }

    [Fact]
    public void SmallNumbersWithNoUnitAreNotGuessedAndTheLineAsks()
    {
        var guess = CsvGuess.For(ShotCsv.Read("x,y\n" + string.Join("\n", FiveShots.Split('\n').Where(l => l.Length > 0).Select(l => l[2..]))));
        Assert.Null(guess.Unit.Value);
        Assert.Contains("Choose it", guess.Unit.Words, StringComparison.Ordinal);
        Assert.False(guess.Complete);
    }

    [Fact]
    public void HeadingsThatNameNoAxisFallBackToTheColumnsOfNumbersPastAShotCount()
    {
        var guess = CsvGuess.For(ShotCsv.Read("#,a (in),b (in)\n" + FiveShots));
        Assert.Equal(1, guess.Across.Value);
        Assert.Equal(2, guess.UpDown.Value);
        Assert.Contains("Check it", guess.Across.Words, StringComparison.Ordinal);
    }

    [Fact]
    public void AFlippedVerticalIsReadFromItsHeading()
    {
        var guess = CsvGuess.For(ShotCsv.Read("x (in),y down (in)\n0.1,0.5\n-0.2,0.1\n0.3,-0.2\n"));
        Assert.False(guess.UpIsPositive.Value);
        var (offsets, _) = ShotCsv.Shots(ShotCsv.Read("x (in),y down (in)\n0.1,0.5\n"), 0, 1, CoordinateUnit.Inch, guess.UpIsPositive.Value!.Value, null);
        Assert.Equal(0.5, offsets[0].Y, 9); // on the screen's axes, down positive: half an inch low
    }

    [Fact]
    public void NoWordForTheDirectionTakesALargerNumberAsHigherAndSaysToChangeIt()
    {
        var guess = CsvGuess.For(ShotCsv.Read("x (in),y (in)\n0.1,0.5\n-0.2,0.1\n0.3,-0.2\n"));
        Assert.True(guess.UpIsPositive.Value);
        Assert.Contains("upside down", guess.UpIsPositive.Words, StringComparison.Ordinal);
    }

    [Fact]
    public void NumbersAveragingZeroLookMeasuredFromTheGroupsCentre()
    {
        var centred = CsvGuess.For(ShotCsv.Read("x (in),y (in)\n-0.2,0.3\n0.2,0.1\n0.1,-0.3\n-0.1,-0.1\n"));
        Assert.True(centred.FromGroupCentre.Value);
        var fromAim = CsvGuess.For(ShotCsv.Read("x (in),y (in)\n0.8,1.3\n1.2,1.1\n1.1,0.7\n0.9,0.9\n"));
        Assert.False(fromAim.FromGroupCentre.Value);
        var said = CsvGuess.For(ShotCsv.Read("x from aim (in),y from aim (in)\n-0.2,0.3\n0.2,0.1\n0.1,-0.3\n-0.1,-0.1\n"));
        Assert.False(said.FromGroupCentre.Value);
        Assert.Contains("as the file says", said.FromGroupCentre.Words, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupLabsOwnExportIsReadBackWithoutAQuestion()
    {
        var state = ShotCsv.Marking([new(0.5, -0.25), new(-0.25, 0.5), new(1, 1)], 3600);
        var guess = CsvGuess.For(ShotCsv.Read(ShotCsv.Write(state)), "group.csv");
        Assert.True(guess.Complete);
        Assert.Equal(CoordinateUnit.Inch, guess.Unit.Value);
        Assert.True(guess.UpIsPositive.Value);
        Assert.False(guess.FromGroupCentre.Value);
    }

    [Fact]
    public void NumbersFromTheGroupsCentreImportWithNoPointOfAim()
    {
        var state = ShotCsv.Marking([new(0.5, -0.25), new(-0.5, 0.25)], null, fromGroupCentre: true);
        Assert.Null(state.PointOfAim);
        Assert.Equal(2, state.Shots.Count);
    }
}
