#!/usr/bin/env python3
"""Writes the iPhone's launch screen: ios/GroupLab.iOS/LaunchScreen.storyboard and its colors and images in Assets.xcassets.

NOTES-FROM-PLANNING.md entry 508 section 1: an empty outline of the home screen (the Capture page), with no words but the name, so the
change from the launch screen to the first frame is barely seen. Apple's launch screens run no code and refuse user-defined runtime
attributes (ibtool: "Launch screens may not contain user-defined runtime attributes"), so a rounded corner cannot come from a layer's
cornerRadius: each rounded shape is a stretchable image instead, drawn here at 3x in the light and dark palettes, whose caps hold the
corners and whose middle is plain fill.

The colors are GroupLab.App.Theme.Tokens' Light and Dark palettes, and the sizes CapturePage's and Shell's at the usual text size;
Entry508LaunchScreenTests holds both. Run it again after changing either:

    python scripts/ios-launch.py
"""
from __future__ import annotations

import itertools
import json
from pathlib import Path

from PIL import Image, ImageDraw

REPO = Path(__file__).resolve().parent.parent
IOS = REPO / "ios" / "GroupLab.iOS"
CATALOG = IOS / "Assets.xcassets"

# role: (light, dark), from Tokens.Light and Tokens.Dark.
PALETTE = {
    "Bg": (0xF4F3F0, 0x131417),
    "Panel": (0xFFFFFF, 0x1A1C20),
    "Panel2": (0xECEAE4, 0x212429),
    "Line": (0xD3D0C9, 0x2C3037),
    "Line2": (0xBDB9B0, 0x3A3F47),
    "Amber": (0x965D12, 0xE0912F),
}

# The plain colors the storyboard draws with: the ground, and the bar along the bottom with its rule.
COLORS = {"LaunchBackground": "Bg", "LaunchPanel": "Panel", "LaunchLine": "Line"}

SCALE = 3
# name: (width, height, radius, edge role or None, fill role, caps (left/right, top/bottom) in pixels, slicing mode), sizes in points.
# The caps cover the corner at 3x, and read as points instead they would still leave a middle, so either reading of the catalog's
# cap-insets draws the same shape. A pill is exactly as tall as its button (48), so it only stretches across.
SHAPES = {
    # Shell.Card: Panel, a one point Line edge, corner 10 (PhoneStyles.CardRadius).
    "LaunchCard": (256 / SCALE, 256 / SCALE, 10, "Line", "Panel", (32, 32), "9-part"),
    # Take a picture: Screens.Primary, Amber, corner 22 (PhoneStyles.PillRadius), 48 high (Screens.Touch).
    "LaunchTake": (512 / SCALE, 48, 22, None, "Amber", (70, 0), "3-part-horizontal"),
    # Choose a photo and Print a target: Screens.Choice, Panel2 with a one point Line2 edge.
    "LaunchChoice": (512 / SCALE, 48, 22, "Line2", "Panel2", (70, 0), "3-part-horizontal"),
}


def rgb(value: int) -> tuple[int, int, int]:
    return value >> 16, (value >> 8) & 255, value & 255


