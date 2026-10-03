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

        // Entry 314 section 1: what the caliber box offers, the same setting as on the desktop.
        column.Children.Add(Screens.Heading(AppSettingsStore.CaliberListLabel));
        var offered = settings.LoadCaliberList();
        foreach (var list in Enum.GetValues<GroupLab.Core.Marking.CaliberList>())
        {
            var radio = Screens.Radio("caliberList", list.ToString(), offered == list);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settings.LoadCaliberList() != list)
                {
                    settings.SaveCaliberList(list);
                    DiagnosticLog.Info("settings.caliberList", ("list", list.ToString()));
                }
            };
            column.Children.Add(radio);
        }

        column.Children.Add(Screens.Dim(AppSettingsStore.CaliberListSays));

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

        var correcting = new CheckBox { Content = "Correct photographs by the chosen printer's scale", IsChecked = settings.LoadPrinterCorrection(), MinHeight = Screens.Touch }.Id("settings-printer-correction");
        correcting.IsCheckedChanged += (_, _) => settings.SavePrinterCorrection(correcting.IsChecked == true);
        column.Children.Add(correcting);
        column.Children.Add(Screens.Choice("Add a printer", () => Shell.Current?.ShowPrinterCheck("")).Id("settings-add-printer"));
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
            // Entry 357 section 1: the question, where it is still due, above the choices; choosing any of them answers it.
            if (SharingSwitches.EverythingOpen && settings.EverythingQuestionDue())
            {
                column.Children.Add(Screens.Line(SharingWords.EverythingQuestion));
                column.Children.Add(Screens.Dim(SharingWords.EverythingQuestionSays));
                column.Children.Add(Screens.Choice("Keep finished targets only", () =>
                {
                    settings.SaveEverythingAsked();
                    DiagnosticLog.Info("send.everything-asked", ("choice", nameof(SendingChoice.Always)));
                }).Id("settings-keep-finished"));
            }

            foreach (var (value, words) in SharingWords.TargetChoices)
            {
                var radio = Screens.Radio("sendingChoice", words, choice == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && settings.LoadSending().Choice != value)
                    {
                        settings.SaveSending(value, settings.LoadSending().Level);
                        if (SharingSwitches.EverythingOpen)
                        {
                            settings.SaveEverythingAsked();
                        }

                        DiagnosticLog.Info("send.choice", ("choice", value.ToString()));
                    }
                };
                column.Children.Add(radio);
            }

            if (SharingSwitches.EverythingOpen)
            {
                // Entry 357 section 1: on a phone everything waits for Wi-Fi unless mobile data is allowed.
                column.Children.Add(Screens.Dim(SharingWords.EverythingSays + " " + SharingWords.EverythingWaitsForWifi));
                var mobileData = new CheckBox { Content = SharingWords.MobileData, IsChecked = settings.LoadMobileData(), MinHeight = Screens.Touch };
                mobileData.IsCheckedChanged += (_, _) =>
                {
                    settings.SaveMobileData(mobileData.IsChecked == true);
                    DiagnosticLog.Info("send.mobile-data", ("allowed", mobileData.IsChecked == true));
                };
                column.Children.Add(mobileData.Id("settings-mobile-data"));
            }

            column.Children.Add(Screens.Dim(SharingWords.TargetsShortNow));
            column.Children.Add(Screens.Dim(SharingWords.LevelHeading));
            var more = new List<Control> { Screens.Dim(SharingWords.TargetsIntroNow) };
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
            more.AddRange(SharingWords.TargetsWhatIsSentNow.Select(line => (Control)Screens.Dim("• " + line)));
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
                        settings.SaveErrorChoice(value, SharingSwitches.FullLogOpen);
                        DiagnosticLog.Info("errors.choice", ("choice", value.ToString()));
                        _ = Phone.SendWaitingErrorsAsync();
                    }
                };
                column.Children.Add(radio);
            }

            column.Children.Add(Screens.Dim(SharingWords.ErrorsShort));
            // Entry 357 section 2: somebody on automatic reports under the thinner wording is asked here too, until they answer.
            if (SharingSwitches.FullLogOpen && settings.ErrorWordingDue())
            {
                column.Children.Add(Screens.Line(SharingWords.ErrorsWordingChanged));
                column.Children.Add(Screens.Choice("Send them with the log", () => settings.SaveErrorChoice(ErrorReportChoice.Always, fullLogWording: true)).Id("settings-errors-with-log"));
            }

            column.Children.Add(MoreFold.Make(settings, "errors",
                [Screens.Dim(SharingWords.ErrorsIntroNow), .. ErrorReports.WhatIsSentNow.Select(line => (Control)Screens.Dim("• " + line))], Screens.Touch));
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
        // Entry 307: everything exported to one file, and imported from one.
        foreach (var control in new DataSection(settings).Controls())
        {
            column.Children.Add(control);
        }

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
        var about = Screens.Card(Screens.Line($"GroupLab {AppInfo.ShortVersion}"));
        column.Children.Add(about);
        // Entry 288: the updater's version, its train's newest, Update now and the automatic switch, in a build that has one.
        if (Phone.Platform.UpdateCard() is { } updates)
        {
            ((StackPanel)about.Child!).Children.Add(updates);
        }

        if (Phone.Platform.IsDevBuild)
        {
            // Entry 234 section 1: said plainly, so a screenshot or a report from it is never mistaken for the published application.
            ((StackPanel)about.Child!).Children.Add(Screens.Dim(OperatingSystem.IsIOS()
                ? "This is GroupLab Dev, the development build. It installs beside GroupLab, can be driven by a developer's scripts, and marks its error and survey reports as coming from a development build."
                : "This is GroupLab Dev, the development build. It installs beside GroupLab from Google Play, can be debugged over adb, and marks its error and survey reports as coming from a development build."));
#if GROUPLAB_DEV

            // Entry 315 section 1: the automation bridge, with its key shown so a developer can reach it over the cable.
            var bridge = new CheckBox { Content = "Let a developer's scripts drive GroupLab Dev over the cable", IsChecked = Dev.Bridge.Wanted, MinHeight = Screens.Touch }.Id("settings-dev-bridge");
            var bridgeSaid = Screens.Dim(Dev.Bridge.Words());
            bridge.IsCheckedChanged += (_, _) =>
            {
                Dev.Bridge.Wanted = bridge.IsChecked == true;
                bridgeSaid.Text = Dev.Bridge.Words();
            };
            ((StackPanel)about.Child!).Children.Add(bridge);
            ((StackPanel)about.Child!).Children.Add(bridgeSaid);

            // Entry 315 section 3: the camera's last seconds kept as a clip each time it closes, for a developer to replay on later builds.
            var record = new CheckBox { Content = "Record the camera's last few seconds, on this device only", IsChecked = Dev.CameraReplay.Recording, MinHeight = Screens.Touch }.Id("settings-dev-record-camera");
            var recordSaid = Screens.Dim(Dev.CameraReplay.Words());
            record.IsCheckedChanged += (_, _) =>
            {
                Dev.CameraReplay.Recording = record.IsChecked == true;
                recordSaid.Text = Dev.CameraReplay.Words();
            };
            ((StackPanel)about.Child!).Children.Add(record);
            ((StackPanel)about.Child!).Children.Add(recordSaid);
#endif
        }

        // Entry 311 section 3 item 1: the logs, the crash records and the kept pictures in one file, through the share sheet.
        var sendSaid = Screens.Dim("The newest logs, any crash records and the kept pictures, in one file you send where you choose.");
        ((StackPanel)about.Child!).Children.Add(Screens.Choice("Send diagnostics", () => sendSaid.Text = DiagnosticsPackage.Send(DateTime.Now)).Id("settings-send-diagnostics"));
        ((StackPanel)about.Child!).Children.Add(sendSaid);

        // Entry 315 section 4: the diagnostics overlay, a plain setting on every build, off until turned on.
        var overlay = new CheckBox { Content = "Show diagnostics on the camera", IsChecked = settings.LoadShowDiagnostics(), MinHeight = Screens.Touch }.Id("settings-show-diagnostics");
        overlay.IsCheckedChanged += (_, _) => settings.SaveShowDiagnostics(overlay.IsChecked == true);
        ((StackPanel)about.Child!).Children.Add(overlay);
        ((StackPanel)about.Child!).Children.Add(Screens.Dim(OverlayWords));

        if (Phone.Platform.KeepsSittings is (true, var keepByDefault))
        {
            // Entry 291 section 7.5: every picture of a sitting kept on the device, for the developer to pull; off, and it is deleted.
            var keeping = new CheckBox { Content = "Keep every picture taken, on this device only", IsChecked = settings.LoadKeepSitting(keepByDefault), MinHeight = Screens.Touch }.Id("settings-keep-pictures");
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

    /// <summary>What the diagnostics overlay shows, in a line under its switch.</summary>
    internal const string OverlayWords = "A small block of numbers over the camera and while a picture is read: frames a second, what the guidance is waiting for, the tilt, the torch, the step being read and its time, the memory in use and how warm the phone is. A screenshot of it shows somebody helping you where GroupLab was.";

    /// <summary>What GroupLab Dev keeps of a sitting, in a line under its switch.</summary>
    private static string KeptWords()
    {
        int count = SittingRecord.Count();
        string now = count == 1 ? "1 picture is" : $"{count} pictures are";
        string where = OperatingSystem.IsIOS() ? "in GroupLab's folder in the Files app" : "in the application's own folder";
        return SittingRecord.On
            ? $"Each picture is kept with what the camera read before it and how it was analyzed, {where}, and sent nowhere. {now} kept now. Turning this off deletes them."
            : "Pictures are not kept.";
    }
}
