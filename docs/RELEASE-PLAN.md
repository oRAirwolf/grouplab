# The first beta or stable release: what must be true, and how Windows builds get signed

NOTES-FROM-PLANNING.md entry 219 item D4. **A plan only.** No beta or stable release happens until Alan asks for one by name (entry 119
section 8); `release.yml` makes it, from a tag, as a draft first.

## What must be true first

Each line is checked, and says where, before Alan is asked whether to release.

1. **No open defect that loses work or gives a wrong figure.** The issues in `grouplab-crash-reports` are read at the start of every run;
   none may be open from a build newer than the one before the release, and every reported crash from the last two nightlies is explained.
2. **CI green on all three systems** on the commit to be released, and the phase 0 gate record passing.
3. **The minimums table** in `docs/PLATFORM-SUPPORT.md` is the one the release notes and the download page state, generated from one place
   (`scripts/platform-support.py`), and the hardware survey, once open, has not contradicted it.
4. **The privacy text is true**: the article what-grouplab-sends lists everything the build can send, and the build sends nothing else
   (`OneWayOutTests`).
5. **The user guide describes this build**: every screen it names is on screen, the guide's PDF is regenerated, and the tour pages match the
   week's screenshots (entry 146 section 4.4).
6. **The release notes** are written for a person who shoots, from the builds' own notes since the last numbered release.
7. **The Windows build is signed**, or the download page says plainly, as it does today, why it is not and what the warning means.
8. **The installed update path works from the previous nightly**, checked on one clean machine (entries 119 to 123).

## The Microsoft Store (entry 224 section 3)

Alan wants GroupLab in the Microsoft Store, with new releases pushed to it automatically. **Built:**

- `scripts/package-msix.ps1` makes an MSIX of the same self-contained build as the zip and the installer, stamped as the Store's copy, in
  which GroupLab's own updater is off and Settings says the Store keeps it up to date. The Store signs it on submission, so no certificate is
  needed. CI builds it on every push with a stand-in identity and checks what it holds.
- `release.yml` builds it with the identity Partner Center gave, from the repository's variables, and checks the package's Identity Name,
  Publisher and PublisherDisplayName against them, failing with the field's name on a mismatch (entry 293). A run by hand with
  `store_draft` builds only that package, runs the Windows App Certification Kit on it, puts it on a draft release named
  `store-draft-<version>`, which makes no tag and is never a `v*` name, and proves the Store login with one read of the product, submitting
  nothing. That draft is what the first submission uploads by hand. **After that, every tagged stable release is sent to the Store by
  itself**, with Microsoft's own Store tooling, as the Entra application Partner Center trusts as a Manager, and the same login check runs
  first.
- **The Store secret expires.** The client secret of the Entra application `grouplab-store-publisher` was made on 2026-09-29 and lasts 24
  months, so it expires about 2028-09-28. Make a new one in Entra by August 2028 and set it with
  `gh secret set AZURE_AD_APPLICATION_SECRET -R oRAirwolf/grouplab`. Until then, a release whose Store login fails says "the Store secret may
  have expired", with Entra's error code, and never the secret.
- **Versions only go up.** The Store refuses a package whose version is not above the last one it accepted. The first submission is the
  draft's `0.2.0.0`, so the next stable release sent to the Store has to be `0.2.1` or later.
- **What goes where:** stable releases go to the Store's public listing when Alan asks for one by name, as today. Nightlies stay on GitHub
  only. If a beta train exists later, its builds can go to a Store package flight for testers; nothing is built for that until it exists.
- The Store needs Windows 10 version 1809 or later for an MSIX, later than the downloaded version's 1607, and the minimums table says so.
- The listing, ready to paste, is `docs/store/LISTING.md`. Alan's part is request 38.

## Signing Windows builds: the options, as of September 2026

Unsigned, a download of GroupLab shows Microsoft Defender SmartScreen's warning until the build has built a reputation, and every new build
starts again. The ways out:

| Option | Cost | What it does for SmartScreen | What it asks of Alan |
|---|---|---|---|
| **Microsoft Store** | Free for individual developers | The Store signs what it distributes, so a Store install shows no warning | A Store account and an MSIX package; GroupLab would still need the direct download for Linux-style updates and for people who avoid the Store |
| **Azure Artifact Signing** (formerly Trusted Signing) | $9.99 a month, about $120 a year, for 5,000 signatures | Signed by a Microsoft-issued certificate; reputation still builds with downloads, but it attaches to the publisher, not to each build | A paid Azure subscription and an identity check; open to individuals in the United States and Canada |
| **An OV code signing certificate** | About $216 to $386 a year | Reputation builds with downloads, the same as the others | A one year certificate on a hardware token or cloud HSM, re-issued every year |
| **An EV certificate** | More than OV | Since 2024 no longer skips the reputation period; the same as OV | As OV, with a stricter check |

**Recommendation: Azure Artifact Signing for the direct download, and the Microsoft Store when the Store package is worth making.** It is
the cheapest way to sign every nightly and release from CI with no key file to keep, it is Microsoft's own, and it costs about half an OV
certificate. The Store is free and removes the warning for everyone who installs from it, but it needs an MSIX build and a listing, so it is
the second step. An EV certificate buys nothing extra any more.

**On hold** (entry 224, 2026-09-25): Alan: "As of right now, nobody is getting windows smart screen warnings. Lets hold off for now."
Request 37 is closed as not yet; this comparison is kept for when it is revisited.

Sources: [Artifact Signing pricing](https://azure.microsoft.com/en-us/pricing/details/artifact-signing/),
[Artifact Signing FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq),
[free Store registration for individual developers](https://learn.microsoft.com/en-us/windows/apps/publish/whats-new-individual-developer),
[SmartScreen reputation for Windows app developers](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation),
[OV code signing prices](https://signmycode.com/ov-code-signing).
