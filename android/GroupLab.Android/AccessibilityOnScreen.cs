using System.Reflection;
using Android.Graphics;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using GroupLab.App.Diagnostics;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 2.1: TalkBack's boxes in the right place. The emulator's audit found every control's box in
/// Android's accessibility tree 128 pixels, the status bar's height, above where it is drawn: Avalonia's helper gives each control its place
/// within GroupLab's own view as its place on the screen, and never adds where the view sits. TalkBack then drew its outline above the
/// control it was reading, Android cut the controls at the top and bottom of the view down to slivers, and a tool that acts by place, such as
/// Voice Access or Switch Access, would act on the wrong control. This wraps Avalonia's helper: it answers every question exactly as
/// Avalonia's does, and moves each control's box by the view's place on the screen before Android clips it to the view.
/// </summary>
internal sealed class AccessibilityOnScreen : ExploreByTouchHelper
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly View host;
    private readonly ExploreByTouchHelper avalonia;
    private readonly MethodInfo at;
    private readonly MethodInfo visible;
    private readonly MethodInfo populate;
    private readonly MethodInfo perform;

    private AccessibilityOnScreen(View host, ExploreByTouchHelper avalonia, MethodInfo at, MethodInfo visible, MethodInfo populate, MethodInfo perform)
        : base(host)
    {
        this.host = host;
        this.avalonia = avalonia;
        this.at = at;
        this.visible = visible;
        this.populate = populate;
        this.perform = perform;
    }

    /// <summary>Wraps Avalonia's helper on every view under <paramref name="root"/> that has one and is not wrapped yet.</summary>
    internal static void Install(View? root)
    {
        if (root is null)
        {
            return;
        }

        try
        {
            if (ViewCompat.GetAccessibilityDelegate(root) is ExploreByTouchHelper helper and not AccessibilityOnScreen
                && helper.GetType().FullName?.StartsWith("Avalonia", StringComparison.Ordinal) == true)
            {
                var type = helper.GetType();
                var at = type.GetMethod("GetVirtualViewAt", Any, [typeof(float), typeof(float)]);
                var visible = type.GetMethod("GetVisibleVirtualViews", Any, [typeof(IList<Java.Lang.Integer>)]);
                var populate = type.GetMethod("OnPopulateNodeForVirtualView", Any, [typeof(int), typeof(AccessibilityNodeInfoCompat)]);
                var perform = type.GetMethod("OnPerformActionForVirtualView", Any, [typeof(int), typeof(int), typeof(Bundle)]);
                if (at is null || visible is null || populate is null || perform is null)
                {
                    DiagnosticLog.Info("accessibility.bounds", ("installed", false), ("why", "Avalonia's helper has changed"));
                    return;
                }

                ViewCompat.SetAccessibilityDelegate(root, new AccessibilityOnScreen(root, helper, at, visible, populate, perform));
                DiagnosticLog.Info("accessibility.bounds", ("installed", true));
            }
        }
        catch (Exception e) when (e is TargetInvocationException or AmbiguousMatchException or Java.Lang.Exception)
        {
            DiagnosticLog.Info("accessibility.bounds", ("installed", false), ("why", e.GetType().Name));
            return;
        }

        if (root is ViewGroup group)
        {
            for (int i = 0; i < group.ChildCount; i++)
            {
                Install(group.GetChildAt(i));
            }
        }
    }

    protected override int GetVirtualViewAt(float x, float y) => (int)at.Invoke(avalonia, [x, y])!;

    protected override void GetVisibleVirtualViews(IList<Java.Lang.Integer>? virtualViewIds) => visible.Invoke(avalonia, [virtualViewIds]);

    protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat? node)
    {
        populate.Invoke(avalonia, [virtualViewId, node]);
        if (node is null)
        {
            return;
        }

        int[] where = new int[2];
        host.GetLocationOnScreen(where);
        using var box = new Rect();
        node.GetBoundsInScreen(box);
        box.Offset(where[0], where[1]);
        node.SetBoundsInScreen(box);
    }

    protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments) =>
        (bool)perform.Invoke(avalonia, [virtualViewId, action, arguments])!;
}
