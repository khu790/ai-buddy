using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using Forms = System.Windows.Forms;

namespace AiBuddy;

public sealed class ScreenFrame : IDisposable
{
    private readonly Bitmap _image;
    public byte[] ChangeSample { get; }

    internal ScreenFrame(Bitmap image, byte[] sample)
    {
        _image = image;
        ChangeSample = sample;
    }

    public byte[] ToJpeg()
    {
        using var stream = new MemoryStream();
        _image.Save(stream, ImageFormat.Jpeg);
        return stream.ToArray();
    }

    public void Dispose() => _image.Dispose();
}

public static class ScreenCapture
{
    public static ScreenFrame CaptureFrame()
    {
        var bounds = Forms.SystemInformation.VirtualScreen;
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new InvalidOperationException("No desktop screen is available.");

        using var full = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(full))
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);

        const int maxWidth = 1600;
        var scale = Math.Min(1d, maxWidth / (double)full.Width);
        var width = Math.Max(1, (int)(full.Width * scale));
        var height = Math.Max(1, (int)(full.Height * scale));
        var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(result))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(full, 0, 0, width, height);
        }

        using var sample = new Bitmap(48, 27, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(sample))
        {
            graphics.InterpolationMode = InterpolationMode.Bilinear;
            graphics.DrawImage(result, 0, 0, sample.Width, sample.Height);
        }
        var bytes = new byte[sample.Width * sample.Height * 3];
        var index = 0;
        for (var y = 0; y < sample.Height; y++)
            for (var x = 0; x < sample.Width; x++)
            {
                var color = sample.GetPixel(x, y);
                bytes[index++] = color.R;
                bytes[index++] = color.G;
                bytes[index++] = color.B;
            }
        return new ScreenFrame(result, bytes);
    }

    public static double DifferenceRatio(byte[] previous, byte[] current)
    {
        if (previous.Length != current.Length || current.Length == 0) return 1;
        var changed = 0;
        for (var i = 0; i < current.Length; i += 3)
        {
            var distance = Math.Abs(previous[i] - current[i]) + Math.Abs(previous[i + 1] - current[i + 1]) + Math.Abs(previous[i + 2] - current[i + 2]);
            if (distance > 75) changed++;
        }
        return changed / (double)(current.Length / 3);
    }
}
