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
/// <para>
/// Entry 468 section 1, concept A's phone boards: Settings opens the list of the eight categories, each with its current choices in one line
/// under its name and the search box at the top; a tap opens that category's page, with "‹ Settings" back. The same categories and rows as
/// the computer's. Every page is built at once, so a search shows the same controls in place.
/// </para>
/// </summary>
public sealed class SettingsView : UserControl
{
    private readonly AppSettingsStore settings;
    private readonly SettingsPages pages = new();

    /// <summary>The pages and their rows, for the search audit (entry 474 section 2).</summary>
    internal SettingsPages Pages => pages;
    private readonly StackPanel list = new() { Spacing = 12 };
    private readonly StackPanel categories = new() { Spacing = 0 };
    private readonly Border categoriesCard;
    private readonly TextBox search = new() { PlaceholderText = SettingsMenu.Find, MinHeight = Screens.Touch };
    private readonly TextBlock nothing = Screens.Dim(SettingsMenu.NothingFound);
    private readonly ScrollViewer scroll;

    /// <summary>The category whose page is open, or null while the list is shown.</summary>
    internal SettingsCategory? Open { get; private set; }

    public SettingsView(AppSettingsStore settings, SettingsCategory? open = null)
    {
        this.settings = settings;
        // A .grouplab file opened from another app waits for its plan on Sync and your data, whichever way Settings was opened.
        open ??= DataSection.Waiting is not null ? SettingsCategory.Data : null;
        var whole = new StackPanel { Spacing = 12 };
        // Entry 246, look B: the page's title, each section's heading, its choices as cards and what it explains in the dim style.
        list.Children.Add(Screens.Title("Settings"));
        Avalonia.Automation.AutomationProperties.SetName(search, SettingsMenu.Find);
        list.Children.Add(search.Id("settings-find"));
        nothing.IsVisible = false;
        list.Children.Add(nothing);
        categoriesCard = new Border { Child = categories, Classes = { PhoneStyles.Card } };
        list.Children.Add(categoriesCard);
        whole.Children.Add(list);

        var general = Page(whole, SettingsCategory.General);
        var unitsPage = Page(whole, SettingsCategory.Units);
        var shooting = Page(whole, SettingsCategory.Shooting);
        var printersPage = Page(whole, SettingsCategory.Printers);
        var data = Page(whole, SettingsCategory.Data);
        var sharing = Page(whole, SettingsCategory.Sharing);
        var about = Page(whole, SettingsCategory.Updates);
        var help = Page(whole, SettingsCategory.Help);

        BuildGeneral(general);
        BuildUnits(unitsPage);
        BuildShooting(shooting);
        BuildPrinters(printersPage);
        BuildData(data);
        BuildSharing(sharing);
        BuildAbout(about);
        BuildHelp(help);

        foreach (var category in SettingsMenu.All)
        {
            var tracked = pages[category];
            tracked.OnItsOwn(PutBackRow(category, (StackPanel)tracked.Root));
        }

        search.TextChanged += (_, _) => Filter();
        FillList();
        scroll = (ScrollViewer)Screens.Page(whole);
        Content = scroll;
        if (open is { } opening)
        {
            Show(opening);
        }
        else
        {
            ShowList();
        }
    }

