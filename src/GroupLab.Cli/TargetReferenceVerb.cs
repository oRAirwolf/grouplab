using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;
using GroupLab.Core.Updates;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab target-reference</c>, NOTES-FROM-PLANNING.md entry 344: steps 1 to 3 from a photograph file, so Alan can try them on the desktop
/// before the screens exist. <c>make</c> straightens a photo of a blank target by one scale source, fingerprints it, checks it against the
/// library and writes the small reference file, never the photograph; <c>check</c> is what Code does with a submitted one (it recognizes
/// its own photograph, it is no duplicate, its family is right); <c>add</c> puts it in the built-in library in the repository; <c>library</c>
/// writes the signed library file the application can fetch.
/// </summary>
public static class TargetReferenceVerb
{
    public const string Usage =
        "grouplab target-reference make <photo> --id <id> --maker <maker> --name <name with {size}> --size <size> [--catalogue <n>] " +
        "(--printed <W>x<H> | --sheet [--print-scale <s>] | --points <x1>,<y1>,<x2>,<y2> --distance <in>) [--corners <x,y,x,y,x,y,x,y>] " +
        "[--bulls <x,y;x,y> | --bulls auto] [--out <file.glref>] [--straightened <png>]\n" +
        "grouplab target-reference check <file.glref> <photo>\n" +
        "grouplab target-reference add <file.glref> [--to <fingerprints folder>]\n" +
        "grouplab target-reference library <out.json> [--key <file holding the private key, base64 PKCS#8>, else $" + UpdateKeys.SecretName + "] [--version <n>, else the built-in list's]";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The built-in library's folder in the repository.</summary>
    public const string Fingerprints = "src/GroupLab.Core/StoreTargets/Fingerprints";

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        string[] a = [.. args];
        try
        {
            return a switch
            {
                ["make", var photo, .. var rest] => Make(photo, rest, output, error),
                ["check", var file, var photo] => Check(file, photo, output),
                ["add", var file, .. var rest] => Add(file, Option(rest, "--to") ?? Fingerprints, output, error),
                ["library", var file, .. var rest] => Library(file, rest, output, error),
                _ => Fail(error),
            };
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException or FormatException or OpenCVException)
        {
            error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int Fail(TextWriter error)
    {
        error.WriteLine(Usage);
        return 2;
    }

    private static string? Option(IReadOnlyList<string> args, string name)
    {
        for (int i = 0; i + 1 < args.Count; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static double[] Numbers(string text) =>
        [.. text.Split([',', ';', 'x'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(v => double.Parse(v, Inv))];

    /// <summary>Steps 1 to 3: the photograph straightened, fingerprinted, checked against the library and written as a reference.</summary>
    private static int Make(string photo, IReadOnlyList<string> rest, TextWriter output, TextWriter error)
    {
        string? id = Option(rest, "--id"), maker = Option(rest, "--maker"), name = Option(rest, "--name"), size = Option(rest, "--size");
        if (id is null || maker is null || name is null || size is null)
        {
            return Fail(error);
        }

        var (grey, metadata) = ImageLoader.Load(photo);
        var (value, _) = ImageLoader.LoadMaxChannel(photo);
        using var colour = Cv2.ImDecode(File.ReadAllBytes(photo), ImreadModes.Color | ImreadModes.IgnoreOrientation);
        IReadOnlyList<PointD> corners;
        if (Option(rest, "--corners") is { } given)
        {
            var n = Numbers(given);
            corners = [new(n[0], n[1]), new(n[2], n[3]), new(n[4], n[5]), new(n[6], n[7])];
        }
        else if (SheetOutline.Find(grey, out string? why) is { } quad)
        {
            corners = quad.Corners;
            output.WriteLine(string.Create(Inv, $"Corners found: {string.Join("  ", corners.Select(c => $"({c.X:0}, {c.Y:0})"))}, edges straight to {quad.EdgeRmsPixels:0.0} px."));
        }
        else
        {
            error.WriteLine($"GroupLab could not find the target's four corners ({why}); give them with --corners.");
            return 1;
        }

        StraightenedTarget target;
        if (Option(rest, "--printed") is { } printed)
        {
            var n = Numbers(printed);
            target = TargetStraightening.FromPrintedSize(corners, n[0], n[1], Option(rest, "--corners") is null ? 1.5 : 3);
        }
        else if (rest.Contains("--sheet"))
        {
            double printScale = Option(rest, "--print-scale") is { } s ? double.Parse(s, Inv) : 1;
            if (TargetReferenceMaker.ReadGroupLabSheet(grey, value, metadata, SheetIdentification.Candidates([Option(rest, "--library") ?? AnalyzeVerb.DefaultLibrary]), printScale) is not { } sheet)
            {
                error.WriteLine("No GroupLab sheet could be read in the photograph.");
                return 1;
            }

            target = TargetStraightening.FromGroupLabSheet(sheet.Plane, corners, sheet.ResidualInches, sheet.SheetInches, Option(rest, "--print-scale") is not null);
        }
        else if (Option(rest, "--points") is { } points && Option(rest, "--distance") is { } distance)
        {
            var n = Numbers(points);
            target = TargetStraightening.FromTwoPoints(corners, new(n[0], n[1]), new(n[2], n[3]), double.Parse(distance, Inv), grey.Width, grey.Height, metadata);
        }
        else
        {
            return Fail(error);
        }

        output.WriteLine(string.Create(Inv, $"{target.Says} The target is {target.WidthInches:0.00} by {target.HeightInches:0.00} in."));
        var (straight, dpi) = TargetReferenceMaker.Straighten(colour, target);
        using (straight)
        {
            if (Option(rest, "--straightened") is { } look)
            {
                Cv2.ImWrite(look, straight);
            }

            IReadOnlyList<PointD>? bulls = null;
            if (Option(rest, "--bulls") is { } b && b != "auto")
            {
                var n = Numbers(b);
                bulls = [.. Enumerable.Range(0, n.Length / 2).Select(i => new PointD(n[2 * i], n[(2 * i) + 1]))];
            }
            else
            {
                output.WriteLine("Bulls found: " + string.Join("  ", StoreFingerprintBuilder.Bulls(straight, dpi).Select(p => string.Create(Inv, $"({p.X:0.00}, {p.Y:0.00})")))
                    + " in. Confirm them, or give them with --bulls x,y;x,y.");
            }

            var fingerprint = StoreFingerprintBuilder.Make(id, straight, dpi, bulls);
            var family = TargetReferenceMaker.CheckFamily(straight, dpi, StoreTargetLibrary.All);
            output.WriteLine(TargetReferenceMaker.Describe(family));
            var reference = new TargetReference(new StoreTarget(id, maker, name, size, Option(rest, "--catalogue") ?? "", family.Family),
                fingerprint, target.Source, target.Uncertainty, target.Says);
            string file = Option(rest, "--out") ?? id + ".glref";
            File.WriteAllText(file, reference.Write());
            output.WriteLine(string.Create(Inv,
                $"{file}: {fingerprint.Points.Count} features, {fingerprint.Bulls.Count} bulls, made at {dpi:0} pixels an inch, {new FileInfo(file).Length / 1024.0:0.0} KB. The photograph is not in it."));
            return family.Duplicate ? 3 : 0;
        }
    }

    /// <summary>
    /// Entry 344 section 3, what Code checks of a submitted reference: it recognizes its own photograph, nothing already in the library claims
    /// that photograph at the same size, and where something claims it at another size the reference names that family.
    /// </summary>
    private static int Check(string file, string photo, TextWriter output)
    {
        var reference = TargetReference.Read(File.ReadAllText(file));
        var backend = new OpenCvFingerprintBackend();
        var picture = backend.Describe(photo) ?? throw new InvalidDataException($"{photo} is not an image");
        StoreTargetLibrary.Install([reference]);
        var own = StoreTargetRecognizer.Try(reference.Target, picture, backend);
        StoreTargetLibrary.Uninstall();
        bool recognizes = own.Inliers >= StoreTargetRecognizer.LeastInliers && own.Layout >= StoreTargetRecognizer.LeastLayout;
        output.WriteLine(string.Create(Inv, $"Its own photograph: {own.Inliers} features, layout {own.Layout:0.000}: {(recognizes ? "recognized" : "NOT recognized")}."));
        bool ok = recognizes;
        foreach (var other in StoreTargetLibrary.Shipped.Where(t => t.Id != reference.Target.Id))
        {
            var c = StoreTargetRecognizer.Try(other, picture, backend);
            if (c.Inliers < StoreTargetRecognizer.LeastInliers || c.Layout < StoreTargetRecognizer.LeastLayout || own.ToImage is null)
            {
                continue;
            }

            double ratio = Scale(c) / Scale(own);
            bool same = Math.Abs(ratio - 1) < 0.03;
            string family = other.Family ?? FamilyFinding.FamilyName(other);
            bool right = !same && reference.Target.Family == family;
            ok &= right;
            output.WriteLine(same
                ? string.Create(Inv, $"DUPLICATE: {other.Title} claims it at the same size (layout {c.Layout:0.000}).")
                : string.Create(Inv, $"{other.Title} claims it at {ratio:0.00} times the size: family should be \"{family}\", the reference says \"{reference.Target.Family}\" ({(right ? "right" : "WRONG")})."));
        }

        output.WriteLine(ok ? "The reference passes." : "The reference does not pass.");
        return ok ? 0 : 1;

        static double Scale(StoreTargetCandidate c)
        {
            var layout = c.Target.Fingerprint.Layout;
            var (xx, xy, yx, yy) = c.ToImage!.Jacobian(new PointD(layout.X0 + (layout.Width / 2), layout.Y0 + (layout.Height / 2)));
            return Math.Sqrt(Math.Abs((xx * yy) - (xy * yx)));
        }
    }

    /// <summary>Puts a checked reference into the built-in library: its fingerprint beside the others, its line in the list, and its family on the product it shares artwork with.</summary>
    private static int Add(string file, string folder, TextWriter output, TextWriter error)
    {
        var reference = TargetReference.Read(File.ReadAllText(file));
        string list = Path.Combine(folder, "library.json");
        var root = JsonNode.Parse(File.ReadAllText(list))!.AsObject();
        var targets = root["targets"]!.AsArray();
        if (targets.Any(t => (string?)t!["id"] == reference.Target.Id))
        {
            error.WriteLine($"{reference.Target.Id} is already in the library.");
            return 1;
        }

        File.WriteAllBytes(Path.Combine(folder, reference.Target.Id + ".glfp"), reference.Fingerprint.ToBytes());
        if (reference.Target.Family is { } family)
        {
            foreach (var t in targets.OfType<JsonObject>().Where(t => t["family"] is null && FamilyName((string)t["name"]!) == family))
            {
                t["family"] = family;
            }
        }

        // Entry 347: every product added makes a newer library, which is what a fetched copy is compared against.
        root["version"] = ((int?)root["version"] ?? 1) + 1;
        targets.Add(new JsonObject
        {
            ["id"] = reference.Target.Id,
            ["maker"] = reference.Target.Maker,
            ["name"] = reference.Target.Name,
            ["size"] = reference.Target.Size,
            ["catalog"] = reference.Target.Catalogue,
            ["family"] = reference.Target.Family,
            ["scaleSource"] = reference.Source.ToString(),
            ["scaleUncertainty"] = Math.Round(reference.Uncertainty, 5),
        });
        File.WriteAllBytes(list, System.Text.Encoding.UTF8.GetBytes(root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + "\n"));
        output.WriteLine($"{reference.Target.Id} added to {folder}. Run shipping-gate.py --generate after git add.");
        return 0;

        static string FamilyName(string name) => string.Join(' ', name.Replace("{size}", "", StringComparison.Ordinal).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>The signed library file: every built-in product as a reference, signed with the key given.</summary>
    private static int Library(string file, IReadOnlyList<string> rest, TextWriter output, TextWriter error)
    {
        // Entry 347: the nightly passes the update key in its secret, as update-manifest takes it; --key names a file instead.
        string? keyText = Option(rest, "--key") is { } keyFile ? File.ReadAllText(keyFile) : Environment.GetEnvironmentVariable(UpdateKeys.SecretName);
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return Fail(error);
        }

        int version = Option(rest, "--version") is { } v ? int.Parse(v, Inv) : StoreTargetLibrary.BuiltInVersion;
        var references = StoreTargetLibrary.Shipped.Select(t => new TargetReference(t, t.Fingerprint, ScaleSource.Scan, 0, "")).ToList();
        byte[] key = Convert.FromBase64String(keyText.Trim());
        File.WriteAllText(file, StoreLibraryFile.Sign(StoreLibraryFile.Payload(version, references), key));
        output.WriteLine($"{file}: {references.Count} products, version {version}, signed.");
        return 0;
    }
}
