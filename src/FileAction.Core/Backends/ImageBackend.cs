using ImageMagick;
using static FileAction.Core.Loc;

namespace FileAction.Core.Backends;

/// <summary>Image conversions and edits through Magick.NET (ImageMagick, runs in-process).</summary>
public static class ImageBackend
{
    private static MagickReadSettings ReadSettings(string path, bool allFrames)
    {
        var settings = new MagickReadSettings();
        if (Formats.KeyOf(path) == "svg")
        {
            settings.Format = MagickFormat.Svg;
            settings.Density = new Density(192);
            settings.BackgroundColor = MagickColors.Transparent;
        }
        if (!allFrames)
        {
            // Only the first frame for animated GIF/WebP/multi-page TIFF.
            settings.FrameIndex = 0;
            settings.FrameCount = 1;
        }
        return settings;
    }

    private static MagickImage Load(string path)
    {
        // Through a stream: ImageMagick's own file access fails on paths longer than 260 characters.
        MagickImage image;
        using (var stream = File.OpenRead(path))
            image = new MagickImage(stream, ReadSettings(path, allFrames: false));
        if (Formats.KeyOf(path) == "svg" && Math.Max(image.Width, image.Height) < 512)
        {
            // An SVG with only a viewBox comes out tiny (20×20); render it again so the long side is ~1024 px.
            var scale = 1024.0 / Math.Max(1, Math.Max(image.Width, image.Height));
            image.Dispose();
            var settings = ReadSettings(path, allFrames: false);
            settings.Density = new Density(Math.Min(192 * scale, 4800));
            using var again = File.OpenRead(path);
            image = new MagickImage(again, settings);
        }
        Normalize(image);
        return image;
    }

    /// <summary>Upright, and in sRGB: CMYK and wide-gamut images would otherwise show wrong colours.</summary>
    private static void Normalize(MagickImage image)
    {
        image.AutoOrient();
        if (image.ColorSpace == ColorSpace.CMYK)
        {
            if (image.GetColorProfile() is null)
                image.TransformColorSpace(ColorProfiles.USWebCoatedSWOP, ColorProfiles.SRGB);
            else
                image.TransformColorSpace(ColorProfiles.SRGB);
        }
    }

    /// <summary>
    /// Removes where a photo was taken (EXIF GPS, and XMP when it holds a position); everything else in the
    /// metadata stays.
    /// </summary>
    public static void RemoveLocation(MagickImage image)
    {
        if (image.GetExifProfile() is { } exif)
        {
            var gps = exif.Values.Where(v => v.Tag.ToString().StartsWith("GPS", StringComparison.Ordinal)).Select(v => v.Tag).ToList();
            if (gps.Count > 0)
            {
                foreach (var tag in gps)
                    exif.RemoveValue(tag);
                image.SetProfile(exif);
            }
        }
        if (image.GetXmpProfile() is { } xmp && System.Text.Encoding.UTF8.GetString(xmp.ToByteArray()).Contains("GPS", StringComparison.Ordinal))
            image.RemoveProfile("xmp");
    }

    /// <summary>True for a GIF/WebP with more than one frame.</summary>
    public static bool IsAnimated(string path)
    {
        if (Formats.KeyOf(path) is not ("gif" or "webp"))
            return false;
        using var stream = File.OpenRead(path);
        using var frames = new MagickImageCollection(stream, ReadSettings(path, allFrames: true));
        return frames.Count > 1;
    }

    private static MagickImageCollection LoadFrames(string path)
    {
        using var stream = File.OpenRead(path);
        var frames = new MagickImageCollection(stream, ReadSettings(path, allFrames: true));
        frames.Coalesce();
        return frames;
    }

    private static void Save(IMagickImage<byte> image, string output, MagickFormat format)
    {
        if (format == MagickFormat.Ico)
        {
            SaveIcon(image, output);
            return;
        }
        using var stream = File.Create(output);
        image.Write(stream, format);
    }

    private static void Save(MagickImageCollection images, string output, MagickFormat format)
    {
        using var stream = File.Create(output);
        images.Write(stream, format);
    }