    private void BuildGeneral((StackPanel Body, SettingsPages.Page Page) general)
    {
        // Entry 468 section 2.6: the phones follow the system's text size, and say so; Reduce motion as the phone asks, or on, or off.
        var look = Group(general, "Look", "text size font larger bigger smaller reduce motion animation flash still accessibility");
        look.Children.Add(Screens.Line(GeneralWords.TextSize));
        look.Children.Add(Screens.Dim(GeneralWords.TextSizeOnThePhone));
        look.Children.Add(Screens.Line(GeneralWords.Motion));
        var motion = settings.LoadReduceMotion();
        foreach (var choice in Enum.GetValues<MotionChoice>())
        {
            var radio = Screens.Radio("reduceMotion", GeneralWords.MotionChoices[(int)choice], motion == choice);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    settings.SaveReduceMotion(choice);
                }
            };
            look.Children.Add(radio);
        }

        look.Children.Add(Screens.Dim(GeneralWords.MotionSays));
        general.Page.OnPutBack(() => settings.SaveReduceMotion(MotionChoice.System));

        // Entry 446 section 1: the guided demo, any time, from the start or where it was left.
        var column = Group(general, "Guided demo", "tour demo walk through sample");
        column.Children.Add(PhoneDemo.SettingsCard(Shell.Current));

        // Entry 298 section 4: on a tablet or an unfolded phone in landscape, the result's split between the sheet and the numbers is dragged
        // and remembered; this puts it back.
        column = Group(general, "Layout", "reset layout panes split tablet");
        column.Children.Add(Screens.Dim("On a tablet or an unfolded phone held sideways, the line between the sheet and the numbers on a result can be dragged, and GroupLab remembers where you left it."));
        var layoutSaid = Screens.Dim("");
        column.Children.Add(Screens.Choice("Reset layout", () =>
        {
            settings.ResetLayout();
            layoutSaid.Text = "Every pane is back to its default size.";
        }));
        column.Children.Add(layoutSaid);
    }

    private void BuildUnits((StackPanel Body, SettingsPages.Page Page) unitsPage)
    {
        // Entry 294 section 1: Units at the top, the scope's unit first and said plainly, with one line on what it changes.
        var column = Group(unitsPage, "Units", "scope unit MOA mil mrad SMOA angle clicks lengths inches mm millimeters millimetres cm distances yards metres meters metric imperial"); // British on purpose: what a person may type in Find a setting
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
        column.Children.Add(Screens.Line("Lengths"));
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

        unitsPage.Page.OnPutBack(() =>
        {
            settings.SaveUnits(GroupLab.Core.Marking.UnitSettings.ForRegion(AppSettingsStore.Region()));
            Shell.Units();
        });

        // Entry 314 section 1: what the caliber box offers, the same setting as on the desktop.
        column = Group(unitsPage, AppSettingsStore.CaliberListLabel, "calibre caliber cartridge diameter"); // British on purpose: what a person may type in Find a setting
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
        unitsPage.Page.OnPutBack(() => settings.SaveCaliberList(GroupLab.Core.Marking.CaliberList.Both));

        // Entry 468 section 2.4: the main figures, as before, or every figure with the folded sections open at once.
        column = Group(unitsPage, DefaultsWords.AnalysisOpens, "advanced folded expanded figures open");
        bool every = settings.LoadEveryFigure();
        foreach (var (words, isEvery) in new[] { (DefaultsWords.AnalysisChoices[0], false), (DefaultsWords.AnalysisChoices[1], true) })
        {
            var radio = Screens.Radio("analysisOpens", words, every == isEvery);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    settings.SaveEveryFigure(isEvery);
                }
            };
            column.Children.Add(radio);
        }

        column.Children.Add(Screens.Dim(DefaultsWords.AnalysisOpensSays));
        unitsPage.Page.OnPutBack(() => settings.SaveEveryFigure(false));

        // Entry 424 section 2: what the zero correction says of an axis inside the scatter, the same setting as on the desktop.
        column = Group(unitsPage, "Zero correction", "zero clicks scatter correction dial leave it");
        column.Children.Add(Screens.Dim(GroupLab.Core.Marking.ZeroWords.SettingLabel));
        foreach (var (rule, words) in new[] { (GroupLab.Core.Marking.InsideScatter.LeaveIt, GroupLab.Core.Marking.ZeroWords.LeaveItChoice), (GroupLab.Core.Marking.InsideScatter.ClicksAnyway, GroupLab.Core.Marking.ZeroWords.AnywayChoice) })
        {
            var radio = Screens.Radio("insideScatter", words, settings.LoadInsideScatter() == rule);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settings.LoadInsideScatter() != rule)
                {
                    settings.SaveInsideScatter(rule);
                    DiagnosticLog.Info("settings.insideScatter", ("rule", rule.ToString()));
                }
            };
            column.Children.Add(radio);
        }

        column.Children.Add(Screens.Dim(GroupLab.Core.Marking.ZeroWords.SettingSays));
        unitsPage.Page.OnPutBack(() => settings.SaveInsideScatter(GroupLab.Core.Marking.InsideScatter.LeaveIt));
    }

    private void BuildShooting((StackPanel Body, SettingsPages.Page Page) shooting)
    {
        // Entry 468 section 2.2: the distance a new target starts with, the last one used (as before) or always the one typed here.
        var column = Group(shooting, DefaultsWords.StartsWith, "distance range yards metres paper letter a4 default new target rifle load rounds fired"); // British on purpose: what a person may type in Find a setting
        var units = settings.LoadUnits();
        var fixedInches = settings.LoadNewTargetDistance();
        column.Children.Add(Screens.Line(DefaultsWords.Distance));
        var lastUsed = Screens.Radio("newDistance", DefaultsWords.DistanceChoices[0], fixedInches is null);
        var always = Screens.Radio("newDistance", DefaultsWords.DistanceChoices[1], fixedInches is not null);
        var fixedBox = new TextBox
        {
            MinHeight = Screens.Touch,
            PlaceholderText = "Distance in " + GroupLab.Core.Marking.UnitSettings.Symbol(units.Distance),
            Text = fixedInches is { } shown ? GroupLab.Core.Marking.UnitSettings.DistanceFromInches(shown, units.Distance).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) : "",
            IsVisible = fixedInches is not null,
        }.Id("settings-new-distance");
        void SaveDistance()
        {
            fixedBox.IsVisible = always.IsChecked == true;
            if (always.IsChecked != true)
            {
                settings.SaveNewTargetDistance(null);
            }
            else if (double.TryParse(fixedBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out double typed) && typed > 0)
            {
                settings.SaveNewTargetDistance(GroupLab.Core.Marking.UnitSettings.DistanceToInches(typed, units.Distance));
            }
        }

        lastUsed.IsCheckedChanged += (_, _) => SaveDistance();
        always.IsCheckedChanged += (_, _) => SaveDistance();
        fixedBox.TextChanged += (_, _) => SaveDistance();
        column.Children.Add(lastUsed);
        column.Children.Add(always);
        column.Children.Add(fixedBox);
        column.Children.Add(Screens.Dim(DefaultsWords.DistanceSays));
        shooting.Page.OnPutBack(() => settings.SaveNewTargetDistance(null));

        // Shown here too; it lives in Printers and paper.
        PaperRadios(column, "paperShooting");
        shooting.Page.OnPutBack(() => settings.ForgetPaper());
        column.Children.Add(Screens.Dim(DefaultsWords.OnTheComputer));

        // Entry 468 section 2.3: the camera starts in Guided or Manual, and makes a sound when it takes the picture unless turned off.
        column = Group(shooting, DefaultsWords.Camera, "camera guided manual starts sound shutter click");
        column.Children.Add(Screens.Line(DefaultsWords.StartsIn));
        bool manual = settings.LoadCameraStartsManual();
        foreach (var (words, isManual) in new[] { (DefaultsWords.StartsInChoices[0], false), (DefaultsWords.StartsInChoices[1], true) })
        {
            var radio = Screens.Radio("cameraStarts", words, manual == isManual);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    settings.SaveCameraStartsManual(isManual);
                    DiagnosticLog.Info("settings.camera-starts", ("manual", isManual));
                }
            };
            column.Children.Add(radio.Id(isManual ? "settings-camera-manual" : "settings-camera-guided"));
        }

        column.Children.Add(Screens.Dim(DefaultsWords.StartsInSays));
        shooting.Page.OnPutBack(() => settings.SaveCameraStartsManual(false));
        bool required = Phone.Platform.ShutterSoundRequired();
        var sound = new CheckBox { Content = DefaultsWords.Sound, IsChecked = required || settings.LoadShutterSound(), IsEnabled = !required, MinHeight = Screens.Touch }.Id("settings-shutter-sound");
        sound.IsCheckedChanged += (_, _) => settings.SaveShutterSound(sound.IsChecked == true);
        column.Children.Add(sound);
        if (required)
        {
            column.Children.Add(Screens.Dim(DefaultsWords.SoundRequired));
        }

        shooting.Page.OnPutBack(() => settings.SaveShutterSound(true));
    }

    /// <summary>Entry 468 section 2.2: Letter or A4 for the sheets GroupLab prints, the region's own until one is chosen.</summary>
    private void PaperRadios(StackPanel column, string group)
    {
        column.Children.Add(Screens.Line(DefaultsWords.Paper));
        bool letter = AppSettingsStore.LetterPaper();
        foreach (var (words, isLetter) in new[] { (DefaultsWords.PaperChoices[0], true), (DefaultsWords.PaperChoices[1], false) })
        {
            var radio = Screens.Radio(group, words, letter == isLetter);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && AppSettingsStore.LetterPaper() != isLetter)
                {
                    settings.SavePaperLetter(isLetter);
                }
            };
            column.Children.Add(radio);
        }

        column.Children.Add(Screens.Dim(DefaultsWords.PaperSays));
    }

    private void BuildPrinters((StackPanel Body, SettingsPages.Page Page) printersPage)
    {
        // Entry 273 section 4: the printers, which one photographs are corrected for, check again, delete, add, and correction off.
        var column = Group(printersPage, "Printers", "printer scale check correction measured M834 M220 add");
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
                Shell.Current?.ShowSettings(SettingsCategory.Printers);
            }));
            column.Children.Add(Screens.Choice("Delete " + p.Name, () =>
            {
                settings.DeletePrinter(p.Name);
                Shell.Current?.ShowSettings(SettingsCategory.Printers);
            }));
        }

        var correcting = new CheckBox { Content = "Correct photographs by the chosen printer's scale", IsChecked = settings.LoadPrinterCorrection(), MinHeight = Screens.Touch }.Id("settings-printer-correction");
        correcting.IsCheckedChanged += (_, _) => settings.SavePrinterCorrection(correcting.IsChecked == true);
        column.Children.Add(correcting);
        column.Children.Add(Screens.Choice("Add a printer", () => Shell.Current?.ShowPrinterCheck("")).Id("settings-add-printer"));
        printersPage.Page.OnPutBack(() => settings.SavePrinterCorrection(true));
        column = Group(printersPage, "", "paper letter a4 sheet size print");
        PaperRadios(column, "paperPrinters");
        printersPage.Page.OnPutBack(() => settings.ForgetPaper());
    }

    private void BuildData((StackPanel Body, SettingsPages.Page Page) data)
    {
        // Entry 468 section 2.1, Alan's priority: a daily copy of the records, first on the page.
        BuildCopies(data);

        // Entry 463 section 1 (1A): synchronization, its own page under Settings.
        var column = Group(data, GroupLab.Core.Sync.SyncScreens.Title, "sync synchronize devices cloud folder phone computer onedrive dropbox google drive icloud");
        var sync = PhoneSync.Setup;
        column.Children.Add(Screens.Row(GroupLab.Core.Sync.SyncScreens.Title,
            sync.On ? (PhoneSync.Unreachable ? GroupLab.Core.Sync.SyncScreens.UnreachableTitle : GroupLab.Core.Sync.SyncScreens.Status(sync, DateTime.UtcNow)) : "Off. Keep your devices the same through your own cloud folder.",
            () => Shell.Current?.ShowSync()).Id("settings-sync"));

        // Entry 307: everything exported to one file, and imported from one.
        column = Group(data, "", "your data export import file records sessions move");
        foreach (var control in new DataSection(settings).Controls())
        {
            column.Children.Add(control);
        }
    }

    private void BuildCopies((StackPanel Body, SettingsPages.Page Page) data)
    {
        var column = Group(data, CopiesWords.Heading, "backup back up copy copies restore daily records database");
        var keep = new CheckBox { Content = CopiesWords.Keep, IsChecked = settings.LoadDailyCopies(), MinHeight = Screens.Touch }.Id("settings-daily-copies");
        var last = Screens.Line(CopiesWords.Last(Phone.Copies?.Newest));
        keep.IsCheckedChanged += (_, _) =>
        {
            settings.SaveDailyCopies(keep.IsChecked == true);
            DiagnosticLog.Info("copies.setting", ("on", keep.IsChecked == true));
        };
        column.Children.Add(keep);
        column.Children.Add(Screens.Dim(CopiesWords.KeepSays));
        column.Children.Add(last);
        data.Page.OnPutBack(() => settings.SaveDailyCopies(true));
        var said = Screens.Dim("");
        column.Children.Add(Screens.Choice(CopiesWords.BackUpNow, () =>
        {
            var result = Phone.Copies?.Make();
            said.Text = result?.Said ?? CopiesWords.None;
            last.Text = CopiesWords.Last(Phone.Copies?.Newest);
            DiagnosticLog.Info("copies.now", ("made", result?.Made == true));
        }).Id("settings-back-up-now"));

        // Restore: the copies with their day and sessions, then the one question, on the page itself.
        var choose = new StackPanel { Spacing = 8, IsVisible = false };
        column.Children.Add(Screens.Choice(CopiesWords.Restore, () =>
        {
            choose.Children.Clear();
            var listed = Phone.Copies?.List(countSessions: true) ?? [];
            choose.Children.Add(Screens.Line(listed.Count == 0 ? CopiesWords.NoneToRestore : CopiesWords.WhichCopy));
            foreach (var copy in listed)
            {
                choose.Children.Add(Screens.Choice(CopiesWords.Choice(copy), () =>
                {
                    choose.Children.Clear();
                    choose.Children.Add(Screens.Line(CopiesWords.Ask(copy)));
                    choose.Children.Add(Screens.Primary(CopiesWords.RestoreIt, () =>
                    {
                        RestoreCopy(copy, said);
                        choose.IsVisible = false;
                        last.Text = CopiesWords.Last(Phone.Copies?.Newest);
                    }).Id("settings-restore-yes"));
                    choose.Children.Add(Screens.Choice(CopiesWords.LeaveIt, () => choose.IsVisible = false));
                }));
            }

            choose.Children.Add(Screens.Choice(CopiesWords.LeaveIt, () => choose.IsVisible = false));
            choose.IsVisible = true;
        }).Id("settings-restore"));
        column.Children.Add(choose);
        column.Children.Add(said);
        column.Children.Add(Screens.Dim(CopiesWords.OnThePhone));
    }

    /// <summary>Puts <paramref name="copy"/> in place of the records, which every screen opens afresh, as the phone's screens open the records each time.</summary>
    private static void RestoreCopy(GroupLab.Core.Records.RecordsCopy copy, TextBlock said)
    {
        var result = Phone.Copies!.Restore(copy);
        DiagnosticLog.Info("copies.restore", ("restored", result.Made));
        said.Text = result.Said;
    }

    private void BuildSharing((StackPanel Body, SettingsPages.Page Page) sharing)
    {
        // Entry 299: the page that says exactly what GroupLab sends, always in view; each section below shows its choice and one short line,
        // and the full explanation under "More".
        var column = Group(sharing, "", "sharing what is sent privacy");
        var whatIsSent = new Button { Content = SharingWords.WhatIsSentLabel, MinHeight = Screens.Touch, Classes = { AppStyles.Link } };
        whatIsSent.Click += (_, _) => Phone.Platform.OpenAddress(SharingWords.WhatIsSentAddress);
        column.Children.Add(whatIsSent);

        column = Group(sharing, "Sending targets", "share pictures consent publish privacy");
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

        column = Group(sharing, "Error reports", "crash send automatically privacy");
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

        column = Group(sharing, "Hardware survey", "benchmark speed phone privacy");
        if (!Shell.SurveyOpen)
        {
            column.Children.Add(Screens.Dim(SharingWords.SurveyClosed));
            return;
        }

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
        var benchmark = new BenchmarkPanel(settings, PhoneAnalysis.BenchmarkWork, FirstRunView.SendSurvey, SharingWords.BenchmarkButton, later: null, runHeight: Screens.Touch);
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

    private void BuildAbout((StackPanel Body, SettingsPages.Page Page) aboutPage)
    {
        var column = Group(aboutPage, "About", "version build update train nightly check");
        var aboutCard = Screens.Card(Screens.Line($"GroupLab {AppInfo.ShortVersion}"));
        var about = (StackPanel)aboutCard.Child!;
        column.Children.Add(aboutCard);
        // Entry 288: the updater's version, its train's newest, Update now and the automatic switch, in a build that has one.
        if (Phone.Platform.UpdateCard() is { } updates)
        {
            about.Children.Add(updates);
        }

        if (!Phone.Platform.IsDevBuild)
        {
            return;
        }

        // Entry 234 section 1: said plainly, so a screenshot or a report from it is never mistaken for the published application.
        about.Children.Add(Screens.Dim(OperatingSystem.IsIOS()
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
        about.Children.Add(bridge);
        about.Children.Add(bridgeSaid);

        // Entry 315 section 3: the camera's last seconds kept as a clip each time it closes, for a developer to replay on later builds.
        var record = new CheckBox { Content = "Record the camera's last few seconds, on this device only", IsChecked = Dev.CameraReplay.Recording, MinHeight = Screens.Touch }.Id("settings-dev-record-camera");
        var recordSaid = Screens.Dim(Dev.CameraReplay.Words());
        record.IsCheckedChanged += (_, _) =>
        {
            Dev.CameraReplay.Recording = record.IsChecked == true;
            recordSaid.Text = Dev.CameraReplay.Words();
        };
        about.Children.Add(record);
        about.Children.Add(recordSaid);
#endif
    }

    private void BuildHelp((StackPanel Body, SettingsPages.Page Page) helpPage)
    {
        var column = Group(helpPage, "Reporting a problem", "report a problem send diagnostics log crash bug issue");
        var helpCard = Screens.Card();
        var help = (StackPanel)helpCard.Child!;
        column.Children.Add(helpCard);

        // Entry 386 section 4 (entry 376 B7): the diagnostics straight to GroupLab, first, through the error reports' own route and rules, with
        // an optional note; sharing the file stays as the second choice. Only while the phones' senders are on.
        if (SharingSwitches.FullLogOpen)
        {
            var note = new TextBox { MinHeight = Screens.Touch, PlaceholderText = "What happened (optional)", AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxLength = ErrorReports.MostNote }.Id("settings-diagnostics-note");
            var straightSaid = Screens.Dim("The two newest logs and any crash record, with your note, sent to GroupLab's maker. No picture, no file names and nothing about where you are; names you typed are left out.");
            async Task Straight()
            {
                straightSaid.Text = "Sending…";
                straightSaid.Text = await ErrorReports.SendDiagnosticsAsync(GroupLab.Core.Updates.TheOutsideWorld.Current, ReceiverTerms.Current.CrashReceiver, ReceiverTerms.Current.ErrorReceiver, note.Text, CancellationToken.None);
            }

            help.Children.Add(note);
            help.Children.Add(Screens.Choice("Send to GroupLab", () => _ = Straight()).Id("settings-send-to-grouplab"));
            help.Children.Add(straightSaid);
        }

        // Entry 311 section 3 item 1: the logs, the crash records and the kept pictures in one file, through the share sheet.
        var sendSaid = Screens.Dim("The newest logs, any crash records and the kept pictures, in one file you send where you choose.");
        help.Children.Add(Screens.Choice("Send diagnostics", () => sendSaid.Text = DiagnosticsPackage.Send(DateTime.Now)).Id("settings-send-diagnostics"));
        help.Children.Add(sendSaid);

        // Entry 315 section 4: the diagnostics overlay, a plain setting on every build, off until turned on.
        column = Group(helpPage, "The camera", "diagnostics overlay camera numbers keep pictures");
        var overlay = new CheckBox { Content = "Show diagnostics on the camera", IsChecked = settings.LoadShowDiagnostics(), MinHeight = Screens.Touch }.Id("settings-show-diagnostics");
        overlay.IsCheckedChanged += (_, _) => settings.SaveShowDiagnostics(overlay.IsChecked == true);
        column.Children.Add(overlay);
        column.Children.Add(Screens.Dim(OverlayWords));
        helpPage.Page.OnPutBack(() => settings.SaveShowDiagnostics(false));

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
            column.Children.Add(keeping);
            column.Children.Add(kept);
        }
    }

    /// <summary>A category's page: "‹ Settings" back, its title and its line, hidden until it is opened.</summary>
    private (StackPanel Body, SettingsPages.Page Page) Page(StackPanel whole, SettingsCategory category)
    {
        var body = new StackPanel { Spacing = 12 };
        var back = Screens.BackButton(SettingsMenu.BackToList, ShowList).Id("settings-back");
        back.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        Avalonia.Automation.AutomationProperties.SetName(back, "Back to Settings");
        var intro = Screens.Dim(SettingsMenu.Intro(category));
        body.Children.Add(back);
        body.Children.Add(Screens.Title(SettingsMenu.Name(category)));
        body.Children.Add(intro);
        whole.Children.Add(body);
        return (body, pages.Add(category, body, back, intro));
    }

    /// <summary>A group of rows on a page, under its heading where it has one, found by the heading and <paramref name="words"/>.</summary>
    private static StackPanel Group((StackPanel Body, SettingsPages.Page Page) page, string heading, string words)
    {
        var group = new StackPanel { Spacing = 12 };
        if (heading.Length > 0)
        {
            group.Children.Add(Screens.Heading(heading));
        }

        page.Body.Children.Add(group);
        page.Page.Card(group, heading, words);
        return group;
    }

    /// <summary>"Put this page back as it came", which asks once, on the page itself, and touches no data.</summary>
    private Control PutBackRow(SettingsCategory category, StackPanel body)
    {
        var holder = new StackPanel { Spacing = 8 };
        var ask = new StackPanel { Spacing = 8, IsVisible = false };
        var link = new Button { Content = SettingsMenu.PutBack, MinHeight = Screens.Touch, Classes = { AppStyles.Link } }.Id("settings-put-back");
        link.Click += (_, _) => ask.IsVisible = true;
        ask.Children.Add(Screens.Dim(SettingsMenu.PutBackAsks(category)));
        ask.Children.Add(Screens.Choice(SettingsMenu.PutBack, () => PutBack(category)).Id("settings-put-back-yes"));
        ask.Children.Add(Screens.Choice("Leave it as it is", () => ask.IsVisible = false));
        holder.Children.Add(link);
        holder.Children.Add(ask);
        body.Children.Add(holder);
        return holder;
    }

    /// <summary>Puts <paramref name="category"/>'s page back as it came, as its link does once answered, and shows it again.</summary>
    internal void PutBack(SettingsCategory category)
    {
        pages.PutBack(category);
        DiagnosticLog.Info("settings.put-back", ("page", category.ToString()));
        Shell.Current?.ShowSettings(category);
    }

    /// <summary>The list of the eight categories, each with its current choices in one line.</summary>
    private void FillList()
    {
        categories.Children.Clear();
        foreach (var category in SettingsMenu.All)
        {
            categories.Children.Add(Screens.Row(SettingsMenu.Name(category), Summary(category), () => Show(category), explain: false)
                .Id("settings-" + category.ToString().ToLowerInvariant()));
        }
    }

    /// <summary>A category's current choices, in the line under its name in the list.</summary>
    internal string Summary(SettingsCategory category)
    {
        switch (category)
        {
            case SettingsCategory.General:
                return "Motion: " + GeneralWords.MotionChoices[(int)settings.LoadReduceMotion()].ToLowerInvariant() + " · guided demo";
            case SettingsCategory.Units:
                var units = settings.LoadUnits();
                return string.Join(" · ", GroupLab.Core.Marking.UnitSettings.Symbol(units.Angular), GroupLab.Core.Marking.UnitSettings.Symbol(units.Linear), GroupLab.Core.Marking.UnitSettings.Symbol(units.Distance));
            case SettingsCategory.Shooting:
                var distanceUnit = settings.LoadUnits().Distance;
                string distance = settings.LoadNewTargetDistance() is { } inches
                    ? GroupLab.Core.Marking.UnitSettings.DistanceFromInches(inches, distanceUnit).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) + " " + GroupLab.Core.Marking.UnitSettings.Symbol(distanceUnit)
                    : "Last distance";
                return string.Join(" · ", distance, AppSettingsStore.LetterPaper() ? "Letter" : "A4", settings.LoadCameraStartsManual() ? "Manual" : "Guided");
            case SettingsCategory.Printers:
                var printers = settings.LoadPrinters();
                return printers.Count == 0 ? "No printer checked yet" : string.Join(" and ", printers.Select(p => p.Name)) + " checked";
            case SettingsCategory.Data:
                var sync = PhoneSync.Setup;
                return sync.On ? GroupLab.Core.Sync.SyncScreens.Status(sync, DateTime.UtcNow) ?? "Sync on" : "Sync off · export and import";
            case SettingsCategory.Sharing:
                var sent = new List<string>();
                if (settings.LoadSending().Choice is SendingChoice.Always or SendingChoice.Everything)
                {
                    sent.Add("targets");
                }

                if (settings.LoadErrorChoice() == ErrorReportChoice.Always)
                {
                    sent.Add("error reports");
                }

                if (settings.LoadSurveyChoice() == SurveyChoice.Yes)
                {
                    sent.Add("the survey");
                }

                return sent.Count == 0 ? "Sends nothing by itself" : "Sends " + string.Join(", ", sent);
            case SettingsCategory.Updates:
                return $"GroupLab {AppInfo.ShortVersion}";
            default:
                return "Report a problem · camera diagnostics";
        }
    }

    /// <summary>Opens <paramref name="category"/>'s page, as tapping it in the list does.</summary>
    internal void Show(SettingsCategory category)
    {
        Open = category;
        search.Text = "";
        pages.Show(category);
        list.IsVisible = false;
        scroll.Offset = default;
        DiagnosticLog.Info("ui.settings", ("page", category.ToString()));
    }

    /// <summary>Back to the list of categories.</summary>
    internal void ShowList()
    {
        Open = null;
        list.IsVisible = true;
        Filter();
        FillList();
        scroll.Offset = default;
    }

    private void Filter()
    {
        if (Open is not null && !string.IsNullOrWhiteSpace(search.Text))
        {
            Open = null;
            list.IsVisible = true;
        }

        int found = pages.Filter(search.Text);
        bool searching = pages.Query is not null;
        categoriesCard.IsVisible = !searching;
        nothing.IsVisible = searching && found == 0;
        if (!searching && Open is null)
        {
            foreach (var page in pages.Pages)
            {
                page.Root.IsVisible = false;
            }
        }
    }

    /// <summary>Searches as typing in "Find a setting" does; the number of groups found.</summary>
    internal int Find(string words)
    {
        search.Text = words;
        Filter();
        return pages.Query is null ? 0 : pages.Pages.Sum(p => p.Cards.Count(c => c.Root.IsVisible));
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
