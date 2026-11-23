# Accessibility checked by machine

NOTES-FROM-PLANNING.md entry 514 section 3, on 2026-11-23: what is held by automation, ahead of request 105's sitting with a real screen
reader, which no machine replaces. Every row is a test or a workflow step that fails when it finds something.

| What | Computer (Windows, macOS, Linux) | Phone, headless (Android, iPhone, iPad) | Android emulator, iOS simulator | Found on 2026-11-23 |
|---|---|---|---|---|
| A name on every control a person can press, type into or choose | Every screen and every Settings page, and the windows GroupLab opens (`Entry447Tests`, `Entry447WindowsTests`), now in all three themes (`Entry514AccessibilityTests`) | Every place along the bottom and every page under one (`Entry461PhoneAccessibilityTests`, `PagesUnderPlacesAuditTests`) | Each screen of the sweep against the platform's own tree (`scripts/android-audit.sh`, the iOS simulator's screen reader audit) | Nothing; the emulator's audit found nothing on its 13 screens (run 45159520629) |
| Order by keyboard | Tab goes round every screen and window without losing the focus or being held in one panel, and every stop is named (`Entry447Tests`) | (no keyboard) | | Nothing |
| Order by the accessibility tree | | Every place in both themes is read top to bottom, a row at a time, with the bar along the bottom after the page (`Entry514PhoneReadingOrderTests`) | | Nothing; the check's own test proves it finds a button read out of order |
| Contrast, 4.5 to 1 for words (3 to 1 for large words) | Every screen and window in dark, light and high contrast (`Entry514AccessibilityTests`, through `LayoutAudit`) | Every place and page in both themes (`Entry462PhoneLayoutTests`, `PagesUnderPlacesAuditTests`) | | Nothing |
| Text at Android's largest size (200%), nothing cut off | | Every place at six phone sizes in both themes, and every page under a place (`Entry462PhoneLayoutTests`, `PagesUnderPlacesAuditTests`) | The sweep's largest-text pass, held to larger text than its plain pass (`scripts/phone-sweep-check.py --larger`) | Nothing |
| Touch targets of 48 by 48 | | Every control on every place (`Entry461PhoneAccessibilityTests`) | Every tappable node (`scripts/android-audit.sh`) | Nothing |

Nothing found needed a look decided, so no DESIGN NEEDED request was written. Still for a person: request 105, twenty minutes with a real
screen reader, since a machine can say that a control has a name but not whether the name makes sense read aloud in that place.
