# GroupLab: Cowork session start

## Paste this as your first message

```
I am building GroupLab, an open source target and group analysis application
for precision rifle shooters. DESIGN.md is the approved design document. The
files under scans/ are real scanned targets the detection pipeline must
eventually handle.

Read DESIGN.md first, then read SAMPLE-NOTES.md, which describes what each
sample file is and why it is interesting.

Work through the following in order, producing each as a separate file in the
workspace. Stop and ask me questions rather than guessing.

1. Patent search: machine-readable, fiducial-marked, or coded targets for
   automated shot or group analysis. Report what you find before I freeze the
   fiducial design. Do not declare anything clear that you cannot verify from
   a primary source.
2. USPTO search on the name "GroupLab" in software and related classes. Same
   standard of evidence.
3. Target definition schema: a versioned, documented file format implementing
   section 8 of DESIGN.md, including the compact binary encoding that gets
   embedded in the QR payload per section 9.
4. Fiducial marker decision: adopt ArUco or AprilTag versus designing a
   bespoke scheme. Weigh detection reliability at print resolution, library
   availability for .NET across Windows, Android and iOS, survivability of
   inkjet printing and weather, and page area consumed.
5. Detection pipeline specification implementing section 12, written against
   the actual files in scans/ rather than in the abstract.
6. Statistics reference implementing section 14, including the validation plan
   against the shotGroups R package.
7. Built-in target library layouts.

Do not write application code in this session. That happens later in Claude
Code, starting with the Phase 0 registration spike.
```

## Working rules for this project

- Markdown for files that live in the repository. Anything meant for reading
  is additionally produced as docx or PDF.
- No em dashes in any written output.
- No pseudoscience. Barrel harmonics, optimal barrel time, velocity nodes and
  accuracy nodes are not real and must never appear in documentation, UI copy,
  or code comments.
- No OnTarget compatibility of any kind. Their target designs and file formats
  are off limits. The files under reference/ontarget-output/ exist to show what
  the incumbent produces, not to be replicated.
- GroupLab is a working name and may change.