    /// <summary>
    /// An .ico with every size stored as PNG (supported since Windows Vista): a few dozen KB instead of the
    /// ~370 KB of raw bitmaps ImageMagick writes. Sizes above the picture's own are left out.
    /// </summary>
    private static void SaveIcon(IMagickImage<byte> image, string output)
    {
        var side = Math.Max(image.Width, image.Height);
        var sizes = new uint[] { 256, 128, 64, 48, 32, 16 }.Where(s => s <= Math.Max(16, Math.Min(side, 256u))).ToList();
        var layers = new List<(uint Size, byte[] Png)>();
        foreach (var size in sizes)
        {
            using var layer = image.Clone();
            layer.Resize(new MagickGeometry(size, size) { IgnoreAspectRatio = true });
            layer.Strip();
            layers.Add((size, layer.ToByteArray(MagickFormat.Png32)));
        }
        using var file = File.Create(output);
        using var writer = new BinaryWriter(file);
        writer.Write((ushort)0);      // reserved
        writer.Write((ushort)1);      // type: icon
        writer.Write((ushort)layers.Count);
        var offset = 6 + 16 * layers.Count;
        foreach (var (size, png) in layers)
        {
            writer.Write((byte)(size >= 256 ? 0 : size)); // 0 means 256
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0);    // palette colours
            writer.Write((byte)0);    // reserved
            writer.Write((ushort)1);  // colour planes
            writer.Write((ushort)32); // bits per pixel
            writer.Write(png.Length);
            writer.Write(offset);
            offset += png.Length;
        }
        foreach (var (_, png) in layers)
            writer.Write(png);
    }

    /// <summary>Animated output: GIF frames are re-optimised (only the changing parts are stored).</summary>
    private static void SaveFrames(MagickImageCollection frames, string output, string format, int quality)
    {
        foreach (var frame in frames)
            frame.Quality = (uint)Math.Clamp(quality, 1, 100);
        if (format == "gif")
            frames.Optimize();
        Save(frames, output, ToMagickFormat(format));
    }

    public static MagickFormat ToMagickFormat(string format) => format switch
    {
        "jpg" => MagickFormat.Jpeg,
        "png" => MagickFormat.Png,
        "webp" => MagickFormat.WebP,
        "heic" => MagickFormat.Heic,
        "avif" => MagickFormat.Avif,
        "tiff" => MagickFormat.Tiff,
        "bmp" => MagickFormat.Bmp,
        "gif" => MagickFormat.Gif,
        "ico" => MagickFormat.Ico,
        "pdf" => MagickFormat.Pdf,
        _ => throw new ConversionException(T($"Nepodporovaný obrázkový formát: {format}", $"Unsupported image format: {format}")),
    };

    private static void PrepareFor(MagickImage image, string format, int quality)
    {
        image.Quality = (uint)Math.Clamp(quality, 1, 100);
        if (format is "jpg" or "bmp" or "pdf" && image.HasAlpha)
        {
            image.BackgroundColor = MagickColors.White;
            image.Alpha(AlphaAction.Remove);
        }
        if (format == "ico")
        {
            // Icons are square: pad instead of stretching, and never blow a small picture up.
            var side = Math.Max(image.Width, image.Height);
            if (image.Width != image.Height)
            {
                image.BackgroundColor = MagickColors.Transparent;
                image.Extent(side, side, Gravity.Center);
            }
            if (side > 256)
                image.Resize(new MagickGeometry(256, 256));
            var sizes = new uint[] { 256, 128, 64, 48, 32, 16 }.Where(s => s <= Math.Max(16, Math.Min(side, 256u)));
            image.Settings.SetDefine(MagickFormat.Ico, "auto-resize", string.Join(",", sizes));
        }
        if (format == "pdf")
        {
            // Page size follows the pixel density: fit the long side to an A4 page (29.7 cm) instead of
            // 72 DPI, which made a phone photo a page over a metre wide.
            var dpi = Math.Max(72, Math.Max(image.Width, image.Height) / 11.69);
            image.Density = new Density(dpi, dpi, DensityUnit.PixelsPerInch);
        }
        if (format == "tiff")
            image.Settings.Compression = CompressionMethod.LZW;
    }

    /// <summary>The JPEG quality to re-save with: never higher than the source had (that only makes the file bigger).</summary>
    private static int QualityFor(MagickImage image, string format, int wanted) =>
        format == "jpg" && image.Format == MagickFormat.Jpeg && image.Quality is > 0 and < 100 ? Math.Min(wanted, (int)image.Quality) : wanted;

    public static Task ConvertAsync(string input, string output, string format, Settings settings, CancellationToken ct) =>
        Task.Run(() =>
        {
            // GIF ↔ WebP keeps the animation.
            if (format is "gif" or "webp" && IsAnimated(input))
            {
                using var frames = LoadFrames(input);
                ct.ThrowIfCancellationRequested();
                SaveFrames(frames, output, format, settings.JpegQuality);
                return;
            }
            using var image = Load(input);
            ct.ThrowIfCancellationRequested();
            if (settings.StripLocation)
                RemoveLocation(image);
            PrepareFor(image, format, QualityFor(image, format, settings.JpegQuality));
            Save(image, output, ToMagickFormat(format));
        }, ct);

    /// <summary>Applies <paramref name="edit"/> and saves in the source format (every frame of an animation).</summary>
    public static Task EditAsync(string input, string output, Action<MagickImage> edit, int? quality, CancellationToken ct, bool stripLocation = false) =>
        Task.Run(() =>
        {
            var format = OutputFormatFor(Formats.KeyOf(input));
            if (IsAnimated(input))
            {
                using var frames = LoadFrames(input);
                foreach (var frame in frames)
                {
                    ct.ThrowIfCancellationRequested();
                    edit((MagickImage)frame);
                    frame.ResetPage();
                }
                SaveFrames(frames, output, format, quality ?? 92);
                return;
            }
            using var image = Load(input);
            var q = QualityFor(image, format, quality ?? 92);
            if (stripLocation)
                RemoveLocation(image);
            edit(image);
            ct.ThrowIfCancellationRequested();
            PrepareFor(image, format, q);
            Save(image, output, ToMagickFormat(format));
        }, ct);

    /// <summary>
    /// Formats we can read but not write are saved as something close: SVG as PNG, HEIC (no encoder in this
    /// ImageMagick build) as JPG.
    /// </summary>
    public static string OutputFormatFor(string inputFormat) => inputFormat switch
    {
        "svg" => "png",
        "heic" => "jpg",
        _ => inputFormat,
    };

    /// <summary>
    /// Removes EXIF, GPS, XMP and comments. The colour profile is not simply dropped (that shifts the colours
    /// of Adobe RGB or P3 photos): the pixels are converted to sRGB first, which needs no profile.
    /// </summary>
    public static void StripMetadata(MagickImage image)
    {
        if (image.GetColorProfile() is { } profile && !profile.Description?.Contains("sRGB", StringComparison.OrdinalIgnoreCase) == true)
            image.TransformColorSpace(ColorProfiles.SRGB);
        image.Strip();
    }

    /// <summary>
    /// Vectorises a picture (the first frame of an animation) into SVG shapes. Made for logos, icons and drawings;
    /// a photo comes out as a poster with flat areas of colour.
    /// </summary>
    public static Task ToSvgAsync(string input, string output, CancellationToken ct) =>
        Task.Run(() =>
        {
            const int Colors = 32, LongSideMin = 1000, LongSideMax = 2000;
            using var image = Load(input);
            int outWidth = (int)image.Width, outHeight = (int)image.Height;
            var longSide = Math.Max(outWidth, outHeight);
            // Small pictures are enlarged first: the edge smoothing then gives a real curve instead of pixel steps.
            if (longSide < LongSideMin)
                image.Resize(new Percentage(100.0 * Math.Min(LongSideMin / (double)longSide, 8)));
            else if (longSide > LongSideMax)
                image.Resize(new Percentage(100.0 * LongSideMax / longSide));
            int width = (int)image.Width, height = (int)image.Height;
            ct.ThrowIfCancellationRequested();

            using var pixels = image.GetPixels();
            var original = pixels.ToByteArray(PixelMapping.RGBA)!;
            using var flat = (MagickImage)image.Clone();
            // Colours under transparency are arbitrary and must not take up a place in the palette.
            flat.BackgroundColor = MagickColors.White;
            flat.Alpha(AlphaAction.Remove);
            flat.Quantize(new QuantizeSettings { Colors = Colors, ColorSpace = ColorSpace.sRGB, DitherMethod = DitherMethod.No });
            using var flatPixels = flat.GetPixels();
            var reduced = flatPixels.ToByteArray(PixelMapping.RGBA)!;
            for (var i = 3; i < reduced.Length; i += 4)
                reduced[i] = original[i];
            File.WriteAllText(output, ImageTracer.Trace(reduced, width, height, outWidth, outHeight, ct), new System.Text.UTF8Encoding(false));
        }, ct);

    public static Task CompressAsync(string input, string output, int quality, CancellationToken ct) =>
        Task.Run(() =>
        {
            var format = OutputFormatFor(Formats.KeyOf(input));
            if (IsAnimated(input))
            {
                using var frames = LoadFrames(input);
                foreach (var frame in frames)
                    StripMetadata((MagickImage)frame);
                SaveFrames(frames, output, format, quality);
                return;
            }
            using var image = Load(input);
            var q = QualityFor(image, format, quality);
            StripMetadata(image);
            image.Quality = (uint)Math.Clamp(q, 1, 100);
            if (format == "png")
            {
                // Lossy palette reduction gives the biggest win for PNG.
                if (image.TotalColors > 256)
                    image.Quantize(new QuantizeSettings { Colors = 256, DitherMethod = DitherMethod.FloydSteinberg });
                image.Settings.SetDefine(MagickFormat.Png, "compression-level", "9");
            }
            else if (format == "jpg")
            {
                image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
                image.Settings.Interlace = Interlace.Jpeg;
            }
            PrepareFor(image, format, q);
            Save(image, output, ToMagickFormat(format));
        }, ct);

    /// <summary>A user preset: optional resize, grayscale and metadata removal, saved in <paramref name="format"/>.</summary>
    public static Task PresetAsync(string input, string output, string format, int quality, int? maxSide, int? percent,
        bool grayscale, bool strip, CancellationToken ct, bool stripLocation = false) =>
        Task.Run(() =>
        {
            void Apply(MagickImage image)
            {
                if (maxSide is > 0)
                    ResizeMax(image, (uint)maxSide.Value);
                else if (percent is > 0 and < 100)
                    ResizePercent(image, percent.Value);
                if (grayscale)
                    Grayscale(image);
                if (strip)
                    StripMetadata(image);
                else if (stripLocation)
                    RemoveLocation(image);
            }

            if (format is "gif" or "webp" && IsAnimated(input))
            {
                using var frames = LoadFrames(input);
                foreach (var frame in frames)
                {
                    ct.ThrowIfCancellationRequested();
                    Apply((MagickImage)frame);
                    frame.ResetPage();
                }
                SaveFrames(frames, output, format, quality);
                return;
            }
            using var image = Load(input);
            var q = QualityFor(image, format, quality);
            Apply(image);
            ct.ThrowIfCancellationRequested();
            PrepareFor(image, format, q);
            Save(image, output, ToMagickFormat(format));
        }, ct);

    public static Task ImagesToPdfAsync(IReadOnlyList<string> inputs, string output, int quality, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var collection = new MagickImageCollection();
            foreach (var input in inputs)
            {
                ct.ThrowIfCancellationRequested();
                var image = Load(input);
                PrepareFor(image, "pdf", quality);
                collection.Add(image);
            }
            Save(collection, output, MagickFormat.Pdf);
        }, ct);

    public static void ResizePercent(MagickImage image, double percent) =>
        image.Resize(new Percentage(percent));

    public static void ResizeMax(MagickImage image, uint maxSide)
    {
        if (image.Width <= maxSide && image.Height <= maxSide)
            return;
        image.Resize(new MagickGeometry(maxSide, maxSide));
    }

    /// <summary>True when <see cref="ResizeMax"/> would leave the image as it is.</summary>
    public static bool FitsWithin(string path, uint maxSide)
    {
        var info = new MagickImageInfo();
        using (var stream = File.OpenRead(path))
            info.Read(stream);
        // EXIF rotation swaps the sides, but "fits" is the same either way.
        return info.Width <= maxSide && info.Height <= maxSide;
    }

    public static void CropSquare(MagickImage image)
    {
        var side = Math.Min(image.Width, image.Height);
        image.Crop(side, side, Gravity.Center);
        image.ResetPage();
    }

    /// <summary>Perceptual grey (the gamma-encoded luma), not linear light, which came out half as bright.</summary>
    public static void Grayscale(MagickImage image) => image.Grayscale(PixelIntensityMethod.Rec709Luma);
}
