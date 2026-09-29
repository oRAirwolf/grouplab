using System.Runtime.InteropServices;
using Foundation;
using GroupLab.iOS;
using ObjCRuntime;
using UIKit;
using UniformTypeIdentifiers;

namespace GroupLab.Share;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 2.3: what GroupLab shows in another app's share sheet. It copies each shared picture whole into
/// the app group, the photograph's own file and never a copy made for the screen, and opens GroupLab, which reads them straight into
/// analysis. Where iOS will not let it open GroupLab, it says the pictures are waiting, and GroupLab reads them the next time it is opened.
/// It reads nothing in the pictures, their location least of all.
/// </summary>
[Register("GroupLabShareViewController")]
public sealed class ShareViewController : UIViewController
{
    private readonly UILabel words = new()
    {
        Lines = 0,
        TextAlignment = UITextAlignment.Center,
        TranslatesAutoresizingMaskIntoConstraints = false,
    };

    private readonly UIButton done = UIButton.FromType(UIButtonType.System);
    private bool started;

    public ShareViewController()
    {
    }

    public ShareViewController(NativeHandle handle)
        : base(handle)
    {
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.SystemBackground;
        words.Text = "Getting the picture ready for GroupLab";
        done.SetTitle("Done", UIControlState.Normal);
        done.TranslatesAutoresizingMaskIntoConstraints = false;
        done.Hidden = true;
        done.TouchUpInside += (_, _) => Finish();
        View.AddSubview(words);
        View.AddSubview(done);
        NSLayoutConstraint.ActivateConstraints(
        [
            words.CenterYAnchor.ConstraintEqualTo(View.CenterYAnchor),
            words.LeadingAnchor.ConstraintEqualTo(View.LayoutMarginsGuide.LeadingAnchor),
            words.TrailingAnchor.ConstraintEqualTo(View.LayoutMarginsGuide.TrailingAnchor),
            done.TopAnchor.ConstraintEqualTo(words.BottomAnchor, 24),
            done.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
        ]);
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
        if (started)
        {
            return;
        }

        started = true;
        _ = Receive();
    }

    private async Task Receive()
    {
        string? incoming = Handoff.Incoming();
        if (incoming is null)
        {
            Say("GroupLab could not receive the picture on this device. Save it to Photos or Files, then choose it in GroupLab.");
            return;
        }

        var providers = (ExtensionContext?.InputItems ?? [])
            .SelectMany(item => item.Attachments ?? [])
            .Where(p => p.HasItemConformingTo("public.image"))
            .ToList();
        string batch = Handoff.Begin(incoming);
        int written = 0;
        for (int i = 0; i < providers.Count; i++)
        {
            if (await Copy(providers[i], batch, i))
            {
                written++;
            }
        }

        if (written == 0)
        {
            Directory.Delete(batch, recursive: true);
            Say(providers.Count == 0
                ? "Nothing GroupLab can read was shared. Share a photograph of a target."
                : "GroupLab could not get the picture. If it is kept only in the cloud, the phone has to be online to fetch it: connect and share it again.");
            return;
        }

        Handoff.Finish(batch);
        if (OpenGroupLab())
        {
            Finish();
            return;
        }

        Say(written == 1
            ? "The picture is waiting in GroupLab. Open GroupLab to read it."
            : $"The {written} pictures are waiting in GroupLab. Open GroupLab to read them.");
    }

    /// <summary>
    /// One shared picture copied whole into the share's folder: the file itself where the app offers one, which iOS downloads first where
    /// it is kept only in the cloud, else the bytes the app hands over. False where it gave neither.
    /// </summary>
    private static async Task<bool> Copy(NSItemProvider provider, string batch, int index)
    {
        string type = provider.RegisteredTypeIdentifiers.FirstOrDefault(id => UTType.CreateFromIdentifier(id)?.ConformsTo(UTTypes.Image) == true) ?? "public.image";
        string extension = UTType.CreateFromIdentifier(type)?.PreferredFilenameExtension ?? "jpg";
        string to = Path.Combine(batch, Handoff.Name(index, extension));

        var file = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.LoadFileRepresentation(type, (url, error) =>
        {
            try
            {
                if (error is null && url?.Path is { } from && File.Exists(from))
                {
                    File.Copy(from, to, overwrite: true);
                    file.TrySetResult(true);
                    return;
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Tried again below as bytes.
            }

            file.TrySetResult(false);
        });
        if (await file.Task)
        {
            return true;
        }

        var data = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.LoadDataRepresentation(type, (bytes, error) =>
        {
            try
            {
                if (error is null && bytes is { Length: > 0 })
                {
                    using var stream = bytes.AsStream();
                    using var output = File.Create(to);
                    stream.CopyTo(output);
                    data.TrySetResult(true);
                    return;
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Nothing to hand over; the person is told.
            }

            data.TrySetResult(false);
        });
        return await data.Task;
    }

    /// <summary>
    /// Opens GroupLab at <c>grouplab://shared</c>. A share extension may not ask iOS for the application object, so it is found where it
    /// always is, at the end of the chain of things on screen, and asked there; false where it is not found.
    /// </summary>
    private bool OpenGroupLab()
    {
        using var address = new NSUrl(Handoff.Address);
        var open = new Selector("openURL:options:completionHandler:");
        for (UIResponder? responder = this; responder is not null; responder = responder.NextResponder)
        {
            if (responder is UIApplication && responder.RespondsToSelector(open))
            {
                using var options = new NSDictionary();
                Send(responder.Handle, open.Handle, address.Handle, options.Handle, IntPtr.Zero);
                return true;
            }
        }

        return false;
    }

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void Send(IntPtr receiver, IntPtr selector, IntPtr url, IntPtr options, IntPtr completion);

    private void Say(string sentence) => InvokeOnMainThread(() =>
    {
        words.Text = sentence;
        done.Hidden = false;
    });

    private void Finish() => InvokeOnMainThread(() => ExtensionContext?.CompleteRequest([], null));
}
