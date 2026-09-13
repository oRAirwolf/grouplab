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

## Package dependencies

| Package | Licence | Used by | Purpose |
|---|---|---|---|
| Net.Codecrete.QrCodeGenerator | MIT | GroupLab.Core | QR symbols for the definition and instance codes |
| PDFtoImage, with PDFium and SkiaSharp | MIT; PDFium BSD-3-Clause and Apache-2.0; SkiaSharp MIT | tests only | Rasterising rendered PDFs for conformance tests 41 and 42 |
| xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | Apache-2.0, MIT | tests only | Test framework |
