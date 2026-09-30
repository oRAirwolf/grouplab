using Avalonia.Controls;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3 and entry 208 section 4: Settings, with the first run's questions together under Sharing, in
/// the same order and words, so every answer is changed in one place. The settings file is the desktop's own format, read and written by
/// the desktop's own code.
/// </summary>
public sealed class SettingsView : UserControl
{
    public SettingsView(AppSettingsStore settings)
    {
        var column = new StackPanel { Spacing = 12 };
        // Entry 246, look B: the page's title, each section's heading, its choices as cards and what it explains in the dim style.
        column.Children.Add(Screens.Title("Settings"));

        // Entry 294 section 1: Units at the top, the scope's unit first and said plainly, with one line on what it changes.
        column.Children.Add(Screens.Heading("Units"));
        column.Children.Add(Screens.Line(GroupLab.Core.Marking.UnitSettings.ScopeUnitLabel));
        var now = settings.LoadUnits();
        foreach (var angle in GroupLab.Core.Marking.UnitSettings.AngularChoices)
        {
            var radio = Screens.Radio("scopeUnit", GroupLab.Core.Marking.UnitSettings.Symbol(angle), now.Angular == angle);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settings.LoadUnits().Angular != angle)
                {
                    settings.SaveUnits(settings.LoadUnits() with { Angular = angle });
                    DiagnosticLog.Info("units.scope", ("angular", angle.ToString()));
                    Shell.Units();
                }
            };
            column.Children.Add(radio);
        }

        column.Children.Add(Screens.Dim(GroupLab.Core.Marking.UnitSettings.ScopeUnitSays));
        column.Children.Add(Screens.Line("Sizes on the paper"));
        foreach (var linear in new[] { GroupLab.Core.Marking.LinearUnit.Inch, GroupLab.Core.Marking.LinearUnit.Millimetre, GroupLab.Core.Marking.LinearUnit.Centimetre })
        {
            var radio = Screens.Radio("lengthUnit", GroupLab.Core.Marking.UnitSettings.Symbol(linear), now.Linear == linear);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settings.LoadUnits().Linear != linear)
                {
                    settings.SaveUnits(settings.LoadUnits() with { Linear = linear });
                    Shell.Units();
                }
            };
            column.Children.Add(radio);
        }

        column.Children.Add(Screens.Line("Distances"));
        foreach (var distance in new[] { GroupLab.Core.Marking.DistanceUnit.Yard, GroupLab.Core.Marking.DistanceUnit.Metre })
        {
            var radio = Screens.Radio("distanceUnit", GroupLab.Core.Marking.UnitSettings.Symbol(distance), now.Distance == distance);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settings.LoadUnits().Distance != distance)
                {
                    settings.SaveUnits(settings.LoadUnits() with { Distance = distance });
                    Shell.Units();
                }
            };
            column.Children.Add(radio);
        }

        // Entry 273 section 4: the printers, which one photographs are corrected for, check again, delete, add, and correction off.
        column.Children.Add(Screens.Heading("Printers"));
        column.Children.Add(Screens.Dim(GroupLab.Core.Marking.DetectionAdvice.OncePerPrinter));
        var printers = settings.LoadPrinters();
        var chosenPrinter = settings.LoadChosenPrinter();
        if (printers.Count == 0)
        {
            column.Children.Add(Screens.Line("No printer checked yet, so photographs are measured in the sheet's own inches."));
        }

        foreach (var p in printers)
        {
            var use = new RadioButton
            {
                GroupName = "printer",
                IsChecked = p.Name == chosenPrinter?.Name,
                MinHeight = Screens.Touch,
                Content = new TextBlock { Text = $"{p.Name}: {p.Percentages}, measured {p.How}", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            };
            use.IsCheckedChanged += (_, _) =>
            {
                if (use.IsChecked == true)
                {
                    settings.ChoosePrinter(p.Name);
                }
            };
            column.Children.Add(use);
            // Entry 291 section 5.2: a check holds for the sheets printed before the printer changed, and says so once it may not.
            if (p.Stale(DateOnly.FromDateTime(DateTime.Now)) is { } stale)
            {
                column.Children.Add(Screens.Line(stale));
            }

            column.Children.Add(Screens.Choice("Check again", () => Shell.Current?.ShowPrinterCheck(p.Name)));
            column.Children.Add(Screens.Choice(GroupLab.Core.Marking.PrinterProfile.ChangedWords, () =>
            {
                settings.MarkPrinterChanged(p.Name, DateOnly.FromDateTime(DateTime.Now));
                Shell.Current?.Show(Shell.Place.Settings);
            }));
            column.Children.Add(Screens.Choice("Delete " + p.Name, () =>
            {
                settings.DeletePrinter(p.Name);
                Shell.Current?.Show(Shell.Place.Settings);
            }));
        }

        var correcting = new CheckBox { Content = "Correct photographs by the chosen printer's scale", IsChecked = settings.LoadPrinterCorrection(), MinHeight = Screens.Touch };
        correcting.IsCheckedChanged += (_, _) => settings.SavePrinterCorrection(correcting.IsChecked == true);
        column.Children.Add(correcting);
        column.Children.Add(Screens.Choice("Add a printer", () => Shell.Current?.ShowPrinterCheck("")));
        column.Children.Add(Screens.Heading("Sharing"));
        // Entry 299: the page that says exactly what GroupLab sends, always in view; each section below shows its choice and one short line,
        // and the full explanation under "More".
        var whatIsSent = new Button { Content = SharingWords.WhatIsSentLabel, MinHeight = Screens.Touch, Classes = { AppStyles.Link } };
        whatIsSent.Click += (_, _) => Phone.Platform.OpenAddress(SharingWords.WhatIsSentAddress);
        column.Children.Add(whatIsSent);

        column.Children.Add(Screens.Heading("Sending targets"));
        if (!Shell.TargetsOpen)
        {
            column.Children.Add(Screens.Dim(SharingWords.TargetsClosed));
        }
        else
        {
            var (choice, level) = settings.LoadSending();
            foreach (var (value, words) in SharingWords.TargetChoices)
            {
                var radio = Screens.Radio("sendingChoice", words, choice == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && settings.LoadSending().Choice != value)
                    {
                        settings.SaveSending(value, settings.LoadSending().Level);
                        DiagnosticLog.Info("send.choice", ("choice", value.ToString()));
                    }
                };
                column.Children.Add(radio);
            }

            column.Children.Add(Screens.Dim(SharingWords.TargetsShort));
            column.Children.Add(Screens.Dim(SharingWords.LevelHeading));
            var more = new List<Control> { Screens.Dim(SharingWords.TargetsIntro) };
            foreach (var (value, words) in SharingWords.Levels(ReceiverTerms.Current))
            {
                more.Add(Screens.Dim(words));
                var radio = Screens.Radio("sendingLevel", SharingWords.LevelName(value), level == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    // A change applies to targets sent from now on and never re-labels one already sent.
                    if (radio.IsChecked == true && settings.LoadSending().Level != value)
                    {
                        settings.SaveSending(settings.LoadSending().Choice, value);
                    }
                };
                column.Children.Add(radio);
            }

            more.Add(Screens.Dim("What is sent:"));
            more.AddRange(TargetPackages.WhatIsSent.Select(line => (Control)Screens.Dim("• " + line)));
            column.Children.Add(MoreFold.Make(settings, "sending", more, Screens.Touch));
        }

        column.Children.Add(Screens.Heading("Error reports"));
        if (!Shell.ErrorsOpen)
        {
            column.Children.Add(Screens.Line("Sending error reports to the project is not open yet. When it is, GroupLab will ask once whether you want to."));
        }
        else
        {
            var choice = settings.LoadErrorChoice();
            foreach (var (value, words) in SharingWords.ErrorChoices)
            {
                var radio = Screens.Radio("errorChoice", words, choice == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && settings.LoadErrorChoice() != value)
                    {
                        settings.SaveErrorChoice(value);
                        DiagnosticLog.Info("errors.choice", ("choice", value.ToString()));
                        _ = Phone.SendWaitingErrorsAsync();
                    }
                };
                column.Children.Add(radio);
            }

            column.Children.Add(Screens.Dim(SharingWords.ErrorsShort));
            column.Children.Add(MoreFold.Make(settings, "errors",
                [Screens.Dim(SharingWords.ErrorsIntro), .. ErrorReports.WhatIsSent.Select(line => (Control)Screens.Dim("• " + line))], Screens.Touch));
        }

        column.Children.Add(Screens.Heading("Hardware survey"));
        if (!Shell.SurveyOpen)
        {
            column.Children.Add(Screens.Dim(SharingWords.SurveyClosed));
        }
        else
        {
            var chosen = settings.LoadSurveyChoice();
            foreach (var (value, words) in SharingWords.SurveyChoices)
            {
                var radio = Screens.Radio("surveyChoice", words, chosen == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && settings.LoadSurveyChoice() != value)
                    {
                        settings.SaveSurveyChoice(value);
                        if (value != SurveyChoice.Yes)
                        {
                            Phone.Survey?.Forget();
                        }

                        DiagnosticLog.Info("survey.choice", ("choice", value.ToString()));
                    }
                };
                column.Children.Add(radio);
            }

            column.Children.Add(Screens.Dim(SharingWords.SurveyShort));
            column.Children.Add(MoreFold.Make(settings, "survey",
                [Screens.Dim(SharingWords.SurveyIntro), .. SurveyReport.WhatIsSent.Select(line => (Control)Screens.Dim("• " + line))], Screens.Touch));

            // Entry 227 section 2: when the benchmark last ran and what it found, and a button to run it now, with its progress.
            column.Children.Add(Screens.Heading("The benchmark"));
            var benchmark = new BenchmarkPanel(settings, PhoneAnalysis.BenchmarkWork, FirstRunView.SendSurvey, SharingWords.BenchmarkButton, later: null);
            benchmark.Say(settings.LoadBenchmark() is { } last
                ? SharingWords.BenchmarkLast(settings.LoadBenchmarkRanAt(), last.Result, last.Sent)
                : SharingWords.BenchmarkNever);
            column.Children.Add(Screens.Card(benchmark));

            // Entry 241 sections 1.2 and 2.4: every run this copy has made, newest first, and the two things a person can do with their number.
            column.Children.Add(Screens.Heading(SharingWords.BenchmarkHistory));
            var history = new StackPanel { Spacing = 4 };
            void FillHistory()
            {
                history.Children.Clear();
                var runs = settings.LoadBenchmarkRuns();
                if (runs.Count == 0)
                {
                    history.Children.Add(Screens.Line(SharingWords.BenchmarkNever));
                }

                foreach (var run in runs.Reverse())
                {
                    history.Children.Add(Screens.Line(SharingWords.BenchmarkRunLine(run)));
                }
            }

            FillHistory();
            benchmark.Ended += FillHistory;
            column.Children.Add(Screens.Card(history));
            var said = Screens.Line("");
            column.Children.Add(Screens.Choice(SharingWords.ResetNumber, () =>
            {
                settings.ReplaceInstallation();
                DiagnosticLog.Info("survey.installation", ("replaced", true));
                said.Text = SharingWords.ResetNumberSaid;
            }));
            async Task Delete()
            {
                bool taken = Phone.Survey is { } queue && await queue.DeleteAsync(Shell.SurveyOpen, CancellationToken.None);
                said.Text = taken ? SharingWords.DeleteReportsSaid : SharingWords.DeleteReportsFailed;
            }

            column.Children.Add(Screens.Choice(SharingWords.DeleteReports, () => _ = Delete()));
            column.Children.Add(said);
        }

        // Entry 298 section 4: on a tablet or an unfolded phone in landscape, the result's split between the sheet and the numbers is dragged
        // and remembered; this puts it back.
        column.Children.Add(Screens.Heading("Layout"));
        column.Children.Add(Screens.Dim("On a tablet or an unfolded phone held sideways, the line between the sheet and the numbers on a result can be dragged, and GroupLab remembers where you left it."));
        var layoutSaid = Screens.Dim("");
        column.Children.Add(Screens.Choice("Reset layout", () =>
        {
            settings.ResetLayout();
            layoutSaid.Text = "Every pane is back to its default size.";
        }));
        column.Children.Add(layoutSaid);

        column.Children.Add(Screens.Heading("About"));
        var about = Screens.Card(Screens.Line($"GroupLab {AppInfo.Version}"));
        column.Children.Add(about);
        // Entry 288: the updater's version, its train's newest, Update now and the automatic switch, in a build that has one.
        if (Phone.Platform.UpdateCard() is { } updates)
        {
            ((StackPanel)about.Child!).Children.Add(updates);
        }

        if (Phone.Platform.IsDevBuild)
        {
            // Entry 234 section 1: said plainly, so a screenshot or a report from it is never mistaken for the published application.
            ((StackPanel)about.Child!).Children.Add(Screens.Dim("This is GroupLab Dev, the development build. It installs beside GroupLab from Google Play, can be debugged over adb, and marks its error and survey reports as coming from a development build."));

            // Entry 291 section 7.5: every picture of a sitting kept on the phone, for the developer to pull; off, and it is deleted.
            var keeping = new CheckBox { Content = "Keep every picture taken, on this phone only", IsChecked = settings.LoadKeepSitting(), MinHeight = Screens.Touch };
            var kept = Screens.Dim(KeptWords());
            keeping.IsCheckedChanged += (_, _) =>
            {
                settings.SaveKeepSitting(keeping.IsChecked == true);
                if (keeping.IsChecked != true)
                {
                    SittingRecord.Clear();
                }

                kept.Text = KeptWords();
            };
            ((StackPanel)about.Child!).Children.Add(keeping);
            ((StackPanel)about.Child!).Children.Add(kept);
        }
        Content = Screens.Page(column);
    }

    /// <summary>What GroupLab Dev keeps of a sitting, in a line under its switch.</summary>
    private static string KeptWords()
    {
        int count = SittingRecord.Count();
        string now = count == 1 ? "1 picture is" : $"{count} pictures are";
        return SittingRecord.On
            ? $"Each picture is kept with what the camera read before it and how it was analyzed, in the application's own folder, and sent nowhere. {now} kept now. Turning this off deletes them."
            : "Pictures are not kept.";
    }
}
