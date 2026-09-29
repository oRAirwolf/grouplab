using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 259 screen 6, "the set as a checklist, A" (Alan's choice), for a set of sheets from Made for your optic: the
/// title "Your set, N of M read"; pooled tiles (mean radius so far, extreme spread, shots so far of the set's); a card listing every sheet,
/// read with its shots and time, or "still to read" in amber with an empty dashed box; the line saying sheets can be photographed in any
/// order; and "Photograph the next sheet" and "See the pooled group". The sheets are the saved sessions of this set's design shot on the same
/// day, pooled by the desktop's own <see cref="SetPool"/>, so a sheet read twice counts once.
/// </summary>
internal sealed class SetPage : UserControl
{
    public SetPage(TargetDefinition definition, string? shotDate, UnitSettings units, Action photograph, Action back)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var store = PhoneAnalysis.Store();
        var id = GroupLab.Core.Gltd.Binary.GltdBinary.Encode(definition).Encoding?.DefinitionId;
        var records = store.List().Where(s => s.SheetName == definition.Name && (shotDate is null || (s.ShotDate ?? s.CreatedUtc[..10]) == shotDate))
            .Select(s => store.Get(s.Id)).OfType<SessionRecord>().Where(r => r.DefinitionId == id).ToList();
        var states = records.Select(r => MarkingFile.Read(r.MarkingJson).State).Where(s => s.SetSheet is not null).ToList();
        var pooled = SetPool.Pool(definition, states);
        DiagnosticLog.Info("phone.set", ("sheets", states.Count), ("set", pooled.SetSize), ("missing", pooled.Missing.Count));

        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title(string.Create(CultureInfo.CurrentCulture, $"Your set, {pooled.Found.Count} of {pooled.SetSize} read")));
        if (pooled.Figures is { } figures)
        {
            var tiles = new List<(string, string, string, bool)>();
            if (figures.MeanRadius is { } mr)
            {
                tiles.Add(("Mean radius so far", units.Length(mr.Value), "", true));
            }

            if (figures.ExtremeSpread is { } es)
            {
                tiles.Add(("Extreme spread", units.Length(es.Value), "center to center", false));
            }

            tiles.Add(("Shots", string.Create(CultureInfo.CurrentCulture, $"{pooled.Shots} of {pooled.BullsInSet}"), "so far", false));
            column.Children.Add(Screens.Tiles(tiles));
        }

        var sheets = new StackPanel { Spacing = 8 };
        // A sheet's place in its set counts from 0, as SetPool keeps it; people count from 1.
        for (int k = 0; k < pooled.SetSize; k++)
        {
            int sheet = k + 1;
            var read = states.Where(s => s.SetSheet == k).ToList();
            bool here = pooled.Found.Contains(k);
            // Read, a green outline with a tick; still to read, an empty amber outline, dashed.
            var box = new Grid { Width = 44, Height = 56 };
            box.Children.Add(new Avalonia.Controls.Shapes.Rectangle
            {
                RadiusX = 4,
                RadiusY = 4,
                StrokeThickness = 1.5,
                Stroke = new SolidColorBrush(here ? Color.FromRgb(46, 160, 90) : Color.FromRgb(232, 150, 46)),
                StrokeDashArray = here ? null : new Avalonia.Collections.AvaloniaList<double> { 3, 2 },
            });
            if (here)
            {
                box.Children.Add(new TextBlock { Text = "✓", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            }
            var said = here
                ? Screens.Dim(read.Count > 0 ? string.Create(CultureInfo.CurrentCulture, $"{read[0].Shots.Count(s => s.IsShot)} shots, read") : "read")
                : new TextBlock { Text = "still to read", Foreground = new SolidColorBrush(Color.FromRgb(232, 150, 46)) };
            sheets.Children.Add(new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 12,
                Children = { box, new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center, Children = { Screens.Line($"Sheet {sheet}"), said } } },
            });
        }

        column.Children.Add(Screens.Card(sheets));
        column.Children.Add(Screens.Dim("Sheets can be photographed in any order. Each one names itself from its code."));
        column.Children.Add(Screens.Primary(pooled.Missing.Count > 0 ? "Photograph the next sheet" : "Photograph another sheet", photograph));
        var group = new StackPanel { Spacing = 6, IsVisible = false, Children = { Screens.Line(pooled.Said) } };
        column.Children.Add(Screens.Choice("See the pooled group", () => group.IsVisible = !group.IsVisible));
        column.Children.Add(group);
        column.Children.Add(Screens.Choice("Back to the result", back));
        Content = Screens.Page(column);
    }
}
