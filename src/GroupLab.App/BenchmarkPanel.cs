using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Survey;
using Button = Avalonia.Controls.Button;
using ProgressBar = Avalonia.Controls.ProgressBar;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 227 section 2: the benchmark as something a person can watch. It starts only from its button, shows the
/// stage it has reached and the seconds so far with a bar, can be cancelled, and says when it finished and what it found; a cancelled or
/// failed run keeps nothing. The first run screen offers it after Yes, with Later beside it, and Settings has it under Sharing with the
/// last run's date and result. The desktop and the phone use this same control.
/// </summary>
public sealed class BenchmarkPanel : StackPanel
{
    /// <summary>How many stages a run is taken to have before one has finished; a finished run's own count replaces it.</summary>
    public const int UsualStages = 12;

    private readonly AppSettingsStore store;
    private readonly Func<(TargetDefinition? Definition, IImagingBackend Backend)> prepare;
    private readonly Func<Task>? afterRun;
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar bar = new() { Minimum = 0, Maximum = 1, IsVisible = false, MinHeight = 8 };
    private readonly Button run;
    private readonly Button cancel;
    private CancellationTokenSource? cancellation;
    private int stagesDone;

    /// <param name="runWords">The start button's words: "Run it now" on the first run screen, "Run the benchmark now" in Settings.</param>
    /// <param name="later">Where there is a Later, what it does; null for no Later.</param>
    public BenchmarkPanel(AppSettingsStore store, Func<(TargetDefinition? Definition, IImagingBackend Backend)> prepare, Func<Task>? afterRun, string runWords, Action? later)
    {
        this.store = store;
        this.prepare = prepare;
        this.afterRun = afterRun;
        Spacing = Tokens.Space8;
        run = new Button { Content = runWords, MinHeight = 44 };
        run.Click += (_, _) => Start();
        cancel = new Button { Content = SharingWords.BenchmarkCancel, MinHeight = 44, IsVisible = false };
        cancel.Click += (_, _) => Cancel();
        var buttons = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        buttons.Children.Add(run);
        buttons.Children.Add(cancel);
        if (later is not null)
        {
            var laterButton = new Button { Content = SharingWords.BenchmarkLater, MinHeight = 44, Margin = new Avalonia.Thickness(Tokens.Space8, 0, 0, 0) };
            laterButton.Click += (_, _) => later();
            buttons.Children.Add(laterButton);
        }

        Children.Add(buttons);
        Children.Add(bar);
        Children.Add(status);
    }

    /// <summary>The run in progress, for the headless tests to wait on.</summary>
    public Task? Running { get; private set; }

    /// <summary>What the panel says, for the tests.</summary>
    public string StatusText => status.Text ?? "";

    public bool CanCancel => cancel.IsVisible;

    /// <summary>Raised on the window's thread when a run ends, however it ends.</summary>
    public event Action? Ended;

    /// <summary>Sets what the panel says before anything has run: Settings shows the last run here.</summary>
    public void Say(string text) => status.Text = text;

    /// <summary>Entry 356 section 3: where a failure is said in the middle of the window, on the desktop; the phone leaves it in the panel.</summary>
    public Action<string, string>? Problem { get; set; }

    public void Start()
    {
        if (Running is { IsCompleted: false })
        {
            return;
        }

        var (definition, backend) = prepare();
        if (definition is null)
        {
            status.Text = "The benchmark's target is missing from this installation.";
            Problem?.Invoke("The benchmark cannot run", status.Text);
            return;
        }

        int expected = store.LoadBenchmark()?.Result.Stages.Count is > 0 and var kept ? kept : UsualStages;
        stagesDone = 0;
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        run.IsVisible = false;
        cancel.IsVisible = true;
        bar.IsVisible = true;
        bar.Value = 0;
        status.Text = SharingWords.BenchmarkProgress(0, expected, 0);
        var timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) =>
            status.Text = SharingWords.BenchmarkProgress(stagesDone, expected, clock.Elapsed.TotalSeconds));
        timer.Start();
        DiagnosticLog.Info("survey.benchmark.start", ("expected", expected));
        Running = Go();

        async Task Go()
        {
            string ending;
            try
            {
                var result = await Task.Run(() => Benchmark.Run(definition, backend, token, _ => Dispatcher.UIThread.Post(() =>
                {
                    stagesDone++;
                    bar.Value = Math.Min(0.95, stagesDone / (double)expected);
                })), token);
                bool goes = store.LoadSurveyChoice() == SurveyChoice.Yes;
                store.SaveBenchmark(result, sent: false, ranAt: DateTimeOffset.UtcNow, toSend: goes);
                DiagnosticLog.Info("survey.benchmark", ("ms", result.TotalMilliseconds), ("peak", result.PeakMegabytes), ("found", result.HolesFound));
                ending = SharingWords.BenchmarkDone(result, goes)
                    + string.Create(CultureInfo.CurrentCulture, $" Finished at {DateTime.Now:HH:mm}.");
                bar.Value = 1;
                status.Text = ending;
                if (goes && afterRun is not null)
                {
                    await afterRun();
                }
            }
            catch (OperationCanceledException)
            {
                DiagnosticLog.Info("survey.benchmark.cancelled", ("seconds", (long)clock.Elapsed.TotalSeconds));
                status.Text = SharingWords.BenchmarkCancelled;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                DiagnosticLog.Exception(LogLevel.Warn, "survey.benchmark.failed", ex, []);
                status.Text = SharingWords.BenchmarkFailed;
                Problem?.Invoke("The benchmark did not finish", status.Text);
            }
            finally
            {
                timer.Stop();
                cancel.IsVisible = false;
                run.IsVisible = true;
                bar.IsVisible = status.Text != SharingWords.BenchmarkCancelled && status.Text != SharingWords.BenchmarkFailed;
                cancellation?.Dispose();
                cancellation = null;
                Ended?.Invoke();
            }
        }
    }

    public void Cancel() => cancellation?.Cancel();
}
