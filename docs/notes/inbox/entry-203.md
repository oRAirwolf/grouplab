## 2026-09-25, entry 203: the consent choices on the first run screen are cut off

Alan opened nightly 102 and got the first run question "Send your targets to help improve GroupLab?". The two consent choices run off the
right edge of the card and are cut mid sentence: "Testing only. I took these photos, or I have permission to share them. GroupLab may use
them to test a" and "May be published. I took these photos, or I have permission to share them. GroupLab may use them to". Everything else
on the card wraps. A person cannot read what they are agreeing to, which for a consent choice is the one text that must never be cut.

## 1. The cause, as read from the code

`MainWindow.Sending.cs` line 308 onward builds both radio buttons with `Content = "Testing only. " + terms.TestingText` and
`"May be published. " + terms.PublishableText`: a plain string, which Avalonia shows on one line. The Settings section's radios (line 383)
are built the same way and will have the same fault.

## 2. What to do

1. Give every radio button, check box and button whose text can be long a wrapping text block as its content (`TextWrapping.Wrap`), in the
   first run screen, the question after Accept and analyze, the Sending targets and error report sections of Settings, and anywhere else
   the same pattern appears. `PressSend` and any test that reads `Content as string` must read the text block instead.
2. **A test that no text is cut, anywhere it matters.** Render the first run screen, the sending question and each Settings section at the
   narrowest window the application allows and at 150 and 200 percent display scale, and fail if any text block, radio or check box is
   wider than its container or ends in a clipped line. Consent text first; if a general check is practical, run it over every dialog.
3. **Confirm nothing is chosen for the user.** The screenshot shows "May be published" selected, which may simply be Alan's click. Check
   that neither level is ever preselected, on the first run screen, the question or in Settings, and that a test holds it. Consent is chosen,
   never defaulted.
4. Keep the full consent wording exactly as it is; this is layout only.

Report in plain words for Alan: fixed in which build, and whether any other screen had text cut off.