def write_json(path: Path, doc: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes((json.dumps(doc, indent=2) + "\n").encode())


def colorset(name: str, role: str) -> None:
    def entry(value: int, dark: bool) -> dict:
        r, g, b = rgb(value)
        color = {"color-space": "srgb", "components": {"alpha": "1.000", "blue": f"0x{b:02X}", "green": f"0x{g:02X}", "red": f"0x{r:02X}"}}
        return ({"appearances": [{"appearance": "luminosity", "value": "dark"}]} if dark else {}) | {"color": color, "idiom": "universal"}

    light, dark = PALETTE[role]
    write_json(CATALOG / f"{name}.colorset" / "Contents.json",
               {"colors": [entry(light, False), entry(dark, True)], "info": {"author": "xcode", "version": 1}})


def shape(path: Path, width: float, height: float, radius: float, edge: int | None, fill: int) -> None:
    """A rounded rectangle at 3x, drawn four times larger and reduced, so its corners are smooth."""
    big = 4 * SCALE
    w, h, r = round(width * big), round(height * big), radius * big
    picture = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(picture)
    if edge is None:
        draw.rounded_rectangle((0, 0, w - 1, h - 1), r, fill=rgb(fill) + (255,))
    else:
        inset = big  # one point
        draw.rounded_rectangle((0, 0, w - 1, h - 1), r, fill=rgb(edge) + (255,))
        draw.rounded_rectangle((inset, inset, w - 1 - inset, h - 1 - inset), r - inset, fill=rgb(fill) + (255,))
    picture.resize((round(width * SCALE), round(height * SCALE)), Image.LANCZOS).save(path, optimize=True)


def imageset(name: str, spec: tuple) -> None:
    width, height, radius, edge, fill, (across, down), mode = spec
    folder = CATALOG / f"{name}.imageset"
    folder.mkdir(parents=True, exist_ok=True)
    images = []
    for dark in (False, True):
        file = f"{name.removeprefix('Launch').lower()}-{'dark' if dark else 'light'}@3x.png"
        pick = 1 if dark else 0
        shape(folder / file, width, height, radius, PALETTE[edge][pick] if edge else None, PALETTE[fill][pick])
        insets = {"left": across, "right": across} | ({"top": down, "bottom": down} if mode == "9-part" else {})
        center = {"mode": "stretch", "width": 1} | ({"height": 1} if mode == "9-part" else {})
        image = {"filename": file, "idiom": "universal", "resizing": {"cap-insets": insets, "center": center, "mode": mode}, "scale": "3x"}
        if dark:
            image = {"appearances": [{"appearance": "luminosity", "value": "dark"}]} | image
        images.append(image)
    write_json(folder / "Contents.json", {"images": images, "info": {"author": "xcode", "version": 1}})


def lockup() -> None:
    """The name: the application's own lockup, light and dark, the files the home screen draws."""
    folder = CATALOG / "LaunchLockup.imageset"
    folder.mkdir(parents=True, exist_ok=True)
    assets = REPO / "src" / "GroupLab.App" / "Assets"
    for file in ("grouplab-lockup-light.svg", "grouplab-lockup.svg"):
        (folder / file).write_bytes((assets / file).read_bytes())
    write_json(folder / "Contents.json", {"images": [
        {"filename": "grouplab-lockup-light.svg", "idiom": "universal"},
        {"appearances": [{"appearance": "luminosity", "value": "dark"}], "filename": "grouplab-lockup.svg", "idiom": "universal"},
    ], "info": {"author": "xcode", "version": 1}, "properties": {"preserves-vector-representation": True}})


def storyboard() -> str:
    ids = itertools.count(1)
    view, safe = "glRoot-Vi-ewR", "glSafe-Ar-eaG"

    def c(first, fattr, second=None, sattr=None, const=0.0, mult=None) -> str:
        s = f'<constraint firstItem="{first}" firstAttribute="{fattr}"'
        if second:
            s += f' secondItem="{second}" secondAttribute="{sattr}"'
        if mult:
            s += f' multiplier="{mult}"'
        if const:
            s += f' constant="{const:g}"'
        return s + f' id="gl{next(ids):03d}-La-unc"/>'

    def image(vid: str, name: str, own: list[str] = ()) -> str:
        s = (f'<imageView clipsSubviews="YES" userInteractionEnabled="NO" contentMode="scaleToFill" image="{name}" '
             f'translatesAutoresizingMaskIntoConstraints="NO" id="{vid}"><rect key="frame" x="0.0" y="0.0" width="10" height="10"/>')
        return s + (f'<constraints>{"".join(own)}</constraints>' if own else "") + "</imageView>"

    def plain(vid: str, color: str, own: list[str] = ()) -> str:
        s = f'<view contentMode="scaleToFill" translatesAutoresizingMaskIntoConstraints="NO" id="{vid}"><rect key="frame" x="0.0" y="0.0" width="10" height="10"/>'
        s += f'<color key="backgroundColor" name="{color}"/>'
        return s + (f'<constraints>{"".join(own)}</constraints>' if own else "") + "</view>"

    subs, cons = [], []
    # The lockup, as CapturePage's BrandMark at its least height (LogoLeast, 36), its top margin 8 inside the page's 16.
    logo = "glLogo-Lo-ckU"
    subs.append(image(logo, "LaunchLockup", [c(logo, "height", const=36), c(logo, "width", logo, "height", mult="476:100")]).replace(
        'contentMode="scaleToFill"', 'contentMode="scaleAspectFit"'))
    cons += [c(logo, "top", safe, "top", 24), c(logo, "leading", safe, "leading", 16)]
    # The first three blocks of the home screen where they sit at the usual text size: the caliber card, Take a picture, the pair.
    card, take, left, right = "glCard-Ca-liB", "glTake-Pi-ctR", "glChoo-se-PhO", "glPrin-tT-arG"
    subs.append(image(card, "LaunchCard", [c(card, "height", const=88)]))
    cons += [c(card, "top", safe, "top", 147), c(card, "leading", safe, "leading", 16), c(safe, "trailing", card, "trailing", 16)]
    subs.append(image(take, "LaunchTake", [c(take, "height", const=48)]))
    cons += [c(take, "top", safe, "top", 250), c(take, "leading", safe, "leading", 16), c(safe, "trailing", take, "trailing", 16)]
    subs.append(image(left, "LaunchChoice", [c(left, "height", const=48)]))
    subs.append(image(right, "LaunchChoice"))
    cons += [c(left, "top", safe, "top", 314), c(left, "leading", safe, "leading", 16),
             c(right, "top", left, "top"), c(right, "leading", left, "trailing", 8), c(safe, "trailing", right, "trailing", 16),
             c(right, "height", left, "height"), c(right, "width", left, "width")]
    # The bar along the bottom: the panel color from 72 points above the home indicator's area to the screen's edge, a one point rule on top.
    bar, rule = "glNavB-ar-BoT", "glNavR-ul-eTp"
    subs.append(plain(bar, "LaunchPanel"))
    subs.append(plain(rule, "LaunchLine", [c(rule, "height", const=1)]))
    cons += [c(bar, "leading", view, "leading"), c(view, "trailing", bar, "trailing"), c(view, "bottom", bar, "bottom"),
             c(bar, "top", safe, "bottom", -72),
             c(rule, "leading", view, "leading"), c(view, "trailing", rule, "trailing"), c(rule, "top", bar, "top")]

    def unit(v: int) -> str:
        return f"{v / 255:.6f}".rstrip("0").rstrip(".") if v else "0.0"

    named = "".join(
        f'<namedColor name="{name}"><color red="{unit(rgb(PALETTE[role][1])[0])}" green="{unit(rgb(PALETTE[role][1])[1])}" '
        f'blue="{unit(rgb(PALETTE[role][1])[2])}" alpha="1" colorSpace="custom" customColorSpace="sRGB"/></namedColor>'
        for name, role in COLORS.items())
    sized = "".join(f'<image name="{name}" width="{spec[0]:g}" height="{spec[1]:g}"/>' for name, spec in SHAPES.items())
    return f'''<?xml version="1.0" encoding="UTF-8"?>
<!-- GroupLab's launch screen (entry 508 section 1), written by scripts/ios-launch.py: an empty outline of the home screen, nothing in it
     but the name. Edit the script, not this file. -->
<document type="com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB" version="3.0" toolsVersion="23504" targetRuntime="iOS.CocoaTouch" propertyAccessControl="none" useAutolayout="YES" launchScreen="YES" useTraitCollections="YES" useSafeAreas="YES" colorMatched="YES" initialViewController="glCtrl-Vi-ewC">
    <device id="retina6_12" orientation="portrait" appearance="dark"/>
    <dependencies>
        <plugIn identifier="com.apple.InterfaceBuilder.IBCocoaTouchPlugin" version="23506"/>
        <capability name="Named colors" minToolsVersion="9.0"/>
        <capability name="Safe area layout guides" minToolsVersion="9.0"/>
        <capability name="documents saved in the Xcode 8 format" minToolsVersion="8.0"/>
    </dependencies>
    <scenes>
        <scene sceneID="glScen-eL-aun">
            <objects>
                <viewController id="glCtrl-Vi-ewC" sceneMemberID="viewController">
                    <view key="view" contentMode="scaleToFill" id="{view}">
                        <rect key="frame" x="0.0" y="0.0" width="393" height="852"/>
                        <autoresizingMask key="autoresizingMask" widthSizable="YES" heightSizable="YES"/>
                        <subviews>
                            {"".join(subs)}
                        </subviews>
                        <viewLayoutGuide key="safeArea" id="{safe}"/>
                        <color key="backgroundColor" name="LaunchBackground"/>
                        <constraints>
                            {"".join(cons)}
                        </constraints>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier="IBFirstResponder" id="glResp-on-der" userLabel="First Responder" sceneMemberID="firstResponder"/>
            </objects>
            <point key="canvasLocation" x="0" y="0"/>
        </scene>
    </scenes>
    <resources>
        <image name="LaunchLockup" width="476" height="100"/>
        {sized}
        {named}
    </resources>
</document>
'''


def main() -> None:
    for name, role in COLORS.items():
        colorset(name, role)
    for name, spec in SHAPES.items():
        imageset(name, spec)
    lockup()
    (IOS / "LaunchScreen.storyboard").write_bytes(storyboard().encode())
    print(f"launch screen written: {len(COLORS)} colors, {len(SHAPES)} shapes, the lockup and the storyboard")


if __name__ == "__main__":
    main()
