# GroupLab: USPTO trademark search

**Prepared** 12 September 2026
**Prepared for** Open item 2 of DESIGN.md section 23, and the risk recorded in section 22
**Status** Search report. Not a clearance opinion, and not legal advice.

---

## 0. What this document is, and what it is not

DESIGN.md section 22 states that no AI assurance of availability should be treated as a clearance. That instruction is correct and this document does not attempt to overturn it.

What follows is a record of searches actually run against the USPTO Trademark Search database on 12 September 2026, with the exact queries and the exact result counts. A trademark clearance opinion requires a search of common-law use, state registrations, business-name registers, domain and social-media use, and foreign registers, and it requires a lawyer to weigh likelihood of confusion under the DuPont factors. None of that is here.

**Methodology note that matters for trusting the numbers.** The USPTO Trademark Search web interface does not submit on the Enter key, only on the search button. An earlier attempt using Enter produced "No results found" for a control query on NIKE, which is obviously wrong. All results below were obtained either by clicking the search button, or by posting queries directly to the same backend endpoint the interface itself calls (`POST https://tmsearch.uspto.gov/prod-stage-v1-0-0/tmsearch`). Both methods were validated against a control query that returned 224 hits for `nike*`, confirming the searches were genuinely executing.

---

## 1. Result

**No United States trademark application or registration for GROUPLAB exists, live or dead.**

Not one record. Not pending, not registered, not abandoned, not cancelled, in any class.

The nearest records are three dead marks reading GROUP LABS, none of which is in a class or field that would be cited against software for firearms accuracy analysis.

**The material risk to the name is not the USPTO register. It is common-law use**, and there is a real instance of it, described in section 4.

---

## 2. Searches run, with counts

All searches against the USPTO Trademark Search database (successor to TESS), both live and dead records included, all classes, on 12 September 2026.

### 2.1 Exact and wildcard, wordmark field

| Query | Field | Live | Dead | Total |
|---|---|---|---|---|
| `grouplab` | Wordmark | 0 | 0 | **0** |
| `grouplab*` | Wordmark | 0 | 0 | **0** |
| `*grouplab*` | Wordmark | 0 | 0 | **0** |
| `grouplab*` | Pseudo-mark | 0 | 0 | **0** |
| `"group lab"` (phrase) | Wordmark | 0 | 0 | **0** |
| `"the group lab"` | Wordmark | 0 | 0 | **0** |

The pseudo-mark search matters. USPTO indexes a "pseudo mark" for compound and misspelled marks, so that GROUPLAB would also be indexed as GROUP LAB. Searching that field is what catches a compound mark that a plain wordmark search would miss. It returned nothing.

### 2.2 Phonetic and orthographic variants, wordmark field

| Query | Total | Notes |
|---|---|---|
| `grouplabs*` | **0** | |
| `gruplab*` | **0** | |
| `grouplabb*` | **0** | |
| `groupelab*` | **0** | |
| `groop*lab*` | **0** | |
| `grouplabo*` | **0** | |

### 2.3 Component search, both words in any mark

| Query | Total | Result |
|---|---|---|
| `GROUP AND LAB` | **7** | Listed below |
| `"group labs"` | **3** | Listed below |

**All seven `GROUP AND LAB` hits:**

| Wordmark | Status | Class | Serial | Owner |
|---|---|---|---|---|
| LAB GROUP | Dead, cancelled | IC 011 | 75755653 | Constructions Industrielles de la Mediterranee (France) |
| NOBLE LAB GROUP | Dead, abandoned | IC 005 | 88916725 | Loki Properties LLC (Montana) |
| A APOLLO LAB GROUP | Dead, abandoned | IC 044 | 90298576 | Apollo Partners LLC and MedComp GX LLC |
| A APOLLO LAB GROUP | Dead, abandoned | IC 044 | 90290327 | Apollo Partners LLC and MedComp GX LLC |
| THE LAB WORLD GROUP | **Live, registered** | IC 035 | 86103217 | The Lab World Group LLC (Massachusetts) |
| FEDERAL MASHUP LAB POWERED BY BROWN VENTURE GROUP | **Live, registered** | IC 041 | 99476440 | Brown Venture Group LLC (Minnesota) |
| VENTURE LAB NITERRA GROUP | Dead, abandoned | IC 035, 041, 043 | 98110080 | Nippon Tokushu Togyo KK (Japan) |

**All three `"group labs"` hits, all dead:**

| Wordmark | Status | Class | Serial | Owner |
|---|---|---|---|---|
| GROUP LABS | Dead | IC 042 | 73129819 (reg. 1124181) | Group Labs, Inc. (Florida), filed 1977 |
| SINGULARITY GROUP LABS | Dead, abandoned | IC 009, 041, 042 | 90833627 | Singularity Education Group (California) |
| B GROUP LABS | Dead, abandoned | IC 005 | 87184193 | B Group Labs, LLC (Texas) |

### 2.4 Reading of these results

The two live marks are in IC 035 (business services: industrial asset liquidation, and advertising or promotional services) and IC 041 (educational services in technology commercialisation). Neither is software, neither is firearms, neither is measurement or analysis, and both are multi-word marks in which LAB and GROUP appear in reverse order or separated by other dominant terms.

Of the dead marks, the closest on its face is **GROUP LABS in IC 042**, serial 73129819, filed 1977 by Group Labs, Inc. of Florida. IC 042 in the current Nice classification covers scientific and technological services including software design and development, so the class is right. The mark is dead, and a registration filed in 1977 is long expired. It has no bearing.

**No mark identical or confusingly similar to GROUPLAB exists in the classes that matter:**

