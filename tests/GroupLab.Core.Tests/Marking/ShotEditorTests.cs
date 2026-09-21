using System.Collections.Immutable;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 2: click a hole and a small editor opens beside it, with move, delete, assign to a bull, the
/// marks, the hole size and a note, and every edit undoable.
/// <para>
/// <b>The tests are about what the popover says and what each action does to the marking</b>, because that is where the decisions are. Where
/// the popover is drawn, how it is animated and which side of the hole it opens on are a screen's business and carry no judgement worth
/// holding; what "flyer" does to a figure carries several.
/// </para>
/// </summary>
public class ShotEditorTests
{
    /// <summary>A sheet of three bulls an inch apart, the third a sighter, with a scale so an inch means something.</summary>
    private static MarkingState Sheet() => MarkingState.Empty with
    {
        Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
        Bulls =
        [
            new BullAim(1, "1", new PointD(100, 100)),
            new BullAim(2, "2", new PointD(300, 100)),
            new BullAim(3, "S1", new PointD(500, 100), Scoring: false),
        ],
    };

    [Fact]
    public void EditingAShotThatIsNoLongerThereGivesNoPanel()
    {
        Assert.Null(ShotEditor.For(Sheet(), 7));
    }

    /// <summary>The popover is headed with the name the rest of the application uses, so a person can see they opened the one they clicked.</summary>
    [Fact]
    public void ThePanelIsHeadedWithTheShotsOwnName()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(310, 105));

        Assert.Equal("2", ShotEditor.For(session.State, id)!.Title);
    }

    [Fact]
    public void EveryBullIsOfferedWithTheShotsOwnMarked()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(310, 105));

        var bulls = ShotEditor.For(session.State, id)!.Bulls;

        Assert.Equal(["bull1", "bull2", "bull3", "bullnone"], bulls.Select(b => b.Id));
        Assert.Equal("S1 (sighter)", bulls.Single(b => b.Id == "bull3").Text);
        Assert.Single(bulls, b => b.On);
        Assert.True(bulls.Single(b => b.Id == "bull2").On);
    }

    /// <summary>On a plain group there is nothing to assign to, and the popover offers nothing rather than offering "no bull" alone.</summary>
    [Fact]
    public void APlainGroupIsOfferedNoBulls()
    {
        var session = new MarkingSession(MarkingState.Empty with { Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1) });
        int id = session.AddShot(new PointD(10, 10));

        Assert.Empty(ShotEditor.For(session.State, id)!.Bulls);
    }

    /// <summary>
    /// The one judgement in this section worth holding with a test: a shot called out as a flyer is still in the group. Entry 131 section 2
    /// asks for "flyer (kept but called out)" beside an exclusion, and if the mark quietly dropped the shot the figures would shrink because
    /// somebody wanted a shot pointed at.
    /// </summary>
    [Fact]
    public void CallingAShotAFlyerChangesNoFigure()
    {
        var session = new MarkingSession(Sheet() with { ShotDistanceInches = 3600 });
        int[] ids = [.. new[] { new PointD(100, 100), new PointD(104, 100), new PointD(100, 104), new PointD(96, 100), new PointD(100, 96), new PointD(120, 130) }.Select(p => session.AddShot(p, 1))];

        var before = GroupAnalysis.Analyse(session.State);
        session.SetFlyer(ids[5], true);
        var after = GroupAnalysis.Analyse(session.State);

        Assert.True(session.State.Find(ids[5])!.Flyer);
        Assert.Equal(before.AllShots!.Shots, after.AllShots!.Shots);
        Assert.Equal(before.AllShots.MeanRadius!.Value, after.AllShots.MeanRadius!.Value, 12);
        Assert.Equal(0, after.Excluded);
    }

    /// <summary>And excluding one does change them, which is what makes the two marks worth having separately.</summary>
    [Fact]
    public void LeavingAShotOutDoesChangeTheFigures()
    {
        var session = new MarkingSession(Sheet() with { ShotDistanceInches = 3600 });
        int[] ids = [.. new[] { new PointD(100, 100), new PointD(104, 100), new PointD(100, 104), new PointD(96, 100), new PointD(100, 96), new PointD(120, 130) }.Select(p => session.AddShot(p, 1))];

        var before = GroupAnalysis.Analyse(session.State);
        session.SetExclusion(ids[5], ExclusionReason.CalledFlyer);
        var after = GroupAnalysis.Analyse(session.State);

        Assert.Equal(1, after.Excluded);
        Assert.NotEqual(before.AllShots!.MeanRadius!.Value, after.WithoutExclusions!.MeanRadius!.Value);
    }

    /// <summary>
    /// A shot marked a sighter by hand is set aside from the group, the same as one sitting on a sighter bull, because otherwise the mark
    /// would be decoration and the person would think they had taken it out when they had not.
    /// </summary>
    [Fact]
    public void AShotMarkedASighterLeavesTheGroup()
    {
        var session = new MarkingSession(MarkingState.Empty with
        {
            Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            ShotDistanceInches = 3600,
        });
        int[] ids = [.. new[] { new PointD(100, 100), new PointD(104, 100), new PointD(100, 104), new PointD(96, 100), new PointD(100, 96), new PointD(160, 170) }.Select(p => session.AddShot(p))];

        session.SetSighter(ids[5], true);
        var report = GroupAnalysis.Analyse(session.State);

        Assert.Equal(5, report.AllShots!.Shots);
        Assert.Equal(1, report.SighterShots);
    }

    /// <summary>
    /// And the sighters' own view measures it rather than setting it aside again. The flag that took the shot out of the group would take it
    /// out of the sighters too, and the one screen built to look at it would be empty.
    /// </summary>
    [Fact]
    public void TheSightersOwnViewMeasuresAShotMarkedBySighterByHand()
    {
        var session = new MarkingSession(MarkingState.Empty with
        {
            Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            ShotDistanceInches = 3600,
        });
        int[] ids = [.. new[] { new PointD(100, 100), new PointD(104, 100), new PointD(100, 104), new PointD(96, 100), new PointD(100, 96) }.Select(p => session.AddShot(p))];
        foreach (int id in ids)
        {
            session.SetSighter(id, true);
        }

        var sighters = GroupAnalysis.Analyse(GroupAnalysis.Sighters(session.State));

        Assert.Equal(5, sighters.AllShots!.Shots);
    }

    /// <summary>A shot already on a sighter bull is shown as one and the button does nothing, so the sheet and the mark cannot disagree.</summary>
    [Fact]
    public void AShotOnASighterBullIsAlreadyASighter()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(505, 100), 3);

        var sighter = ShotEditor.For(session.State, id)!.Marks.Single(m => m.Id == "sighter");

        Assert.True(sighter.On);
        Assert.False(sighter.Available);
        Assert.Equal("Sighter (this bull's)", sighter.Text);
    }

    /// <summary>
    /// An arrow key moves the shot by a hundredth of an inch on the paper, not by a pixel, so the same press means the same distance on a
    /// photograph taken from further away.
    /// </summary>
    [Fact]
    public void AnArrowKeyMovesAHundredthOfAnInchOnThePaper()
    {
        // 100 pixels to the inch.
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);

        session.NudgeShot(id, 1, 0, coarse: false);

        Assert.Equal(301, session.State.Find(id)!.Image.X, 9);
        Assert.Equal(100, session.State.Find(id)!.Image.Y, 9);
    }

    [Fact]
    public void ShiftMovesTenTimesAsFar()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);

        session.NudgeShot(id, 0, -1, coarse: true);

        Assert.Equal(90, session.State.Find(id)!.Image.Y, 9);
    }

    /// <summary>With no scale the step is a pixel, and the popover says so rather than printing inches it cannot work out.</summary>
    [Fact]
    public void WithNoScaleTheStepIsAPixelAndSaysSo()
    {
        var session = new MarkingSession(MarkingState.Empty);
        int id = session.AddShot(new PointD(40, 40));

        var steps = ShotEditor.Steps(session.State);
        Assert.Equal("px", steps.Unit);
        Assert.Contains("1 px a press", steps.Describe(), StringComparison.Ordinal);

        session.NudgeShot(id, -1, 0, coarse: false);
        Assert.Equal(39, session.State.Find(id)!.Image.X, 9);
    }

    /// <summary>A nudge is an edit like any other, so one press is one undo.</summary>
    [Fact]
    public void EveryNudgeIsOneUndoStep()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);
        session.NudgeShot(id, 1, 0, coarse: false);
        session.NudgeShot(id, 1, 0, coarse: false);

        session.Undo();
        Assert.Equal(301, session.State.Find(id)!.Image.X, 9);

        session.Undo();
        Assert.Equal(300, session.State.Find(id)!.Image.X, 9);

        session.Redo();
        Assert.Equal(301, session.State.Find(id)!.Image.X, 9);
    }

    /// <summary>A press of no direction is not an edit, so holding a modifier alone does not fill the undo stack with nothing.</summary>
    [Fact]
    public void APressThatMovesNothingIsNotAnEdit()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);
        bool before = session.CanUndo;

        session.NudgeShot(id, 0, 0, coarse: false);

        Assert.Equal(before, session.CanUndo);
    }

    /// <summary>
    /// A hole size set by hand does not erase what the detector measured. The measurement is evidence about the image, and a person
    /// disagreeing with it is a second opinion, not a reason to lose the first.
    /// </summary>
    [Fact]
    public void SettingTheHoleSizeKeepsWhatWasMeasured()
    {
        var session = new MarkingSession(Sheet() with
        {
            Shots = [new MarkedShot(1, new PointD(300, 100), ShotProvenance.Automatic, Bull: 2, MeasuredDiameterInches: 0.31)],
            NextId = 2,
        });

        session.SetHoleDiameter(1, 0.224);
        var shot = session.State.Find(1)!;

        Assert.Equal(0.31, shot.MeasuredDiameterInches);
        Assert.Equal(0.224, shot.ChosenDiameterInches);
        Assert.Equal(0.224, shot.DiameterInches);
        Assert.Contains("set by hand", ShotEditor.For(session.State, 1)!.HoleSize, StringComparison.Ordinal);

        session.SetHoleDiameter(1, null);
        Assert.Equal(0.31, session.State.Find(1)!.DiameterInches);
        Assert.Contains("measured", ShotEditor.For(session.State, 1)!.HoleSize, StringComparison.Ordinal);
    }

    [Fact]
    public void AHoleNobodyMeasuredSaysSo()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);

        Assert.Equal("not measured", ShotEditor.For(session.State, id)!.HoleSize);
    }

    [Fact]
    public void ANoteIsKeptTrimmedAndAnEmptyOneIsNoNote()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);

        session.SetNote(id, "  called low left  ");
        Assert.Equal("called low left", ShotEditor.For(session.State, id)!.Note);

        session.SetNote(id, "   ");
        Assert.Null(session.State.Find(id)!.Note);
    }

    /// <summary>The keys shown in the popover are the keys the editor answers to, one list, so the two cannot drift apart.</summary>
    [Fact]
    public void EveryShortcutShownHasAKeyAndEveryMarkHasOne()
    {
        Assert.All(ShotEditor.Shortcuts, a => Assert.False(string.IsNullOrWhiteSpace(a.Key), $"{a.Id} is shown as a shortcut with no key"));
        Assert.All(ShotEditor.Shortcuts, a => Assert.False(string.IsNullOrWhiteSpace(a.Text), $"{a.Id} is shown as a shortcut with no words"));

        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);
        var panel = ShotEditor.For(session.State, id)!;

        var keys = ShotEditor.Shortcuts.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        Assert.All(panel.Marks, m => Assert.Contains(m.Id, keys));

        // Entry 131 section 2 names Delete, the arrows, A for add and Escape, and undo and redo.
        foreach (string key in new[] { "Delete", "Escape", "A", "Ctrl+Z", "Ctrl+Y", "arrows" })
        {
            Assert.Contains(ShotEditor.Shortcuts, s => s.Key == key);
        }
    }

    /// <summary>The marks a shot carries are shown as pressed, which is how a person sees what has already been done to it.</summary>
    [Fact]
    public void TheMarksAShotCarriesAreShownAsOn()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);
        session.SetFlyer(id, true);
        session.SetExclusion(id, ExclusionReason.PulledShot);

        var marks = ShotEditor.For(session.State, id)!.Marks;

        Assert.True(marks.Single(m => m.Id == "flyer").On);
        Assert.True(marks.Single(m => m.Id == "exclude").On);
        Assert.False(marks.Single(m => m.Id == "notashot").On);
    }

    /// <summary>Adding mode says what clicking will do, which bull it is adding to, how many it has added and how to stop.</summary>
    [Fact]
    public void AddingModeSaysWhatItIsDoing()
    {
        var state = Sheet();
        var adding = AddingShots.For(2);

        Assert.Contains("to bull 2", adding.Describe(state), StringComparison.Ordinal);
        Assert.Contains("none yet", adding.Describe(state), StringComparison.Ordinal);
        Assert.Contains("Escape or Done", adding.Describe(state), StringComparison.Ordinal);

        adding = adding.With(4).With(5);
        Assert.Equal([4, 5], adding.Added);
        Assert.Contains("2 added", adding.Describe(state), StringComparison.Ordinal);
    }

    [Fact]
    public void AddingModeOnAPlainGroupAddsWhereYouClick()
    {
        Assert.Contains("where you click", AddingShots.For(null).Describe(MarkingState.Empty), StringComparison.Ordinal);
    }

    /// <summary>The marks survive a save and a reopen, or the note a person typed would be gone in the morning.</summary>
    [Fact]
    public void TheMarksAndTheNoteSurviveSavingAndReopening()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);
        session.SetFlyer(id, true);
        session.SetSighter(id, true);
        session.SetHoleDiameter(id, 0.224);
        session.SetNote(id, "called low left");

        var (state, notes) = MarkingFile.Read(MarkingFile.Write(session.State));

        Assert.Empty(notes);
        var shot = state.Find(id)!;
        Assert.True(shot.Flyer);
        Assert.True(shot.Sighter);
        Assert.Equal(0.224, shot.ChosenDiameterInches);
        Assert.Equal("called low left", shot.Note);
    }

    /// <summary>A marking written before any of this existed reopens with the marks off rather than failing to open.</summary>
    [Fact]
    public void AMarkingWrittenBeforeTheEditorReopens()
    {
        var session = new MarkingSession(Sheet());
        int id = session.AddShot(new PointD(300, 100), 2);
        string json = MarkingFile.Write(session.State)
            .Replace("\"flyer\": null,", "", StringComparison.Ordinal)
            .Replace("\"sighter\": null,", "", StringComparison.Ordinal)
            .Replace("\"chosenDiameterInches\": null,", "", StringComparison.Ordinal)
            .Replace("\"note\": null,", "", StringComparison.Ordinal);

        var (state, _) = MarkingFile.Read(json);
        var shot = state.Find(id)!;

        Assert.False(shot.Flyer);
        Assert.False(shot.Sighter);
        Assert.Null(shot.ChosenDiameterInches);
        Assert.Null(shot.Note);
    }
}
