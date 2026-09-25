using System.Globalization;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Runtime;
using SizeF = Android.Util.SizeF;

namespace GroupLab.Android.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 209 section 1.3: every rear camera the phone reports through Camera2, logical and physical, with its focal
/// lengths, sensor size, largest still, and whether it reports intrinsics and distortion. Nothing is opened or photographed; these are
/// the camera's own characteristics, read without starting it.
/// </summary>
public static class SpikeCameras
{
    public static IEnumerable<string> Report(Context context)
    {
        var inv = CultureInfo.InvariantCulture;
        var manager = (CameraManager)context.GetSystemService(Context.CameraService)!;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ids = new Queue<string>(manager.GetCameraIdList());
        while (ids.Count > 0)
        {
            string id = ids.Dequeue();
            if (!seen.Add(id))
            {
                continue;
            }

            var c = manager.GetCameraCharacteristics(id);
            int? facing = c.Get(CameraCharacteristics.LensFacing!) is Java.Lang.Integer f ? f.IntValue() : null;
            if (facing is not null && facing != (int)LensFacing.Back)
            {
                continue;
            }

            var focal = (float[]?)c.Get(CameraCharacteristics.LensInfoAvailableFocalLengths!);
            var sensor = c.Get(CameraCharacteristics.SensorInfoPhysicalSize!)?.JavaCast<SizeF>();
            var map = c.Get(CameraCharacteristics.ScalerStreamConfigurationMap!)?.JavaCast<StreamConfigurationMap>();
            var largest = map?.GetOutputSizes((int)ImageFormatType.Jpeg)?.MaxBy(s => (long)s.Width * s.Height);
            var intrinsics = (float[]?)c.Get(CameraCharacteristics.LensIntrinsicCalibration!);
            var distortion = (float[]?)c.Get(CameraCharacteristics.LensDistortion!);
            var physical = c.PhysicalCameraIds ?? [];
            foreach (string p in physical)
            {
                ids.Enqueue(p);
            }

            double? equivalent = focal is { Length: > 0 } && sensor is not null
                ? focal[0] * 43.27 / Math.Sqrt((sensor.Width * sensor.Width) + (sensor.Height * sensor.Height))
                : null;
            yield return string.Concat(
                string.Create(inv, $"camera {id}: focal {(focal is null ? "none" : string.Join("/", focal.Select(f => f.ToString("0.00", inv))))} mm{(equivalent is { } e ? $" ({e:0} mm equivalent)" : "")}, "),
                string.Create(inv, $"sensor {(sensor is null ? "not reported" : $"{sensor.Width:0.00} by {sensor.Height:0.00} mm")}, largest still {(largest is null ? "none" : $"{largest.Width} by {largest.Height} ({largest.Width * (double)largest.Height / 1e6:0.0} MP)")}, "),
                string.Create(inv, $"intrinsics {(intrinsics is null ? "not reported" : string.Join(" ", intrinsics.Select(v => v.ToString("0.#", inv))))}, "),
                string.Create(inv, $"distortion {(distortion is null ? "not reported" : string.Join(" ", distortion.Select(v => v.ToString("0.####", inv))))}"),
                (physical.Count > 0 ? $", made of physical cameras {string.Join(", ", physical)}" : ""));
        }
    }
}
