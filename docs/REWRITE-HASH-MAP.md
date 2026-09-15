# The 2026-09-14 history rewrite: old and new commit ids

On 2026-09-14, `git filter-branch --index-filter` rewrote every local branch of this repository to replace sixteen Phase 0 phone photographs under `scans/phase0/` with copies scrubbed of their location metadata. Thirteen of them carried the GPS coordinates of where they were taken, and the repository was about to be published (`docs/NOTES-FROM-PLANNING.md` entries 29 and 30, `docs/QUESTIONS-FOR-PLANNING.md` question 13).

The scrub changed metadata only, never a pixel, so no measurement moved. It did change the id of every commit from the one that added the photographs onwards. A document, commit message or old clone that cites one of the ids on the left refers to the commit on the right.

- **Branches rewritten:** `main`, `phase-0`, `phase-0a`, `phase-1` and one leftover worktree branch.
- **Commits:** 59 before the rewrite. 42 changed id, listed below, and 17 did not.
- **Not listed:** commits made after the rewrite, which never had another id.
- **The pre-rewrite history** is kept outside the repository in Alan's bundle, `grouplab-prerewrite-2026-09-14.bundle`, until the rewritten history is verified in a fresh repository.

| Old id | New id | Subject |
|---|---|---|
| `e24705748f2884fa0dd1a43cc0280088c7954a39` | `8423ef6e92d9dc555141c4b9f148f647d46d1586` | Add the Phase 0 sample set; fix marker id ordering in layout.py |
| `a9a5f3240f833e91a2fca6c82de1d6c2593c1853` | `eb03e2a977465def4311e4948ce63a2394b5c1a2` | Preliminary Phase 0 measurement from the first sample set |
| `d1257a3d2bfaf727c710f9b64cbc5497df1a9c0b` | `8aa9a3116e4d063f28f4c7b048a8958e3be4c501` | The Phase 0 displacement field is fixed to the paper, not the scanner |
| `c36aa4dd372384e6cb755276f8d35333b21e67ca` | `8bba727911fbc8c5500f8b66dc15f726a471a4e6` | Accept the two-gate structure; write the Phase 0 spike brief |
| `e276f9d0f7fea56de4349db4cfd5953dd96b96f3` | `70c6ea428bb1ca41d96935da58f7ba6abffe397c` | Scope the caliper check to gross error, not hundredths |
| `2237e2bd60b21d25d94ee0fda9a7a3dcd366794d` | `8b90c84c04c33cd3239a3a8ef350035d34a60272` | Ignore Claude Code's working output folder |
| `a007cf631fce8667a7886be4a19efe1ceaf99d25` | `c0746d71dae25ed83a6d49838cf8c27086b99beb` | Record the reference/ folder as a blocker for going public |
| `1dac1836f96baaf3dd738297319fc9f18e2f3e0c` | `845e0e8c2ce909e553cf001e1e2a4a1309733fb1` | Rename the trunk to main; state the branch naming convention |
| `dbf658a0afe7f946fbf02c9b24daddb74221088b` | `857456a84967eda4c449a4acbb88edd5489b54a7` | Phase 0 M1: measure verb, stage trace, bull locators, paper gate |
| `afa1c99cab1fbe84d8dd458fd819f3d8f62f3e33` | `c1106ecb43681bbeda1cc26ed804de8683569e74` | Phase 0 M2: every measurement, wall photographs by lens, raw rows |
| `23f69e901582661bd404a816cc47c8be0e958064` | `cb43859673a01b32261c3be122783998c02b725f` | Phase 0: bracketing rule as a warning, curved-sheet requirement |
| `5f5df2c9deb40f7a3015c5037a973ee865d5ef71` | `c65a04d302fa9aa22b75020578c393b22b40b89b` | Phase 0: flat photograph gate on main_flat1-3, and the verdict |
| `8e8cc37702f32bff21d6428b26b1f2a5a9aa5237` | `cfef3be7badb597dc5f5f74d990fedbcf0873917` | Phase 0: freeze the printed definitions; question 6 on the geometry commit |
| `61f6820ba50af551700c5b70d068dc6d25b33a4a` | `82c3fa15942cd13530f670a02faa1ea016837355` | Phase 1 M0: the marker module sweep |
| `5b9ca7bb11ba895e4b8fee4b86cd5ba44ec88aad` | `75a7c39a9b2e61ba96cc23831ae2cf38fd5f8436` | Keep generated sweep files and PDFs byte-exact on checkout |
| `d78c17c5138b3cba6b9a6a88cec693f01655d9f6` | `88dc0f9f02ce6c2eac5fb719bb8d95f1f83b6ce0` | Phase 1 M1: developable surface fit, built and swept on synthetic truth |
| `ab42e33aa3e21def3686def0060202c9c0111eee` | `430081b20cf687fe036733f77868252e8d30aecd` | Phase 1 M1: the real-frame run crashed; recorded, not retried |
| `d574a7efc663d1532c5220a615ad00ac63cfba84` | `e30fbebe3d0c9828fdcf3228354db2842f1d799c` | Phase 1 M1: the locator guard, one focal start per lens, the real-frame run and the M1 report |
| `8fe8ef2ee2694ec36f15998b7ea3a668f42be493` | `bca15e79b67a7f47867a760e8c7704293adc1665` | Phase 1: the geometry commit, entry 13 option A, and test 26f an error |
| `a766ff864896448902cf1ef887fce655d6f288d2` | `05a4f070ac7a792328af04777d84ca38856105fd` | Phase 1 M1: entry 15 section 4, the lens held and the shape left |
| `d73b9a49309f7a735dd0bb1b5c25a6f13f1d1162` | `3dcc814362f71f4f24141e252a63db770f8e545e` | Phase 1: the geometry commit, entry 13 option A, and test 26f an error |
| `0bc213a1fe0da9b4c94b8abf3aa322b2f9fb05ea` | `dfc770b89bc050906aa630ce8e8b31fac86e490d` | Questions 7 and 8: the module sweep sheets under test 26f, and the lens key |
| `fa36186757a3321efcb421887c3087e851d0b98a` | `adbc0cc42c614e7eb2e3fb102ba4a2d8206f1db7` | Phase 1 M1.8: the mounted frames' corners on the noise sweep |
| `4332c8dde57bf17768af43f5798c652df3b99f3a` | `9f4dcdb8dd758bb2d3f7a0b7bd1ef1698d7b1619` | Phase 1 M1.9: joint fits grouped by pixel geometry; entry 16 sections 2 to 4 |
| `31be350f977aa88424652a8a4bce5631f1405bc5` | `f9b66733df660e24a665161155f92d8fb56c24c6` | Phase 1 M1.10: the general developable surface, and the surface models stop |
| `8c7026a392c6f7f8721384cdf4cff0c0be418c59` | `7748ab6931352c5e11ce708f5a98fc3a1e8bb590` | Phase 1 M1.11: frames fitted alone, the plane below eight corners, and the correlation diagnostic |
| `65132caf6f72827d07dcc2295463b84dce0443bb` | `0f5443dad9f35ed8c68fe848497ba8258ecb4e67` | Phase 1 M2.1: the neutral-darkness hole detector, ported and revalidated |
| `e7fcbf22381cf7c6fa7753885c85d33b39909435` | `64a24681163a310ef1dcc641a3674f78517eebf2` | Phase 1 M2.2: render-and-difference on synthetic sheets, and the gate reading |
| `82481b4d085940896b84ef2860b6544941c8ee4e` | `3204f7e0ac150a874ca5f44fad8bb6a4444623db` | Questions 9 and 10: gate 2's centre tolerance, and the missing shotGroups fixtures |
| `20f562efc914f9e5965c81f586798e1a92d28102` | `dbea97ba9a4213de3ffe20234bf2246817169c9d` | Entry 18: shotGroups fixtures, gate 2 amended to 0.15 in, questions 7 to 10 answered |
| `e361d2d442d4b3458a8f28313a42d5cec086633a` | `c1beb74b935ce45a86471edb63b2c1044552e3ac` | Phase 1 M3.1: the statistics engine against shotGroups, and question 11 |
| `e5d5f469058ab5e4b603cd6f56c834c2b8cf0610` | `fb25e918650daca6b0ce209d3e7c2137910f6f0b` | Phase 1 M3: the statistics engine reported against section 15.5 |
| `76eecc5ba6afc8ba5a2567bde3c62dd644334c24` | `cb9ba874fb56aa8accc8ec4a5eb8f133b7a69cc0` | Entries 19 and 20: the N568 photograph measured against its own scan |
| `95df9cb770dbae621d0c195d1a93e72290e5d8fe` | `f710f69c0621cc3cc25abb2ce7136ca03c380845` | Phase 1 M4: the marking screen, manual path and correction interface in one |
| `b9b117b6d65d229b8716f07e8f3af000ad575b60` | `5f8dafd1e40ee0caaefc01122ad44a36e903fd63` | Entry 24 sections 1 to 3: no group size below five shots, intervals labelled with their coverage, no NaN in the export |
| `1627dac079cd87680ec72848790103c0e9e2e43b` | `d5d75f2d9e2515c22e7398ed40854a0e69a18ebc` | Entries 24 and 26: rotation as a view transform, marking file version 2, calibre |
| `f4f526313ff3a4f71ef0a8a1fbd9b8afbd279997` | `2665c05b343492c42d7f52e1a2cc01bd39acc827` | Entry 25 section 1: an application-wide unit setting that changes display and never storage |
| `be8da736c46175700aa43042bc59844eadd7ffca` | `8c38cd280af4553e1f7af56257ec6313e42afddc` | Entry 25 section 2: the application prints a target |
| `b412f02d55515118f36d0153fd61f4811b099035` | `f3fbfef27769be88b0038d7fde3975ad137b54c4` | Entry 23: the point of aim in the harness, question 11 answered, detection inside the sheet |
| `2e51dc7e7639d4269c8638fcb2bc9e07defce1d9` | `f2c8585ffa2202baaf575bd1d9a9dd8c9801b40c` | Entries 22 and 27: the intake gate, the publication test, digital zoom in the lens key, and question 13 |
| `b10caf5b1f08b27383de6844ef3191b5d632ad43` | `15667711005d696b3895acc9ae7a33a7e4495ed5` | Entry 28: the real upload schema, questions 12 to 14 answered, every probe key compared |
| `aed8ae5e52e868a35089dbe685a717c0478de0fd` | `3b96b90642dfb40eaaf35504e94c8354cde834ad` | Entry 29 preparation: one scrubber definition, grouplab scrub, and every committed photograph proven pixel-identical |
