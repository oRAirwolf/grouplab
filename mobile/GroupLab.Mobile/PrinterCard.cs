using System.Globalization;
using Avalonia.Controls;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// Real inches on photographs, NOTES-FROM-PLANNING.md entry 271, on the phone's result: the one line that says what the figures are measured
/// in, a scan's offer to keep its scale for the printer, and on a photograph "Measure this sheet with a ruler", for this sheet or to keep.
/// </summary>
internal static class PrinterCard
{
    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>The card for this result, or null where the sheet was not registered from its markers.</summary>
    public static Control? For(PhoneResult result, MarkingSession session, Action changed)
    {
        if (session.State.Scale is not SheetReference sheet || result.Definition is not { } definition)
        {
            return null;
        }

        var said = Screens.Dim(sheet.ScaleFrom ?? (sheet.RealInches ? "" : DetectionAdvice.SheetInches));
        said.IsVisible = said.Text!.Length > 0;
        var card = new StackPanel { Spacing = 8, Children = { said } };
        // Entry 273 section 5: the paper's own edge, as a check on the profile or on a sheet printed with Fit to page.
        if (PaperEdgeCheck.Advice(result.Paper, Phone.Settings.PrinterForPhotos()) is { } edge)
        {
            card.Children.Add(Screens.Line(edge));
        }

        if (PrinterProfile.FromScan(null, result.Measured, Today()) is { } measured)
        {
            var name = new TextBox { MinHeight = Screens.Touch, Text = Phone.Settings.LoadChosenPrinter()?.Name ?? PrinterProfile.DefaultName };
            var offer = Screens.Line(PrinterProfile.Offer(measured.Scale));
            Button? use = null;
            use = Screens.Choice("Use it for photos", () =>
            {
                var kept = measured with { Name = string.IsNullOrWhiteSpace(name.Text) ? PrinterProfile.DefaultName : name.Text.Trim() };
                Phone.Settings.SavePrinter(kept);
                offer.Text = string.Create(CultureInfo.CurrentCulture, $"Kept. Photographs of sheets from {kept.Name} are now corrected for its {kept.Scale * 100:0.0} percent.");
                name.IsVisible = use!.IsVisible = false;
            });
            card.Children.Add(offer);
            card.Children.Add(name);
            card.Children.Add(use);
        }
        else if (sheet.ScaleFrom is { } from && Phone.Settings.PrinterForPhotos() is { } printer && from == printer.Line && printer.Stale(Today()) is { } stale)
        {
            // Entry 291 section 5.2: corrected by a check the printer may have outgrown, which the result says, with the check beside it.
            card.Children.Add(Screens.Line(stale));
            card.Children.Add(Screens.Choice("Check your printer", () => Shell.Current?.ShowPrinterCheck(printer.Name)));
        }
        else if (!sheet.RealInches && RulerSpan.Of(definition) is { } span)
        {
            // Entry 273 section 5: "Measured in the sheet's own inches", with the way to real inches beside it.
            card.Children.Add(Screens.Choice("Check your printer", () => Shell.Current?.ShowPrinterCheck()));
            var form = new StackPanel { Spacing = 8, IsVisible = false };
            var reading = new TextBox { MinHeight = Screens.Touch, PlaceholderText = "5 3/4 or 146 mm" };
            var keep = new CheckBox { Content = "Keep it for photos of sheets from this printer", IsChecked = true, MinHeight = Screens.Touch };
            var name = new TextBox { MinHeight = Screens.Touch, Text = Phone.Settings.LoadChosenPrinter()?.Name ?? PrinterProfile.DefaultName };
            var wrong = Screens.Dim("");
            wrong.IsVisible = false;
            Button? open = null;
            open = Screens.Choice("Measure this sheet with a ruler", () =>
            {
                open!.IsVisible = false;
                form.IsVisible = true;
            });
            form.Children.Add(Screens.Line(span.Ask));
            form.Children.Add(reading);
            form.Children.Add(keep);
            form.Children.Add(name);
            form.Children.Add(Screens.Choice("Correct this sheet", () =>
            {
                if (RulerSpan.ReadInches(reading.Text) is not { } inches)
                {
                    wrong.Text = "Type the distance as it reads on the ruler, such as 5 3/4, 5.75 in or 146 mm.";
                    wrong.IsVisible = true;
                    return;
                }

                if (PrinterProfile.FromRuler(name.Text, inches, span.DrawnInches, Today()) is not { } profile)
                {
                    wrong.Text = string.Create(CultureInfo.CurrentCulture,
                        $"That makes the sheet {inches / span.DrawnInches * 100:0} percent of its size, which no printer does. Measure again, from the center of bull {span.From} to the center of bull {span.To}.");
                    wrong.IsVisible = true;
                    return;
                }

                bool kept = keep.IsChecked == true;
                string line = kept
                    ? profile.Line
                    : string.Create(CultureInfo.CurrentCulture, $"Corrected for this sheet, {profile.Percentages}, measured with a ruler");
                if (kept)
                {
                    Phone.Settings.SavePrinter(profile);
                }

                if (session.State.Scale is SheetReference current)
                {
                    session.SetScale(current.CorrectedBy(profile, line));
                }

                said.Text = line;
                said.IsVisible = true;
                form.IsVisible = false;
                changed();
            }));
            form.Children.Add(wrong);
            card.Children.Add(open);
            card.Children.Add(form);
        }

        return Screens.Card(card);
    }
}
