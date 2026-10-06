using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Publication;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 357 section 1 on the phone, in both states of the switch: off, Settings and the first run screen offer the
/// three choices in their old words and nothing about mobile data; on, the four choices, the line that says what everything means, the
/// mobile data switch off by default, and the one-time question for somebody who chose every target automatically.
/// </summary>
public class Entry357Tests
{
    private static AppSettingsStore Fresh(out string folder)
    {
        folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry357-{Guid.NewGuid():N}");
        return new AppSettingsStore(Path.Combine(folder, "settings.json"));
    }

    private static IReadOnlyList<string> Words(Control view)
    {
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var words = view.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")
            .Concat(view.GetLogicalDescendants().OfType<ContentControl>().Select(c => c.Content as string ?? "")).ToList();
        window.Close();
        return words;
    }

    private static void Start()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }
    }

    [AvaloniaFact]
    public void WithTheSwitchOffTheChoicesAreAsBefore()
    {
        var store = Fresh(out string folder);
        try
        {
            Start();
            SharingSwitches.EverythingOverride = false;
            // Entry 379 switched the phones' target sender on; this holds the switches-off case, so it is off here too.
            SharingSwitches.TargetsFromPhoneOverride = false;
            SharingSwitches.FullLogOverride = false;
            store.SaveSending(SendingChoice.Always, ConsentLevel.Testing);
            var words = Words(new SettingsView(store));
            if (ReceiverTerms.Current.AppOpen)
            {
                Assert.Contains("Send every target automatically", words);
            }

            Assert.DoesNotContain(words, w => w.Contains("everything I open", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(SharingWords.MobileData, words);
        }
        finally
        {
            SharingSwitches.EverythingOverride = null;
            SharingSwitches.TargetsFromPhoneOverride = null;
            SharingSwitches.FullLogOverride = null;
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void WithTheSwitchOnTheFourChoicesAndMobileDataAreOffered()
    {
        var store = Fresh(out string folder);
        try
        {
            Start();
            SharingSwitches.EverythingOverride = true;
            store.SaveSending(SendingChoice.Always, ConsentLevel.Testing);
            Assert.True(store.EverythingQuestionDue());
            var view = new SettingsView(store);
            var words = Words(view);
            if (ReceiverTerms.Current.AppOpen)
            {
                Assert.Contains("Send everything I open, to help improve GroupLab", words);
                Assert.Contains("Send finished targets only", words);
                Assert.Contains(SharingWords.MobileData, words);
                Assert.Contains(SharingWords.EverythingQuestion, words);
                Assert.False(store.LoadMobileData());
            }
        }
        finally
        {
            SharingSwitches.EverythingOverride = null;
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void TheFirstRunScreenAsksOnceUnderTheNewWording()
    {
        var store = Fresh(out string folder);
        try
        {
            Start();
            store.SaveScopeAnswer(ScopeAnswer.Moa, GroupLab.Core.Marking.LinearUnit.Inch);
            store.SaveErrorChoice(GroupLab.App.Diagnostics.ErrorReportChoice.Never);
            store.SaveSurveyChoice(GroupLab.Core.Survey.SurveyChoice.No);
            store.SaveSending(SendingChoice.Always, ConsentLevel.Publishable);
            SharingSwitches.EverythingOverride = false;
            Assert.False(FirstRunView.Due(store));
            SharingSwitches.EverythingOverride = true;
            Assert.Equal(ReceiverTerms.Current.AppOpen, FirstRunView.Due(store));
            store.SaveEverythingAsked();
            Assert.False(FirstRunView.Due(store));
            Assert.Equal((SendingChoice.Always, (ConsentLevel?)ConsentLevel.Publishable), store.LoadSending());
        }
        finally
        {
            SharingSwitches.EverythingOverride = null;
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>Section 2 on the phone: automatic reports chosen under the thinner wording are asked about again only once the log goes.</summary>
    [AvaloniaFact]
    public void AutomaticReportsAreAskedAgainUnderTheNewWording()
    {
        var store = Fresh(out string folder);
        try
        {
            Start();
            store.SaveScopeAnswer(ScopeAnswer.Moa, GroupLab.Core.Marking.LinearUnit.Inch);
            store.SaveSending(SendingChoice.Never, null);
            store.SaveSurveyChoice(GroupLab.Core.Survey.SurveyChoice.No);
            store.SaveErrorChoice(GroupLab.App.Diagnostics.ErrorReportChoice.Always);
            SharingSwitches.FullLogOverride = false;
            Assert.False(FirstRunView.Due(store));
            SharingSwitches.FullLogOverride = true;
            Assert.Equal(ReceiverTerms.Current.ErrorReportsOpen, FirstRunView.Due(store));
            Assert.Contains(SharingWords.ErrorsWordingChanged, Words(new FirstRunView(store, () => { })));
            store.SaveErrorChoice(GroupLab.App.Diagnostics.ErrorReportChoice.Always, fullLogWording: true);
            Assert.False(FirstRunView.Due(store));
        }
        finally
        {
            SharingSwitches.FullLogOverride = null;
            Directory.Delete(folder, recursive: true);
        }
    }
}
