"""Shared chart style for the GroupLab research articles.

Palette from the GroupLab charts reference: blue, orange, aqua in that fixed order, neutral text and grid.

NOTES-FROM-PLANNING.md entry 143 section 1.2. Every figure used to be drawn on a near-white surface, which
glared against the site's dark theme and looked pasted on. The entry asks that each script write both a light
and a dark version from the same data, and that this module grow the dark palette rather than each script
deciding for itself.

**So no script has to change.** ``save`` draws the figure once, writes the light version, then recolours the
same figure to the dark palette and writes it again beside it as ``<name>-dark.png``. Recolouring rather than
redrawing is what keeps the two identical in everything but colour: they are the same artists, the same data
and the same layout, so a figure cannot come out saying two different things in two themes.

The three data colours are unchanged between themes. They were chosen to sit on either.
"""
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

BLUE, ORANGE, AQUA = "#2a78d6", "#eb6834", "#1baf7a"
INK, INK2, MUTED, GRID, SURFACE = "#0b0b0b", "#52514e", "#8a8984", "#e6e5e0", "#fcfcfb"

# The site's own dark theme, from the stylesheet in website/build.py: --text, --dim, --line2 and a surface a
# shade lighter than --bg, so a figure reads as sitting on the page rather than punched through it.
INK_DARK, INK2_DARK, MUTED_DARK, GRID_DARK, SURFACE_DARK = "#e6e8ea", "#9aa1a9", "#6b737c", "#2c3037", "#191a1e"


def apply():
    plt.rcParams.update({
        "figure.facecolor": SURFACE, "axes.facecolor": SURFACE, "savefig.facecolor": SURFACE,
        "font.family": "DejaVu Sans", "font.size": 11,
        "axes.edgecolor": MUTED, "axes.labelcolor": INK2, "axes.titlecolor": INK,
        "axes.titlesize": 12.5, "axes.titleweight": "bold", "axes.titlelocation": "left",
        "xtick.color": INK2, "ytick.color": INK2,
        "axes.grid": True, "grid.color": GRID, "grid.linewidth": 0.8,
        "axes.spines.top": False, "axes.spines.right": False,
        "lines.linewidth": 2, "legend.frameon": False, "savefig.dpi": 150,
    })


def _is(colour, *of):
    """Whether an artist's colour is one of the light palette's, whatever form matplotlib is holding it in."""
    try:
        rgba = matplotlib.colors.to_rgba(colour)
    except (ValueError, TypeError):
        return False
    return any(rgba == matplotlib.colors.to_rgba(c) for c in of)


def _recolour_group(group):
    """The neutrals in a collection of marks, such as the points of a scatter, moved to the dark palette.

    A collection holds its colours as an array rather than one value, and a legend's sample mark is a separate copy
    of it, so both go through here. "Chasing the zero" came out with a white cross on the chart and a black one in
    the key for it, because only the first of those two was being recoloured.
    """
    if not hasattr(group, "get_facecolor"):
        return

    try:
        faces = [SURFACE_DARK if _is(c, SURFACE, "white", "w") else (INK_DARK if _is(c, INK, "black", "k") else c)
                 for c in group.get_facecolor()]
        edges = [INK_DARK if _is(c, INK, "black", "k") else c for c in group.get_edgecolor()]
    except (TypeError, ValueError):
        return

    if faces:
        group.set_facecolor(faces)
    if edges:
        group.set_edgecolor(edges)


