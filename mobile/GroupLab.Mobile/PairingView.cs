using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Records;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 351, pairing A on the phone ("Pairing A: a row per reading, tap to change"): a card stating the proposal and
/// its reasons; a row per reading in the order fired, its number, speed in the string's own unit and time, and on the right one mark saying
/// what it goes with, teal when paired, amber when it needs a look, plain for another group's; a pause between readings as a thin labelled
/// divider. Tapping a mark opens a sheet from the bottom with the choices the computer's rows offer in the same words. At the bottom,
/// "Keep this pairing" and "Leave unpaired".
/// </summary>
internal sealed class PairingView : UserControl
{
    private readonly ChronographMarks marks;
    private readonly Action<IReadOnlyList<ChronographPair>?> keep;
    private readonly Action back;
    private readonly StackPanel rows = new();
    private readonly TextBlock said = Screens.Line("");
    private readonly Border sheet;
    private readonly Border scrim;
    private readonly StackPanel sheetColumn = new() { Spacing = 0 };
    private int? changing;
    private bool choosingShot;

    /// <param name="keep">Keeps the readings with the pairing given, or with none for "Leave unpaired".</param>
    public PairingView(ChronographMarks marks, Action<IReadOnlyList<ChronographPair>?> keep, Action back)
    {
        this.marks = marks ?? throw new ArgumentNullException(nameof(marks));
        this.keep = keep ?? throw new ArgumentNullException(nameof(keep));
        this.back = back ?? throw new ArgumentNullException(nameof(back));
        scrim = new Border { IsVisible = false };
        scrim.PointerPressed += (_, e) =>
        {
            Close();
            e.Handled = true;
        };
        sheet = new Border
        {
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Bottom,
            CornerRadius = new CornerRadius(16, 16, 0, 0),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 10, 0, 16),
            Child = sheetColumn,
        }.Id("chrono-sheet");
        AutomationProperties.SetName(sheet, "What the reading goes with");
        ActualThemeVariantChanged += (_, _) => Fill();
        Fill();
    }

    /// <summary>The marks as they stand, for the tests.</summary>
    internal ChronographMarks Marks => marks;

    /// <summary>The reading whose sheet is open, or null.</summary>
    internal int? Changing => changing;

    internal string Said => said.Text ?? "";

    private void Fill()
    {
        var palette = Tokens.For(ActualThemeVariant);
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Readings and shots"));
        column.Children.Add(Screens.Card(
            new TextBlock { Text = marks.Headline, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap },
            Screens.Quiet(marks.Says(touch: true))).Id("chrono-proposal"));

        var head = Row("#", marks.Unit, marks.Timed ? "time" : "", new TextBlock { Text = "goes with", HorizontalAlignment = HorizontalAlignment.Right, Classes = { PhoneStyles.Dim } }, header: true);
        column.Children.Add(head);
        FillRows(palette);
        column.Children.Add(new Border { Child = Screens.Detach(rows), Classes = { PhoneStyles.Card }, Padding = new Thickness(0) });
        column.Children.Add(Screens.Detach(said));

        var foot = new StackPanel { Spacing = 8, Margin = new Thickness(16, 12, 16, 16) };
        foot.Children.Add(Screens.Primary(ChronographMarks.KeepWords, () => keep(marks.Pairs())).Id("chrono-keep-paired"));
        foot.Children.Add(Screens.Choice(ChronographMarks.UnpairedWords, () => keep(null)).Id("chrono-keep-unpaired"));
        foot.Children.Add(Screens.Choice("Back to the list", back).Id("chrono-pairing-back"));
        var footBorder = new Border { Child = foot, BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = new SolidColorBrush(palette.Line) };
        var dock = new DockPanel();
        DockPanel.SetDock(footBorder, Dock.Bottom);
        dock.Children.Add(footBorder);
        dock.Children.Add(Screens.Page(column));

        scrim.Background = new SolidColorBrush(Tokens.Scrim);
        sheet.Background = new SolidColorBrush(palette.Panel);
        sheet.BorderBrush = new SolidColorBrush(palette.Line2);
        Content = new Grid { Children = { dock, Screens.Detach(scrim), Screens.Detach(sheet) } };
        FillSheet();
    }

    private void FillRows(Palette palette)
    {
        rows.Children.Clear();
        var pauses = marks.Pauses.ToDictionary(p => p.After);
        for (int i = 0; i < marks.Count; i++)
        {
            int reading = i;
            var mark = new Button
            {
                Content = new TextBlock { Text = marks.Label(i), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Right },
                MinHeight = 44,
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(12, 4),
                Classes = { AppStyles.ReadingMark },
            }.Id(string.Create(CultureInfo.InvariantCulture, $"chrono-mark-{i + 1}"));
            switch (marks.Tone(i))
            {
                case MarkTone.Paired:
                    mark.Classes.Add(AppStyles.Good);
                    break;
                case MarkTone.NeedsLook:
                    mark.Classes.Add(AppStyles.Warn);
                    break;
            }

            AutomationProperties.SetName(mark, string.Create(CultureInfo.InvariantCulture, $"Reading {i + 1}, {marks.Speed(i)} {marks.Unit}: {marks.Label(i)}. Change it"));
            mark.Click += (_, _) => Open(reading);
            var row = Row((i + 1).ToString(CultureInfo.InvariantCulture), marks.Speed(i), marks.Time(i), mark, header: false);
            if (changing == i)
            {
                row.Background = new SolidColorBrush(palette.AmberTint);
            }

            if (i > 0)
            {
                row.BorderBrush = new SolidColorBrush(palette.Line);
                row.BorderThickness = new Thickness(0, 1, 0, 0);
            }

            rows.Children.Add(row);
            if (pauses.TryGetValue(i, out var pause) && i < marks.Count - 1)
            {
                rows.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush(palette.Line),
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Padding = new Thickness(0, 4),
                    Child = new TextBlock { Text = pause.Words, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 12, Classes = { PhoneStyles.Dim } },
                }.Id(string.Create(CultureInfo.InvariantCulture, $"chrono-pause-{i + 1}")));
            }
        }
    }

    /// <summary>One row: the reading's number, speed and time on the left, its mark on the right, which takes what room is left and wraps.</summary>
    private static Border Row(string number, string speed, string time, Control mark, bool header)
    {
        TextBlock Cell(string text, double width) => new()
        {
            Text = text,
            Width = width,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = header ? Tokens.Sans : Tokens.Mono,
            Classes = { PhoneStyles.Dim },
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,*"), ColumnSpacing = 8 };
        var cells = new Control[] { Cell(number, 26), Cell(speed, 58), Cell(time, time.Length == 0 ? 0 : 46), mark };
        if (!header)
        {
            ((TextBlock)cells[1]).Classes.Remove(PhoneStyles.Dim);
        }

        for (int c = 0; c < cells.Length; c++)
        {
            Grid.SetColumn(cells[c], c);
            grid.Children.Add(cells[c]);
        }

        mark.VerticalAlignment = VerticalAlignment.Center;
        return new Border { Child = grid, Padding = new Thickness(12, header ? 0 : 4), MinHeight = header ? 0 : 52 };
    }

    /// <summary>Opens the sheet for a reading.</summary>
    internal void Open(int reading)
    {
        changing = reading;
        choosingShot = false;
        DiagnosticLog.Info("chronograph.mark.open", ("reading", reading + 1));
        FillRows(Tokens.For(ActualThemeVariant));
        FillSheet();
    }

    internal void Close()
    {
        changing = null;
        choosingShot = false;
        FillRows(Tokens.For(ActualThemeVariant));
        FillSheet();
    }

    /// <summary>A choice of the sheet, as tapping it does: the mark set, the sheet closed and what changed said.</summary>
    internal void Choose(ReadingGoesWith kind, int? shot = null)
    {
        if (changing is not { } reading)
        {
            return;
        }

        if (kind == ReadingGoesWith.Shot && shot is null)
        {
            choosingShot = true;
            FillSheet();
            return;
        }

        string? words = marks.Set(reading, kind, shot);
        DiagnosticLog.Info("chronograph.mark", ("reading", reading + 1), ("kind", kind.ToString()), ("swap", words?.Contains("swapped", StringComparison.Ordinal) == true));
        said.Text = words ?? "";
        Close();
        Fill();
    }

    private void FillSheet()
    {
        sheetColumn.Children.Clear();
        bool open = changing is not null;
        sheet.IsVisible = open;
        scrim.IsVisible = open;
        if (changing is not { } reading)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        sheetColumn.Children.Add(new Border { Width = 40, Height = 4, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(palette.Line2), Margin = new Thickness(0, 0, 0, 10) });
        sheetColumn.Children.Add(new TextBlock { Text = marks.Asks(reading), Margin = new Thickness(16, 0, 16, 8), TextWrapping = TextWrapping.Wrap, Classes = { PhoneStyles.Dim } });
        var now = marks[reading];
        foreach (var (kind, words) in ChronographMarks.Choices)
        {
            bool current = now.Kind == kind;
            string text = kind == ReadingGoesWith.Shot ? words + "…" : words;
            sheetColumn.Children.Add(Choice(text, current, () => Choose(kind)).Id("chrono-choice-" + kind.ToString().ToLowerInvariant()));
            if (kind == ReadingGoesWith.Shot && choosingShot)
            {
                var shots = new WrapPanel { Margin = new Thickness(12, 0, 12, 8) };
                foreach (int shot in marks.Shots)
                {
                    int at = shot;
                    int? holder = marks.ReadingOf(shot);
                    string name = "Shot " + ShotName(shot);
                    var button = new Button
                    {
                        Content = new TextBlock { TextWrapping = TextWrapping.Wrap, Text = holder is { } h && h != reading ? string.Create(CultureInfo.InvariantCulture, $"{name}, reading {h + 1}") : name },
                        MinHeight = 44,
                        Margin = new Thickness(4),
                    }.Id("chrono-shot-" + ShotName(shot));
                    if (now.Kind == ReadingGoesWith.Shot && now.ShotId == shot)
                    {
                        button.Classes.Add(AppStyles.Chosen);
                    }

                    AutomationProperties.SetName(button, holder is { } o && o != reading
                        ? string.Create(CultureInfo.InvariantCulture, $"{name}, which reading {o + 1} has; the two swap")
                        : name);
                    button.Click += (_, _) => Choose(ReadingGoesWith.Shot, at);
                    shots.Children.Add(button);
                }

                sheetColumn.Children.Add(new ScrollViewer { Content = shots, MaxHeight = 220, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
            }
        }

        sheetColumn.Children.Add(Choice("Cancel", false, Close).Id("chrono-choice-cancel"));
    }

    private string ShotName(int shot) => marks.ShotName(shot);

    private static Button Choice(string words, bool current, Action chosen)
    {
        var dot = new TextBlock { Text = current ? "●" : "", Width = 20, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Warn } }; // one line on purpose: a dot
        var label = new TextBlock { Text = words, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        var content = new DockPanel { Children = { dot, label } };
        DockPanel.SetDock(dot, Dock.Left);
        var button = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            MinHeight = 52,
            Padding = new Thickness(16, 4),
            Classes = { PhoneStyles.Row },
        };
        AutomationProperties.SetName(button, current ? words + ", chosen now" : words);
        button.Click += (_, _) => chosen();
        return button;
    }
}
