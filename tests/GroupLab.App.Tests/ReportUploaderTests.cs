using System.Net;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 41 section 7 and entry 45 sections 3 and 4: the request is one multipart POST with the zip in <c>report</c> and
/// the version in <c>version</c>; a success shows the server's reference; a refusal shows the server's error verbatim; anything else keeps the
/// zip and says where; and the Send button exists only when an address is configured.
/// </summary>
public sealed class ReportUploaderTests : IDisposable
{
    private readonly string root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-upload-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        GroupLab.Tests.Support.Temp.Delete(root);
        GC.SuppressFinalize(this);
    }

    /// <summary>A server that records what it was sent and answers as told.</summary>
    private sealed class FakeServer(HttpStatusCode status, string reply, bool fail = false) : HttpMessageHandler
    {
        public string? Method { get; private set; }

        public string? Body { get; private set; }

        public string? ContentType { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (fail)
            {
                throw new HttpRequestException("No connection could be made");
            }

            Method = request.Method.Method;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? null : Encoding.Latin1.GetString(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            return new HttpResponseMessage(status) { Content = new StringContent(reply, Encoding.UTF8, "application/json") };
        }
    }

    private string Zip()
    {
        string path = Path.Combine(root, "grouplab-report-20260915-070000.zip");
        ReportPackage.Build(path, null, null, null, "environment", "what happened", null);
        return path;
    }

    [Fact]
    public async Task TheRequestCarriesTheZipAsReportAndTheVersionAndASuccessShowsTheReference()
    {
        var server = new FakeServer(HttpStatusCode.OK, "{ \"ok\": true, \"reference\": \"2026-09-15_1a2b3c4d\", \"sha256\": \"6b80184e\" }");
        var result = await ReportUploader.SendAsync("https://reports.example.invalid/api/crash-report.php", Zip(), "0.1.0+3f9c2a1", server, TestContext.Current.CancellationToken);

        Assert.True(result.Sent);
        Assert.Equal("2026-09-15_1a2b3c4d", result.Reference);
        Assert.Contains("2026-09-15_1a2b3c4d", result.Message, StringComparison.Ordinal);
        Assert.Equal("POST", server.Method);
        Assert.Equal("multipart/form-data", server.ContentType);
        Assert.Contains("name=report; filename=grouplab-report-20260915-070000.zip", server.Body, StringComparison.Ordinal);
        Assert.Contains("name=version", server.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefusalShowsTheServersErrorVerbatimAndNothingElseIsRetried()
    {
        const string error = "That report package contains a file this server does not accept: IMG_1580.jpg";
        var result = await ReportUploader.SendAsync("https://reports.example.invalid/", Zip(), "0.1.0", new FakeServer(HttpStatusCode.UnprocessableEntity, $"{{ \"ok\": false, \"error\": \"{error}\" }}"), TestContext.Current.CancellationToken);
        Assert.False(result.Sent);
        Assert.Equal(error, result.Message);

        var unreadable = await ReportUploader.SendAsync("https://reports.example.invalid/", Zip(), "0.1.0", new FakeServer(HttpStatusCode.BadGateway, "<html>bad gateway</html>"), TestContext.Current.CancellationToken);
        Assert.False(unreadable.Sent);
        Assert.Contains("HTTP 502", unreadable.Message, StringComparison.Ordinal);
        Assert.Contains("kept at", unreadable.Message, StringComparison.Ordinal);

        var offline = await ReportUploader.SendAsync("https://reports.example.invalid/", Zip(), "0.1.0", new FakeServer(HttpStatusCode.OK, "", fail: true), TestContext.Current.CancellationToken);
        Assert.False(offline.Sent);
        Assert.Contains("kept at", offline.Message, StringComparison.Ordinal);

        var plain = await ReportUploader.SendAsync("http://reports.example.invalid/", Zip(), "0.1.0", new FakeServer(HttpStatusCode.OK, "{}"), TestContext.Current.CancellationToken);
        Assert.False(plain.Sent);
    }

    [AvaloniaFact]
    public void TheSendButtonExistsOnlyWhenAnAddressIsConfigured()
    {
        string run = Path.Combine(root, "grouplab-20260915-064100-4242.log");
        File.WriteAllText(run, "a run");

        var unconfigured = new ReportWindow(null, run, null, sendUrl: "");
        unconfigured.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain(unconfigured.GetLogicalDescendants().OfType<Button>(), b => (b.Content as string) == "Send" && b.IsVisible);
        unconfigured.Close();

        var configured = new ReportWindow(null, run, null, sendUrl: "https://reports.example.invalid/api/crash-report.php");
        configured.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(configured.GetLogicalDescendants().OfType<Button>(), b => (b.Content as string) == "Send" && b.IsVisible);
        configured.Close();
    }
}
