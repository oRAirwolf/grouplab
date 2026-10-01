using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 331 section 2: chronograph files read into the same list of velocities the hand-entry box makes. The LabRadar
/// and Garmin Xero files here are written from those exports' documented layouts, not copied from anybody's file, so those two readers stay
/// Experimental until a real file passes.
/// </summary>
public class ChronographFilesTests
{
    [Fact]
    public void AGenericCsvTakesTheColumnItsHeaderNames()
    {
        var read = ChronographFiles.Read("Shot,Velocity (fps),Note\n1,2701.5,cold bore\n2,2695,\n3,2710.2,\nAverage,2702.2,\n");
        Assert.Equal(ChronographFormat.Generic, read.Format);
        Assert.Equal([2701.5, 2695, 2710.2, 2702.2], read.VelocitiesFps);
        Assert.Equal(1, read.Column);
        Assert.False(read.Experimental);
        Assert.Contains("named by its header", read.Said, StringComparison.Ordinal);
    }

    [Fact]
    public void MetresASecondAreConvertedWhereTheHeaderOrThePersonSaysSo()
    {
        var header = ChronographFiles.Read("n;speed m/s\n1;823,4\n2;825,0\n");
        Assert.Equal(823.4 / 0.3048, header.VelocitiesFps[0], 6);
        Assert.Contains("as its header says", header.Said, StringComparison.Ordinal);

        var chosen = ChronographFiles.Read("a,b\n1,820\n2,822\n", column: 1, metres: true);
        Assert.Equal(820 / 0.3048, chosen.VelocitiesFps[0], 6);
        Assert.Contains("the column you chose", chosen.Said, StringComparison.Ordinal);
    }

    [Fact]
    public void AFileWithNoHeaderRowIsReadFromItsFirstLine()
    {
        var read = ChronographFiles.Read("2701\n2695\n2710\n");
        Assert.Equal([2701, 2695, 2710], read.VelocitiesFps);
        Assert.Contains("taken as ft/s", read.Said, StringComparison.Ordinal);
    }

    [Fact]
    public void NoColumnOfVelocitiesAsksTheChoice()
    {
        var read = ChronographFiles.Read("name,group\nalpha,a\nbeta,b\n");
        Assert.Empty(read.VelocitiesFps);
        Assert.Null(read.Column);
        Assert.Equal(["name", "group"], read.Columns);
    }

    /// <summary>LabRadar's series report as its manual documents it: header lines, the velocity unit, then a table with V0.</summary>
    [Fact]
    public void ALabRadarReportGivesItsMuzzleVelocities()
    {
        const string report = "sep=;\nDevice ID;LBR-0000000;;\nSeries No;0007;;\nTotal number of shots;0003;;\nUnits velocity;fps;;\nUnits distances;yd;;\n\n" +
            "Shot ID;V0;V10;V20;Ke0;Date;Time;\n0001;2701,40;2690,10;2679,00;2833;30-09-2026;14:03:11;\n0002;2695,90;2684,70;2673,50;2822;30-09-2026;14:04:02;\n" +
            "0003;2710,20;2699,00;2687,90;2851;30-09-2026;14:05:20;\n";
        var read = ChronographFiles.Read(report);
        Assert.Equal(ChronographFormat.LabRadar, read.Format);
        Assert.Equal([2701.4, 2695.9, 2710.2], read.VelocitiesFps);
        Assert.True(read.Experimental);
        Assert.EndsWith(ChronographFiles.ExperimentalWords, read.Said, StringComparison.Ordinal);

        var metric = ChronographFiles.Read(report.Replace("Units velocity;fps", "Units velocity;m/s", StringComparison.Ordinal));
        Assert.Equal(2701.4 / 0.3048, metric.VelocitiesFps[0], 6);
    }

    /// <summary>Garmin Xero's ShotView export as documented: shots numbered from 1 under a SPEED column, then the session's averages.</summary>
    [Fact]
    public void AGarminXeroExportGivesItsShotsAndNotItsAverages()
    {
        const string export = "Load 41.5 gr\n#,SPEED (FPS),Δ AVG (FPS),KE (FT-LB),POWER FACTOR (kgr⋅ft/s),TIME,CLEAN BORE,COLD BORE,SHOT NOTES\n" +
            "1,2701.4,-1.1,2833,472.7,14:03:11,,,\n2,2695.9,-6.6,2822,471.8,14:04:02,,,\n3,2710.2,7.7,2851,474.3,14:05:20,,,\n-,,,,,,,,\n" +
            "AVERAGE SPEED,2702.5\nSTD DEV,7.2\nSPREAD,14.3\n";
        var read = ChronographFiles.Read(export);
        Assert.Equal(ChronographFormat.GarminXero, read.Format);
        Assert.Equal([2701.4, 2695.9, 2710.2], read.VelocitiesFps);
        Assert.True(read.Experimental);

        var metric = ChronographFiles.Read(export.Replace("SPEED (FPS)", "SPEED (M/S)", StringComparison.Ordinal).Replace("2701.4", "823.4", StringComparison.Ordinal));
        Assert.Equal(823.4 / 0.3048, metric.VelocitiesFps[0], 6);
    }

    /// <summary>Every reader ends in the list the hand-entry box takes, so the same reconciliation follows.</summary>
    [Fact]
    public void WhatAFileGivesTheHandEntryBoxReadsTheSame()
    {
        var read = ChronographFiles.Read("Velocity\n2701.5\n2695\n");
        var (typed, refusal) = Chronograph.Read(string.Join(", ", read.VelocitiesFps.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        Assert.Null(refusal);
        Assert.Equal(read.VelocitiesFps, typed);
    }
}
