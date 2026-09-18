using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 104 section 1: a status line is a claim like any other. Question 15 stayed marked open for three days after
/// entry 52, headed "question 15 answered", answered it, and entry 103 then asked for work that was already done. So a question marked open
/// must not be one an entry's heading says it answers.
/// </summary>
public partial class QuestionStatusTests
{
    [Fact]
    public void NoQuestionMarkedOpenIsOneAnEntrySaysItAnswered()
    {
        var answered = new Dictionary<int, string>();
        foreach (string heading in File.ReadAllLines(Repo.PathTo("docs", "NOTES-FROM-PLANNING.md")).Where(l => l.StartsWith("## ", StringComparison.Ordinal)))
        {
            foreach (Match match in AnsweredQuestions().Matches(heading))
            {
                foreach (Match number in Number().Matches(match.Groups["list"].Value))
                {
                    answered[int.Parse(number.Value, System.Globalization.CultureInfo.InvariantCulture)] = heading;
                }
            }
        }

        Assert.True(answered.Count >= 5, $"only {answered.Count} questions found answered in the notes' headings; they have named at least five since entry 13, so the headings are probably not being read.");

        string[] questions = File.ReadAllLines(Repo.PathTo("docs", "QUESTIONS-FOR-PLANNING.md"));
        int checkedCount = 0;
        for (int i = 0; i < questions.Length; i++)
        {
            if (QuestionHeading().Match(questions[i]) is not { Success: true } q)
            {
                continue;
            }

            string status = questions.Skip(i + 1).FirstOrDefault(l => l.StartsWith("**Status:", StringComparison.Ordinal)) ?? "";
            int id = int.Parse(q.Groups["id"].Value, System.Globalization.CultureInfo.InvariantCulture);
            checkedCount++;
            Assert.False(
                status.StartsWith("**Status: open", StringComparison.Ordinal) && answered.TryGetValue(id, out _),
                $"question {id} is marked open in docs/QUESTIONS-FOR-PLANNING.md, and the notes say it was answered: \"{(answered.TryGetValue(id, out string? by) ? by : "")}\". Mark it answered, or correct the heading.");
        }

        Assert.True(checkedCount >= 19, $"only {checkedCount} questions read from docs/QUESTIONS-FOR-PLANNING.md");
    }

    [GeneratedRegex(@"\bquestions? (?<list>\d+(?:(?:, | and )\d+)*) answered")]
    private static partial Regex AnsweredQuestions();

    [GeneratedRegex(@"\d+")]
    private static partial Regex Number();

    [GeneratedRegex(@"^## \d{4}-\d{2}-\d{2}, question (?<id>\d+):")]
    private static partial Regex QuestionHeading();
}
