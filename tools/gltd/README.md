# GLTD-B reference encoder

Sizing check for the binary payload specified in TARGET-SCHEMA.md section 5.
Not production code and not a decoder: it exists to answer "does the payload
fit in the QR symbol the layout reserves room for" with a measured number
rather than an estimate.

## Files

| File | What it does |
|---|---|
| `encode.py` | Wire version 1 encoder: `body()`, `frame()`, `defid()` |
| `check.py` | Encodes six representative library sheets and reports body size, frame size, symbol fit and definition identifier |

## Running

```
python3 check.py
```

```
target              body  frame  v8-H  v10-H   id
GL-CF25-LTR           55     70    ok     ok   GL-YCSK-DZZ1-R0VJ-4T5Y
GL-CF25-LTR-D         60     75    ok     ok   GL-KVJD-1XE6-7N39-H19S
GL-RF36-LTR           55     70    ok     ok   GL-5HMN-72K2-ZMT8-YGX4
GL-LR300-T            56     71    ok     ok   GL-BFXE-6QHA-DBQS-5R8R
GL-LR300-T (3x2)      56     71    ok     ok   GL-P0HX-V0PH-FGGP-2597
GL-LR300-R24          70     85    NO     ok   GL-BYNS-FWB4-P57Z-8T4C
GL-ZERO-MOA-100Y      70     85    NO     ok   GL-D2WG-NMY1-47JP-J4BW
```

Three results worth keeping in mind:

- Payload size does not scale with bull count. GL-CF25-LTR at 25 bulls and
  GL-RF36-LTR at 36 both encode to 55 bytes, because parametric mode stores a
  grid rather than a bull list. It scales with the number of *kinds* of block.
- The worst case is 85 bytes, which is why the library reserves a v10-H symbol
  (119 bytes) rather than v8-H (84).
- The two assembly presets of the 300 yard tile encode to the same 56 bytes and
  hash differently, because `cols` and `rows` in the tiling block are part of the
  definition. That is what lets a decoder know how many sheets to expect.

Capacities were measured against the `segno` library, not read from a table.
