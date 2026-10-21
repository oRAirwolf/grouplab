namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 5A: the first run asks one question at a time, with "Question N of M", a step bar, Back, Next
/// and Skip for now, on the computer and the phone alike (the phone's file links this one). Answering a question moves on to the next;
/// Skip for now moves on and records nothing, so a skipped question keeps the answer it had, unset, and is asked again at the next start;
/// Back returns to the question before, and Next passes over one already answered. Past the last, the first run is finished.
/// </summary>
internal sealed class FirstRunSteps
{
    public const string Back = "Back";
    public const string Next = "Next";
    public const string Skip = "Skip for now";

    private readonly bool[] answered;

    public FirstRunSteps(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        answered = new bool[count];
    }

    public int Count => answered.Length;

    /// <summary>The question showing, from 0; <see cref="Count"/> once the last has been passed.</summary>
    public int At { get; private set; }

    public bool Finished => At >= Count;

    public bool IsLast => At == Count - 1;

    public bool CanBack => !Finished && At > 0;

    public bool CanNext => !Finished && answered[At];

    public bool CanSkip => !Finished && !answered[At];

    /// <summary>Whether question <paramref name="index"/> was answered on this screen.</summary>
    public bool Answered(int index) => answered[index];

    /// <summary>"Question 2 of 4", for the question showing.</summary>
    public string Of => Words(Math.Min(At, Count - 1) + 1, Count);

    public static string Words(int question, int count) => $"Question {question} of {count}";

    /// <summary>What one part of the step bar is, for a screen reader: done, now or to come.</summary>
    public string BarName(int index) =>
        $"{Words(index + 1, Count)}, {(index == At ? "now" : answered[index] ? "answered" : index < At ? "skipped for now" : "to come")}";

    /// <summary>The question showing has been answered: it is marked so, and the next one shows.</summary>
    public void Answer()
    {
        if (!Finished)
        {
            answered[At] = true;
            At++;
        }
    }

    public void MoveNext()
    {
        if (CanNext)
        {
            At++;
        }
    }

    /// <summary>On without an answer: nothing is recorded for the question, and its setting stays as it was.</summary>
    public void SkipForNow()
    {
        if (CanSkip)
        {
            At++;
        }
    }

    public void MoveBack()
    {
        if (CanBack)
        {
            At--;
        }
    }
}