- **IC 009**, downloadable computer software. This is the class GroupLab would file in if it were ever filed.
- **IC 042**, software as a service, software design and development.
- **IC 041**, education and training services.
- **IC 013**, firearms and ammunition.
- **IC 028**, sporting goods, which is where printed paper targets sit.

---

## 3. The relevant classes, for reference

If registration is ever pursued, these are the classes an application would name. The list is included because DESIGN.md defers the final name and this is the shape of the decision when it arrives.

| Class | Covers | Applicable to GroupLab |
|---|---|---|
| **IC 009** | Downloadable computer software | Yes. This is the primary class for a downloadable Windows, Android and iOS application |
| **IC 042** | SaaS, software design and development, hosting | Only if the community target-definition library or the share-link service becomes a hosted offering |
| **IC 041** | Education, training, entertainment | Unlikely |
| **IC 028** | Sporting goods, including shooting targets | Possible if printed targets were ever sold, which DESIGN.md section 3 excludes |
| **IC 013** | Firearms, ammunition | No |

Note the practical wrinkle. Under 15 USC 1051, a use-based application requires use in commerce, and an intent-to-use application requires a bona fide intention to use the mark in commerce. A freely distributed open-source application with no paid tier, no licence key and no commercial offering, which is exactly what DESIGN.md section 3 specifies, sits awkwardly against that requirement. Free distribution can still constitute use in commerce in some circumstances, but it is not automatic and it is a question for a trademark attorney. **The realistic position is that GroupLab probably does not need and probably cannot easily obtain a registration.** What matters is the defensive question, which is whether using the name exposes the project to a claim by someone else. On the register, it does not.

---

## 4. The actual risk: common-law use

The USPTO register is clean. Common-law use is not, and in the United States common-law rights arise from use rather than registration.

### 4.1 GroupLab, University of Calgary

There is a long-established human-computer interaction research group at the University of Calgary named **GroupLab**, led by Saul Greenberg, at `grouplab.cpsc.ucalgary.ca`. It has operated under that name for decades and has released software under it, including groupware toolkits and the Phidgets work.

**Assessment, and it is an assessment rather than an opinion:**

- The fields are unrelated. Academic groupware and CSCW research versus firearms accuracy measurement is about as far apart as two pieces of software can be, and likelihood of confusion turns substantially on relatedness of goods and channels of trade.
- It is a Canadian university research group, not a US commercial entity, which limits the geographic and commercial scope of any common-law rights in the United States.
- They hold no US registration, as section 2 establishes.
- They have been using the name since well before GroupLab would launch, so priority runs their way in whatever scope their rights have.

**The practical consequences are search visibility and domain availability rather than legal exposure.** A user searching for "GroupLab" will find a Calgary HCI lab with strong search-engine authority. `grouplab.cpsc.ucalgary.ca` is theirs. Whether `grouplab.com`, `grouplab.org` or `grouplab.app` are available was not determined and should be checked before the name is committed.

### 4.2 Other common-law uses not investigated

The following were not searched and should be before the name is finalised:

- **State trademark registers.** Fifty separate databases. A Colorado search at minimum would be prudent.
- **Business-name registers.** Secretary of State filings in each state.
- **Domain registrations** across the common TLDs.
- **GitHub organisation and repository namespace.** This matters practically for a public repository. A general web search did not surface a prominent GroupLab project on GitHub, but the namespace was not checked directly and should be, since it is a thirty-second check with a real consequence.
- **Package-registry namespaces**, NuGet in particular given the .NET stack, if any library is ever published.
- **App store listing names** on Microsoft Store, Google Play and the App Store. Store listing names are separately governed by each store's policies and can be refused for reasons unrelated to trademark law.
- **Foreign registers.** EUIPO and CIPO were not searched. Relevant if the application is distributed internationally, which an A4-supporting open-source tool certainly will be.

---

## 5. Recommendation

**On the register, the name is clear.** There is no US trademark obstacle to using GROUPLAB for software. That finding is as solid as a search can make it: exact, wildcard, pseudo-mark and component searches all returned zero, on a search interface validated against a control query.

**Three things are worth doing before the name is frozen**, and DESIGN.md already defers the decision to Phase 6 for the Android package ID and the iOS bundle ID, which is the right sequencing.

1. **Check the GitHub organisation namespace and the obvious domains.** Minutes of work, and it is the most likely practical collision.
2. **Decide whether the Calgary collision matters to you.** It is not a legal problem. It is a discoverability problem, and it is the kind of thing that is annoying forever once the repository, the package IDs and the store listings are committed. A distinctive name with no established prior user in software would avoid it entirely.
3. **If registration is ever wanted, take advice on the use-in-commerce question first.** For a free open-source tool it may be neither necessary nor obtainable, and the money is better spent on the patent question in the patent search document, which is the one that actually carries risk.

**What this document does not do is clear the name.** No search performed by an AI assistant, including this one, is a clearance. If the name is to be carried onto permanent identifiers, a trademark attorney should run a full availability search. The cost of that is small next to the cost of changing an Android package ID after publication, which cannot be done.

---

## Sources

- USPTO Trademark Search, https://tmsearch.uspto.gov/search/search-results, searched 12 September 2026. Queries run through the interface search button and through the interface's own backend endpoint `POST https://tmsearch.uspto.gov/prod-stage-v1-0-0/tmsearch`, validated with a control query returning 224 records for `nike*`.
- [GroupLab, University of Calgary](https://grouplab.cpsc.ucalgary.ca/)
- [Saul Greenberg, GroupLab publications](https://grouplab.cpsc.ucalgary.ca/Publications/SaulGreenberg)
