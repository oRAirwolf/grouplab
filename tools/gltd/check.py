from encode import frame, defid, HDR

STD = [ {"diameter":254,"inkIdx":0}, {"diameter":238,"inkIdx":15},
        {"diameter":127,"inkIdx":0}, {"diameter":115,"inkIdx":15},
        {"diameter":25, "inkIdx":0} ]
AIM = [ {"diameter":127,"inkIdx":0}, {"diameter":114,"inkIdx":15},
        {"diameter":25, "inkIdx":0} ]
INKS = [{"srgb":"#000000","role":"artwork"},{"srgb":"#FFFFFF","role":"paper"}]
FID  = {"scheme":"grid-boundary-1","family":"apriltag-36h11","markerSize":40,
        "quietZone":10,"inkIdx":0}
CODES= {"count":4,"ecLevel":"H","moduleSize":4}
DB9  = {"x":120,"y":2364,"width":1919,"height":310,"layout":"fields-3x3-1",
        "fieldSet":"standard-9","reserve":280,"inkIdx":0}
DB6  = dict(DB9, height=210, fieldSet="standard-6", y=2464)

T = {}
T["GL-CF25-LTR"] = {
 "page":{"size":"letter","width":2159,"height":2794},"inks":INKS,
 "ringSets":[{"discs":STD}],
 "grid":{"cols":5,"rows":5,"originX":320,"originY":539,"pitchX":380,"pitchY":380,
         "ringSetIdx":0,"order":0},
 "sighters":[{"count":3,"originX":700,"originY":2515,"pitchX":380,"ringSetIdx":0}],
 "fiducials":FID,"codes":CODES}
T["GL-CF25-LTR-D"] = {
 "page":{"size":"letter","width":2159,"height":2794},"inks":INKS,
 "ringSets":[{"discs":STD}],
 "grid":{"cols":5,"rows":5,"originX":320,"originY":537,"pitchX":380,"pitchY":380,
         "ringSetIdx":0,"order":0},
 "sighters":[],"fiducials":FID,"codes":CODES,"dataBlock":DB9}
T["GL-RF36-LTR"] = {
 "page":{"size":"letter","width":2159,"height":2794},"inks":INKS,
 "ringSets":[{"discs":[{"diameter":127,"inkIdx":0},{"diameter":115,"inkIdx":15},
                       {"diameter":64,"inkIdx":0},{"diameter":56,"inkIdx":15},
                       {"diameter":25,"inkIdx":0}]}],
 "grid":{"cols":6,"rows":6,"originX":330,"originY":540,"pitchX":254,"pitchY":254,
         "ringSetIdx":0,"order":0},
 "sighters":[{"count":4,"originX":700,"originY":2300,"pitchX":254,"ringSetIdx":0}],
 "fiducials":FID,"codes":CODES}
T["GL-LR300-T"] = {
 "page":{"size":"letter","width":2159,"height":2794},"inks":INKS,
 "ringSets":[{"discs":STD}],
 "grid":{"cols":2,"rows":3,"originX":572,"originY":381,"pitchX":1016,"pitchY":1016,
         "ringSetIdx":0,"order":0},
 "sighters":[],
 "fiducials":dict(FID, scheme="grid-boundary-half-1"),
 "codes":dict(CODES,count=2),
 "tiling":{"cols":2,"rows":2,"sheetWidth":2159,"sheetHeight":2794,"overlap":0}}
T["GL-LR300-T (3x2)"] = dict(T["GL-LR300-T"],
 tiling={"cols":3,"rows":2,"sheetWidth":2159,"sheetHeight":2794,"overlap":0})
T["GL-LR300-R24"] = {
 "page":{"size":"roll-24","width":6096,"height":7112},"inks":INKS,
 "ringSets":[{"discs":[{"diameter":381,"inkIdx":0},{"diameter":365,"inkIdx":15},
                       {"diameter":190,"inkIdx":0},{"diameter":178,"inkIdx":15},
                       {"diameter":38,"inkIdx":0}]}],
 "grid":{"cols":6,"rows":5,"originX":508,"originY":979,"pitchX":1016,"pitchY":1016,
         "ringSetIdx":0,"order":0},
 "sighters":[{"count":3,"originX":2032,"originY":6238,"pitchX":1016,"ringSetIdx":0}],
 "fiducials":FID,"codes":CODES,
 "dataBlock":dict(DB9,x=120,y=6682,width=5856)}
T["GL-LR300-R42"] = dict(T["GL-LR300-R24"],
 page={"size":"roll-42","width":10668,"height":6096},
 grid={"cols":10,"rows":4,"originX":762,"originY":979,"pitchX":1016,"pitchY":1016,
       "ringSetIdx":0,"order":0},
 dataBlock=dict(DB9,x=120,y=5666,width=10428))
T["GL-ZERO-MOA-100Y"] = {
 "page":{"size":"letter","width":2159,"height":2794},"inks":INKS,
 "ringSets":[{"discs":AIM}],
 "grid":{"cols":1,"rows":1,"originX":1079,"originY":1292,"pitchX":0,"pitchY":0,
         "ringSetIdx":0,"order":0},
 "sighters":[],
 "fiducials":dict(FID, scheme="field-ring-1"),
 "codes":CODES,"dataBlock":DB6,
 "grids":[{"centreX":1079,"centreY":1292,"half":798,"divisions":6,"majorEvery":2,
           "unit":"moa","distance":100,"distanceUnit":"yd","inkPair":0x00,
           "style":1,"labelStep":2}]}

print(f"{'target':18s} {'body':>5s} {'frame':>6s} {'v8-H':>5s} {'v10-H':>6s}   id")
for k,v in T.items():
    fr, bd = frame(v)
    print(f"{k:18s} {len(bd):5d} {len(fr):6d} {'ok' if len(fr)<=84 else 'NO':>5s} "
          f"{'ok' if len(fr)<=119 else 'NO':>6s}   {defid(bd)}")