def _darken(fig):
    """Swap the light palette for the dark one on a figure that is already drawn.

    Only the neutrals move. Anything drawn in a data colour, or in a colour a script chose for itself, is left
    exactly as it is: a script that picked a colour to mean something still means it.
    """
    fig.patch.set_facecolor(SURFACE_DARK)

    for text in fig.texts:
        if _is(text.get_color(), INK, "black", "k"):
            text.set_color(INK_DARK)
        elif _is(text.get_color(), INK2, MUTED):
            text.set_color(INK2_DARK)

    for ax in fig.get_axes():
        ax.set_facecolor(SURFACE_DARK)

        # An axes has three title artists, one per position, and ``ax.title`` is only the centre one. This style sets
        # ``axes.titlelocation`` to left, so every title in the research articles lives on the left artist and
        # recolouring ``ax.title`` recoloured an empty string. The first dark figures came out with a black title on a
        # black background and everything else correct, which is the kind of fault a build cannot see.
        for title in (ax.title, getattr(ax, "_left_title", None), getattr(ax, "_right_title", None)):
            if title is not None:
                title.set_color(INK_DARK)

        ax.xaxis.label.set_color(INK2_DARK)
        ax.yaxis.label.set_color(INK2_DARK)

        for spine in ax.spines.values():
            if _is(spine.get_edgecolor(), MUTED, INK, INK2, "black", "k"):
                spine.set_edgecolor(MUTED_DARK)

        ax.tick_params(colors=INK2_DARK, which="both")
        for label in ax.get_xticklabels() + ax.get_yticklabels():
            label.set_color(INK2_DARK)

        for line in ax.get_xgridlines() + ax.get_ygridlines():
            line.set_color(GRID_DARK)

        # Text a script drew on the axes: annotations, bar labels, the word beside a line. Only the ones left in
        # the neutral inks move, for the same reason as above.
        for text in ax.texts:
            if _is(text.get_color(), INK, "black", "k"):
                text.set_color(INK_DARK)
            elif _is(text.get_color(), INK2, MUTED):
                text.set_color(INK2_DARK)

        legend = ax.get_legend()
        if legend is not None:
            for text in legend.get_texts():
                text.set_color(INK_DARK)
            legend.get_frame().set_facecolor(SURFACE_DARK)
            legend.get_frame().set_edgecolor(MUTED_DARK)

            # A legend's sample marks are copies made when the legend was built, so recolouring the artists they were
            # copied from leaves them behind. "Chasing the zero" came out with a white cross on the chart and a black
            # one in the key for it, which is worse than either alone.
            for handle in legend.legend_handles:
                if handle is None:
                    continue
                if hasattr(handle, "get_color") and _is(handle.get_color(), INK, "black", "k"):
                    handle.set_color(INK_DARK)
                if hasattr(handle, "get_markeredgecolor") and _is(handle.get_markeredgecolor(), INK, "black", "k"):
                    handle.set_markeredgecolor(INK_DARK)
                if hasattr(handle, "get_markerfacecolor") and _is(handle.get_markerfacecolor(), INK, "black", "k"):
                    handle.set_markerfacecolor(INK_DARK)
                _recolour_group(handle)

        # A shape filled with the light surface, such as a card behind a label, would become a white hole.
        for patch in ax.patches:
            if _is(patch.get_facecolor(), SURFACE, "white", "w"):
                patch.set_facecolor(SURFACE_DARK)
            if _is(patch.get_edgecolor(), INK, "black", "k"):
                patch.set_edgecolor(INK_DARK)

        # Something a script drew in ink rather than in a data colour: the cross marking the aim point, a rule across
        # a chart, the outline of a scatter marker. These disappear entirely on a dark background, and a figure whose
        # subject has vanished still looks like a figure, which is why it needs saying rather than seeing.
        for line in ax.lines:
            if _is(line.get_color(), INK, "black", "k"):
                line.set_color(INK_DARK)
            elif _is(line.get_color(), INK2, MUTED):
                line.set_color(MUTED_DARK)
            if _is(line.get_markerfacecolor(), SURFACE, "white", "w"):
                line.set_markerfacecolor(SURFACE_DARK)
            if _is(line.get_markeredgecolor(), INK, "black", "k"):
                line.set_markeredgecolor(INK_DARK)

        for group in ax.collections:
            _recolour_group(group)


def save(fig, path):
    """Write the light figure at ``path`` and the dark one beside it as ``<name>-dark.png``.

    The site shows whichever matches the reader's theme, and falls back to the light one where no dark file
    exists, so an older figure that has not been regenerated still appears.
    """
    fig.tight_layout()
    fig.savefig(path, bbox_inches="tight")

    text = str(path)
    dot = text.rfind(".")
    dark = (text[:dot] + "-dark" + text[dot:]) if dot > text.replace(chr(92), "/").rfind("/") else text + "-dark"

    _darken(fig)

    # The axes title is the one artist that will not stay recoloured: matplotlib applies ``axes.titlecolor`` when the
    # figure is drawn, so setting it on the artist is undone by the save. The first dark figures came out with a black
    # title on a black background and everything else correct, which is exactly the sort of fault a build does not
    # notice. So the whole neutral half of the style is held to the dark palette for the length of the second save.
    with matplotlib.rc_context({
        "figure.facecolor": SURFACE_DARK, "axes.facecolor": SURFACE_DARK, "savefig.facecolor": SURFACE_DARK,
        "text.color": INK_DARK, "axes.titlecolor": INK_DARK, "axes.labelcolor": INK2_DARK,
        "axes.edgecolor": MUTED_DARK, "xtick.color": INK2_DARK, "ytick.color": INK2_DARK, "grid.color": GRID_DARK,
    }):
        fig.savefig(dark, bbox_inches="tight", facecolor=SURFACE_DARK)

    plt.close(fig)
