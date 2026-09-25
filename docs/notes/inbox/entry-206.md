## 2026-09-25, entry 206: the phones GroupLab must run on, and the budget that sets

Alan asked for a study of the current phone market in the Americas and Europe, in the spirit of the Steam hardware survey, to decide how
much CPU, memory, storage, camera and computation the application may use, and what the minimum is. The planning session researched it on
2026-09-25. Put the findings in `docs/ANDROID.md` as a new section, "The phones it must run on", with the sources, and hold the design to
the budget once Alan approves it (section 2). Where a figure is judgment rather than measurement, it says so.

## 1. What the market looks like

Android against iPhone (StatCounter, web traffic, August 2026): United States iOS 60.7%, Android 39.3%; North America 60.3 / 39.7; United
Kingdom 51.4 / 48.6; Germany 27.6 / 72.4; Europe 37.3 / 62.7; South America 23.1 / 76.9.

Android versions in use, cumulative (apilevels.com from StatCounter, April 2026): 16+ 22.3%, 15+ 41.0%, 14+ 54.5%, 13+ 68.9%, 12+ 78.8%,
11+ 86.9%, 10+ 91.1%, 9+ 93.5%, 8+ 96.1%, 7+ 96.6%.

iOS versions (TelemetryDeck, end of August 2026): iOS 26 86.6%, iOS 18 7.9%, iOS 27 3.3%. Oldest iPhone on iOS 27: iPhone 11 (2019, 4 GB).

What sells: the iPhone 17 was the best selling phone in the US, UK, Germany and France in Q2 2026. Latin America's 2025 top ten was almost
all budget Android under 200 dollars: Galaxy A06 first (7%), Moto G15, Redmi 14C, Moto G05, Redmi A5, Moto G35, Redmi Note 14 4G, Galaxy
A16, A15 and A56.

Memory and storage: no public survey gives installed RAM by region. AnTuTu's Q1 2026 report on Android outside China, which skews toward
enthusiasts, shows 4 GB or less 7.6%, 6 GB 9.8%, 8 GB 39.3%, 12 GB 36.1%, 16 GB 6.8%; storage 128 GB 26.1%, 256 GB 49.7%. Treat it as the
upper bound; the Latin American best sellers ship with 4 GB and 64 or 128 GB.

Speed (Geekbench 6 single and multi core): Fold 7, Snapdragon 8 Elite, about 3196 and 10142; Galaxy A16 5G, Exynos 1330, about 960 and
1826; Galaxy A06, Helio G85, about 405 and 1349.

Cameras: every phone above has 12 MP output or more with autofocus. A Letter sheet framed with margin spans about 13 inches of a 4000 pixel
image, so 12 MP gives roughly 300 pixels an inch and 8 MP about 250, against the quality score's perfect 150 and useless 50.

Android 17 adds a per app memory limit scaled from device RAM, counting native memory (where OpenCV's buffers live), formula unpublished; an
app over it is killed.

## 2. The proposed budget and minimum (judgment, for Alan to approve)

1. Minimum Android 10 (API 29), not 7: about 91% of Android devices; the phones dropped are 2019 or older with 2 to 3 GB, which could not
   hold the engine anyway. Say if the OpenCV build or CameraX makes a different floor better.
2. Minimum memory 4 GB. Peak memory under about 400 MB, aimed at 300 MB, on any image. Today's 716 MB is too much: measure how peak memory
   scales with image size, then process at a capped working resolution (for example the camera's 12 MP, a scan brought to about 300 dpi),
   after measuring the accuracy cost against full resolution on the same images.
3. Reference phones: Galaxy A16 class as the normal low end, Galaxy A06 class as the floor. Detection within about 10 s on the A16 class and
   30 s on the A06 class, with progress and cancel; live capture checks at 10 frames a second or better on the A06 class. Use the emulator
   with limited cores and memory as a rough stand in and say how rough; buying a Galaxy A16 is Alan's decision.
4. Camera at least 8 MP with autofocus, refused with the reason otherwise.
5. Installed app under about 100 MB; warn when free space falls under about 500 MB.
6. Screens down to 360 dp wide.

## 3. GroupLab's own hardware survey

Only with the consent sending targets and error reports already ask for: device model, Android version, RAM, cores, camera resolution,
working resolution, detection time and peak memory, nothing that identifies the person. A Steam style page on grouplab.org can then show what
GroupLab actually runs on. Design now, build with the real app. The Play Console device catalog adds the installed base later.

## 4. iPhone, for the record

Not planned; Alan's decision. iPhone is about 60% of US phones and half of UK phones. If ever reconsidered: floor iPhone 11 on iOS 26 or
later; Avalonia runs on iOS; the engine would need OpenCV built for iOS; GitHub's macOS build machines can build it without anyone owning a
Mac; distribution needs the paid Apple developer program. Record in `docs/PLATFORM-SUPPORT.md` as a fact, not a plan.

## 5. Sources

- https://www.digitalapplied.com/blog/mobile-os-market-share-2026-ios-vs-android
- https://gs.statcounter.com/os-market-share/mobile/north-america
- https://apilevels.com/
- https://telemetrydeck.com/survey/apple/iOS/majorSystemVersions/
- https://www.antutu.com/web/news/detail?id=136552
- https://www.phonearena.com/news/best-selling-smartphones-usa-china-india-germany-uk-france-korea-japan-q2-2026_id182887
- https://www.gsmarena.com/counterpoint_samsung_galaxy_a06_was_the_bestselling_phone_in_latam_for_2025-news-71620.php
- https://nanoreview.net/en/phone-compare/samsung-galaxy-a16-5g-vs-samsung-galaxy-a06
- https://www.cpu-monkey.com/en/compare_cpu-qualcomm_snapdragon_8_elite-vs-mediatek_helio_g85
- https://stora.sh/blog/2026-04-25-android-17-memory-limits-guide
- https://support.apple.com/guide/iphone/iphone-models-compatible-with-ios-27-iphe3fa5df43/ios
