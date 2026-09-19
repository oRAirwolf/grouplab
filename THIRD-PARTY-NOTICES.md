# Third-party notices

GroupLab includes or depends on the following work by others.

## Included in the source

### AprilTag tag36h11 code table

`src/GroupLab.Core/Rendering/Markers/Tag36h11.cs` contains the 587 codes and the bit layout of the `tag36h11` family, generated from `tag36h11.c` of the AprilTag reference implementation, https://github.com/AprilRobotics/apriltag.

```
Copyright (C) 2013-2016, The Regents of The University of Michigan.
All rights reserved.

This software was developed in the APRIL Robotics Lab under the
direction of Edwin Olson, ebolson@umich.edu. This software may be
available under alternative licensing terms; contact the address above.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the Regents of The University of Michigan.
```


### IBM Plex

`src/GroupLab.App/Assets/Fonts/` contains these faces, embedded in `GroupLab.App` for the interface and for every numeric readout (NOTES-FROM-PLANNING.md entry 42 section 3):
- IBM Plex Sans, Regular, Medium, SemiBold and Bold, from https://github.com/IBM/plex;
- IBM Plex Sans Condensed, Bold, from https://github.com/google/fonts;
- IBM Plex Mono, Regular and Medium, also from https://github.com/google/fonts.

The fonts are unmodified.

```
Copyright © 2017 IBM Corp. with Reserved Font Name "Plex"
```

They are licensed under the SIL Open Font License, Version 1.1. The full text is `src/GroupLab.App/Assets/Fonts/LICENSE.txt`, and it ships beside the application as `IBM-Plex-LICENSE.txt`.

## Package dependencies

| Package | Licence | Used by | Purpose |
|---|---|---|---|
| Net.Codecrete.QrCodeGenerator | MIT | GroupLab.Core | QR symbols for the definition and instance codes |
| Microsoft.Data.Sqlite, with SQLitePCLRaw and SQLite | MIT; SQLitePCLRaw Apache-2.0; SQLite public domain | GroupLab.Core | The session and records database of DESIGN.md section 15 (NOTES-FROM-PLANNING.md entry 112 section 1) |
| OpenCvSharp4 and OpenCvSharp4.runtime.win, with OpenCV | Apache-2.0; OpenCV Apache-2.0 | GroupLab.Cli, and the tests through it | Marker detection, homography fitting and resampling behind `IImagingBackend` |
| PDFtoImage, with PDFium and SkiaSharp | MIT; PDFium BSD-3-Clause and Apache-2.0; SkiaSharp MIT | tests only | Rasterising rendered PDFs for conformance tests 39, 41, 42 and 43 |
| xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | Apache-2.0, MIT | tests only | Test framework |
