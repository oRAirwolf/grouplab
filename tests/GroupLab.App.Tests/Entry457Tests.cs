using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 457 section 1, crash report 37: on nightly 204, Next unsure hole and the autosave raised
/// "Index was out of range" from the focus keeping, four times in one session. A control whose visual parent is a panel but which is not one
/// of its Children was recorded at place -1, and putting the focus back read Children[-1]. Keeping the focus is a convenience; it must never
/// raise anything.
/// </summary>
public class Entry457Tests
{
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>A panel that hosts a control as a visual child of its own, not one of its Children, as a presenter or a template does.</summary>
    private sealed class Hosting : StackPanel
    {
        private Control hosted;

        public Hosting(Control hosted)
        {
            this.hosted = hosted;
            VisualChildren.Add(hosted);
            Children.Add(new Button { Content = "Ordinary child" });
        }

        /// <summary>The hosted control rebuilt, as a template's or a presenter's content is.</summary>
        public void Rebuild(Control next)
        {
            VisualChildren.Remove(hosted);
            hosted = next;
            VisualChildren.Add(next);
        }
    }

    [AvaloniaFact]
    public void LosingTheFocusInsideAControlThatIsNotAPanelChildRaisesNothing()
    {
        var button = new Button { Content = "Next unsure hole", Focusable = true };
        var inner = new StackPanel { Children = { button } };
        var host = new Hosting(inner);
        var outer = new StackPanel { Children = { host, new Button { Content = "Other" } } };
        var window = new Window { Width = 400, Height = 300, Content = outer };
        var raised = new List<Exception>();
        void Caught(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            raised.Add(e.Exception);
            e.Handled = true;
        }

        Dispatcher.UIThread.UnhandledException += Caught;
        try
        {
            window.Show();
            KeyboardFocus.Attach(window);
            Settle();
            button.Focus(NavigationMethod.Tab);
            Settle();

            // The rebuild: the hosted control replaced while its host stays, as the marking's panel is after Next unsure hole and the
            // autosave; the host is the nearest ancestor still in the window, and the old child's place in it was -1.
            host.Rebuild(new StackPanel { Children = { new Button { Content = "Next unsure hole" } } });
            Settle();
            Assert.Empty(raised);
        }
        finally
        {
            Dispatcher.UIThread.UnhandledException -= Caught;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NextUnsureHoleFromTheMouseAndTheKeyboardRaisesNothing()
    {
        var (window, _, _) = Entry109Tests.Sheet();
        var raised = new List<Exception>();
        void Caught(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            raised.Add(e.Exception);
            e.Handled = true;
        }

        Dispatcher.UIThread.UnhandledException += Caught;
        try
        {
            Settle();
            for (int i = 0; i < 3; i++)
            {
                window.NextUnsureHole();
                Settle();
            }

            window.SaveWaitingNow();
            Settle();
            Assert.Empty(raised);
        }
        finally
        {
            Dispatcher.UIThread.UnhandledException -= Caught;
            window.Close();
        }
    }

    /// <summary>Entry 457 section 2: Send is the report window's first button while the receiver is open; the email and GitHub way is the fallback.</summary>
    [AvaloniaFact]
    public void TheReportWindowSendsFirstAndTheWayRoundIsOnlyTheFallback()
    {
        foreach (bool open in new[] { true, false })
        {
            var report = new GroupLab.App.Diagnostics.ReportWindow(null, null, null, receiverOpen: open);
            try
            {
                report.Show();
                Settle();
                var buttons = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(report).OfType<Button>().Where(b => b.IsVisible).Select(b => b.Content as string).ToList();
                var texts = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(report).OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text ?? "").ToList();
                if (open)
                {
                    Assert.Equal("Send", buttons[0]);
                    Assert.DoesNotContain(texts, t => t.Contains(GroupLab.App.Diagnostics.ReportWindow.Repository, StringComparison.Ordinal));
                }
                else
                {
                    Assert.DoesNotContain("Send", buttons);
                    Assert.Contains(texts, t => t.StartsWith(GroupLab.App.Diagnostics.ReportWindow.NotOpen, StringComparison.Ordinal));
                }
            }
            finally
            {
                report.Close();
            }
        }
    }
}
