using System.Globalization;
using FileAction.Core.Backends;
using static FileAction.Core.Operations.Helpers;
using static FileAction.Core.Loc;

namespace FileAction.Core.Operations;

/// <summary>Every conversion and tool the wheels can offer. The app adds its own file actions.</summary>
public static class Catalog
{
    public static IReadOnlyList<Operation> Build()
    {
        var ops = new List<Operation>();
        AddImages(ops);
        AddAudio(ops);
        AddVideo(ops);
        AddPdf(ops);
        AddDocuments(ops);
        AddSlides(ops);
        AddSpreadsheets(ops);
        AddSubtitles(ops);
        AddArchives(ops);
        return ops;
    }

    /// <summary>Operations for a wheel that apply to the dropped items and whose tools are installed.</summary>
    public static IReadOnlyList<Operation> For(IEnumerable<Operation> all, WheelKind wheel, IReadOnlyList<FileItem> items, ToolLocator tools) =>
        all.Where(o => o.Wheel == wheel && o.AppliesTo(items) && o.IsAvailable(tools, items)).ToList();

    // ---------------------------------------------------------------- images

    private static void AddImages(List<Operation> ops)
    {
        foreach (var target in Formats.ImageOutputs)
        {
            ops.Add(new PerFileOperation($"image.to.{target}", WheelKind.Convert, Formats.Display(target), Glyphs.Photo, Formats.Images,
                async (item, ctx, _) => [await Produce(item, ctx, target, tmp => ImageBackend.ConvertAsync(item.Path, tmp, target, ctx.Settings, ctx.CancellationToken))],
                targetFormat: target, subtitle: target == "pdf" ? T("dokument", "document") : null));
        }
        ops.Add(new PerFileOperation("image.to.svg", WheelKind.Convert, "SVG", Glyphs.Photo, Formats.Images,
            async (item, ctx, _) => [await Produce(item, ctx, "svg", tmp => ImageBackend.ToSvgAsync(item.Path, tmp, ctx.CancellationToken))],
            targetFormat: "svg", subtitle: T("vektor", "vector")));
        ops.Add(new PerFileOperation("gif.to.mp4", WheelKind.Convert, "MP4", Glyphs.Video, ["gif"],
            async (item, ctx, p) => [await FfmpegBackend.ConvertAsync(item, "mp4", ctx, p)], subtitle: "video")
        { Requires = [ExternalTool.Ffmpeg] });
        ops.Add(new PerFileOperation("gif.to.webm", WheelKind.Convert, "WEBM", Glyphs.Video, ["gif"],
            async (item, ctx, p) => [await FfmpegBackend.ConvertAsync(item, "webm", ctx, p)], subtitle: "video")
        { Requires = [ExternalTool.Ffmpeg] });

        ImageTool(ops, "image.compress", T("Komprimovat", "Compress"), Glyphs.Shrink, T("komprimováno", "compressed"),
            async (item, ctx, tmp) =>
            {
                await ImageBackend.CompressAsync(item.Path, tmp, ctx.Settings.CompressQuality, ctx.CancellationToken);
                // A "compressed" copy that is bigger than the original helps nobody.
                if (new FileInfo(tmp).Length >= new FileInfo(item.Path).Length)
                    throw new ConversionException(T("Obrázek je už dobře zkomprimovaný, menší verze nevznikla.", "The image is already well compressed; no smaller copy was made."), nothingToDo: true);
            });
        ImageEdit(ops, "image.half", T("Zmenšit", "Shrink"), Glyphs.Shrink, "50 %", T("zmenšeno", "smaller"), img => ImageBackend.ResizePercent(img, 50));
        ImageTool(ops, "image.max1920", "Max 1920 px", Glyphs.Shrink, "1920", (item, ctx, tmp) =>
            ImageBackend.FitsWithin(item.Path, 1920)
                ? throw new ConversionException(T("Obrázek už je menší než 1920 px, nic se nezměnilo.", "The image is already smaller than 1920 px; nothing to change."), nothingToDo: true)
                : ImageBackend.EditAsync(item.Path, tmp, img => ImageBackend.ResizeMax(img, 1920), ctx.Settings.JpegQuality, ctx.CancellationToken, ctx.Settings.StripLocation),
            subtitle: T("na web", "for web"));
        ImageEdit(ops, "image.rotate.right", T("Otočit", "Rotate"), Glyphs.Rotate, T("doprava", "right"), T("otočeno", "rotated"), img => img.Rotate(90));
        ImageEdit(ops, "image.rotate.left", T("Otočit", "Rotate"), Glyphs.Rotate, T("doleva", "left"), T("otočeno", "rotated"), img => img.Rotate(-90));
        ImageEdit(ops, "image.flip", T("Zrcadlit", "Mirror"), Glyphs.Flip, null, T("zrcadleno", "mirrored"), img => img.Flop());
        ImageEdit(ops, "image.square", T("Čtverec", "Square"), Glyphs.Crop, T("oříznout", "crop"), T("čtverec", "square"), ImageBackend.CropSquare);
        ImageEdit(ops, "image.grayscale", T("Černobíle", "Grayscale"), Glyphs.Color, null, T("černobíle", "grayscale"), ImageBackend.Grayscale);
        ImageEdit(ops, "image.strip", T("Bez metadat", "No metadata"), Glyphs.Clear, "GPS, EXIF", T("bez metadat", "no metadata"), ImageBackend.StripMetadata);
        ops.Add(new BatchOperation("image.merge.pdf", WheelKind.Tools, T("Spojit do PDF", "Merge to PDF"), Glyphs.Merge,
            BatchOperation.AllOf(Formats.Images, min: 2),
            async (items, ctx) => [await OutputPaths.ProduceAsync(ctx.OutputDirFor(items[0].Path), items[0].BaseName, "pdf",
                tmp => ImageBackend.ImagesToPdfAsync(items.Select(i => i.Path).OrderBy(p => p, NaturalComparer.Instance).ToList(), tmp, ctx.Settings.JpegQuality, ctx.CancellationToken))]));
    }

    private static void ImageTool(List<Operation> ops, string id, string label, string glyph, string suffix, Func<FileItem, OperationContext, string, Task> run, string? subtitle = null) =>
        ops.Add(new PerFileOperation(id, WheelKind.Tools, label, glyph, Formats.Images,
            async (item, ctx, _) => [await OutputPaths.ProduceAsync(ctx.OutputDirFor(item.Path), $"{item.BaseName} ({suffix})",
                ImageBackend.OutputFormatFor(item.Format), tmp => run(item, ctx, tmp))], subtitle: subtitle));

    private static void ImageEdit(List<Operation> ops, string id, string label, string glyph, string? subtitle, string suffix, Action<ImageMagick.MagickImage> edit) =>
        ImageTool(ops, id, label, glyph, suffix, (item, ctx, tmp) => ImageBackend.EditAsync(item.Path, tmp, edit, ctx.Settings.JpegQuality, ctx.CancellationToken, ctx.Settings.StripLocation), subtitle);

    // ---------------------------------------------------------------- audio

    private static void AddAudio(List<Operation> ops)
    {
        foreach (var target in Formats.AudioOutputs)
        {
            ops.Add(new PerFileOperation($"audio.to.{target}", WheelKind.Convert, Formats.Display(target), Glyphs.Audio, Formats.Audio,
                async (item, ctx, p) => [await FfmpegBackend.ConvertAsync(item, target, ctx, p)], targetFormat: target)
            { Requires = [ExternalTool.Ffmpeg] });
        }

        Ffmpeg(ops, "audio.compress", T("Komprimovat", "Compress"), Glyphs.Shrink, Formats.Audio, T("komprimováno", "compressed"),
            (item, ctx, info, _) => ["-map", "0:a:0", "-c:a", "libmp3lame", "-b:a", "128k", "-vn"], outputFormat: _ => "mp3", subtitle: "MP3 128k",
            skip: info => info.LossyAudio && info.AudioBitrate is <= 128
                ? T("Zvuk už má nízký datový tok (128 kbit/s nebo méně), menší by jen ztratil kvalitu.", "The audio is already at 128 kbit/s or less; smaller would only lose quality.")
                : null,
            mustShrink: true);
        Ffmpeg(ops, "audio.normalize", T("Normalizovat", "Normalize"), Glyphs.Equalizer, Formats.Audio, T("normalizováno", "normalized"),
            (item, ctx, info, toneMap) =>
            [
                // loudnorm works at 192 kHz internally; go back to the source rate or the file grows several times.
                "-af", $"loudnorm=I=-16:TP=-1.5:LRA=11,aresample={info.SampleRate ?? 48000}",
                .. FfmpegBackend.CodecArgs(FfmpegBackend.SafeContainer(item.Format), ctx.Settings, info, toneMap),
            ],
            subtitle: T("hlasitost", "loudness"));
        ops.Add(Trim("audio.trim", Formats.Audio));
        ops.Add(new BatchOperation("audio.concat", WheelKind.Tools, T("Spojit", "Join"), Glyphs.Merge, BatchOperation.AllOf(Formats.Audio, min: 2),
            async (items, ctx) =>
            {
                var sorted = items.OrderBy(i => i.Path, NaturalComparer.Instance).ToList();
                var infos = new List<MediaInfo>();
                foreach (var i in sorted)
                    infos.Add(await MediaProbe.ProbeAsync(ctx, i.Path));
                // Every part in the same format, so a mono first file doesn't turn the rest into mono.
                var rate = infos.Max(i => i.SampleRate ?? 44100);
                var layout = infos.Any(i => i.Channels >= 2) ? "stereo" : "mono";
                var format = FfmpegBackend.SafeContainer(sorted[0].Format);
                var graph = string.Concat(Enumerable.Range(0, sorted.Count).Select(i => $"[{i}:a]aformat=sample_rates={rate}:channel_layouts={layout}[a{i}];"))
                    + string.Concat(Enumerable.Range(0, sorted.Count).Select(i => $"[a{i}]")) + $"concat=n={sorted.Count}:v=0:a=1[a]";
                return [await OutputPaths.ProduceAsync(ctx.OutputDirFor(sorted[0].Path), $"{sorted[0].BaseName} ({T("spojeno", "joined")})", format, tmp =>
                    FfmpegBackend.RunAsync(ctx, sorted.Select(i => i.Path).ToList(),
                        ["-filter_complex", graph, "-map", "[a]", .. FfmpegBackend.CodecArgs(format, ctx.Settings, infos[0] with { HasCover = false }, false)], tmp, ctx.Progress))];
            })
        { Requires = [ExternalTool.Ffmpeg] });
    }

    // ---------------------------------------------------------------- video

    /// <summary>Scales the short side (a portrait phone video "720p" is 720 wide, not 406×720), never up.</summary>
    private static string ShortSide(int size) =>
        $"scale='if(gt(iw,ih),-2,min({size},iw))':'if(gt(iw,ih),min({size},ih),-2)',setsar=1";

    private static void AddVideo(List<Operation> ops)
    {
        foreach (var target in Formats.VideoOutputs)
        {
            ops.Add(new PerFileOperation($"video.to.{target}", WheelKind.Convert, Formats.Display(target), target == "mp3" ? Glyphs.Audio : Glyphs.Video, Formats.Video,
                async (item, ctx, p) => [await FfmpegBackend.ConvertAsync(item, target, ctx, p)], targetFormat: target,
                subtitle: target switch { "mp3" => T("jen zvuk", "audio only"), "gif" => T("animace", "animation"), _ => null })
            { Requires = [ExternalTool.Ffmpeg] });
        }

        Ffmpeg(ops, "video.compress", T("Komprimovat", "Compress"), Glyphs.Shrink, Formats.Video, T("komprimováno", "compressed"),
            (item, ctx, info, toneMap) => H264(ctx.Settings.CompressVideoCrf, "slow", null, "128k", info, toneMap), outputFormat: _ => "mp4", subtitle: "H.264",
            mustShrink: true);
        Ffmpeg(ops, "video.720p", "720p", Glyphs.Shrink, Formats.Video, "720p",
            (item, ctx, info, toneMap) => H264(ctx.Settings.VideoCrf, "medium", ShortSide(720), "160k", info, toneMap), outputFormat: _ => "mp4", subtitle: T("zmenšit", "downscale"),
            skip: info => info.ShortSide is > 0 and <= 720
                ? T("Video už má 720p nebo méně, zmenšením by se nic nezískalo.", "The video is already 720p or smaller; there is nothing to gain.")
                : null);
        Ffmpeg(ops, "video.mute", T("Ztlumit", "Mute"), Glyphs.Mute, Formats.Video, T("bez zvuku", "muted"),
            (item, ctx, info, toneMap) => FfmpegBackend.SafeContainer(item.Format) == item.Format
                ? ["-map", "0:v:0", "-c:v", "copy", "-an", "-map_metadata", "0"]
                : [.. H264(ctx.Settings.VideoCrf, "medium", null, null, info, toneMap), "-an"],
            subtitle: T("bez zvuku", "muted"));
        ops.Add(Trim("video.trim", Formats.Video));
        Ffmpeg(ops, "video.rotate", T("Otočit", "Rotate"), Glyphs.Rotate, Formats.Video, T("otočeno", "rotated"),
            (item, ctx, info, toneMap) => H264(ctx.Settings.VideoCrf, "medium", "transpose=1", "160k", info, toneMap), outputFormat: _ => "mp4", subtitle: T("doprava", "right"));
        Ffmpeg(ops, "video.speed2", T("Zrychlit", "Speed up"), Glyphs.Play, Formats.Video, "2×",
            (item, ctx, info, toneMap) => [.. H264(ctx.Settings.VideoCrf, "medium", "setpts=0.5*PTS", "160k", info, toneMap, subtitles: false), "-af", "atempo=2.0"],
            outputFormat: _ => "mp4", subtitle: "2×");
        AddColorLuts(ops);
        Ffmpeg(ops, "video.thumbnail", T("Náhled", "Thumbnail"), Glyphs.Camera, Formats.Video, T("náhled", "thumbnail"),
            (item, ctx, info, toneMap) => ["-vf", (toneMap ? FfmpegBackend.ToneMap + "," : "") + "thumbnail=100", "-frames:v", "1", "-update", "1"],
            outputFormat: _ => "png", subtitle: T("snímek PNG", "PNG frame"),
            // From a tenth of the way in: many clips open on a black or fade-in frame.
            inputArgs: info => info.Duration is > 4 ? ["-ss", (info.Duration.Value * 0.1).ToString("0.###", CultureInfo.InvariantCulture)] : null);
    }

    /// <summary>
    /// Colour LUTs: Sony S-Log3 clips (recognised by the camera's own metadata) to normal Rec.709 video, and
    /// any .cube file dropped together with videos. The result is an 8-bit H.264 MP4 that keeps the recording
    /// date both inside the file and on it, so photo libraries sort it with the rest of the day.
    /// </summary>
    private static void AddColorLuts(List<Operation> ops)
    {
        ops.Add(new BatchOperation("video.slog3", WheelKind.Tools, T("Z S-Logu", "From S-Log"), Glyphs.Color,
            items => items.All(i => !i.IsDirectory && Formats.Video.Contains(i.Format) && LogVideo.Detect(i.Path) is not null),
            (items, ctx) => GradeAsync(items, ctx, "Rec.709", (item, dir) =>
            {
                var cube = Path.Combine(dir, "slog3.cube");
                File.WriteAllText(cube, LogVideo.SLog3Cube(LogVideo.Detect(item.Path)?.Gamut ?? LogVideo.Gamut.SGamut3Cine));
                // S-Log3 is defined on legal-range BT.709 video, whatever the file says.
                return $"scale=in_color_matrix=bt709:in_range=tv,format=gbrp16le,lut3d=file={LogVideo.FilterPath(cube)}:interp=tetrahedral";
            }),
            subtitle: T("Sony → běžné video", "Sony → normal video"))
        { Requires = [ExternalTool.Ffmpeg] });

        ops.Add(new BatchOperation("video.lut", WheelKind.Tools, T("Použít LUT", "Apply LUT"), Glyphs.Color,
            items => items.Count >= 2 && items.Count(i => i.Format == "cube") == 1
                     && items.All(i => !i.IsDirectory && (i.Format == "cube" || Formats.Video.Contains(i.Format))),
            (items, ctx) =>
            {
                var lut = items.Single(i => i.Format == "cube");
                var oneD = LogVideo.IsOneDimensional(lut.Path);
                return GradeAsync(items.Where(i => i != lut).ToList(), ctx, lut.BaseName, (_, dir) =>
                {
                    // A copy with a plain name: ffmpeg's filter syntax trips over commas and brackets in paths.
                    var cube = Path.Combine(dir, "lut.cube");
                    File.Copy(lut.Path, cube);
                    return $"format=gbrp16le,{(oneD ? "lut1d" : "lut3d")}=file={LogVideo.FilterPath(cube)}" + (oneD ? "" : ":interp=tetrahedral");
                });
            },
            subtitle: T("soubor .cube", ".cube file"))
        { Requires = [ExternalTool.Ffmpeg] });
    }

    private static async Task<IEnumerable<string>> GradeAsync(IReadOnlyList<FileItem> videos, OperationContext ctx, string suffix,
        Func<FileItem, string, string> filter)
    {
        var outputs = new List<string>();
        for (var i = 0; i < videos.Count; i++)
        {
            var item = videos[i];
            var temp = OutputPaths.TempDirectory();
            try
            {
                var info = await MediaProbe.ProbeAsync(ctx, item.Path);
                // setparams: the frames carry the colour tags, otherwise ffmpeg writes the primaries and curve as "unknown".
                var vf = filter(item, temp) + ",scale=out_color_matrix=bt709:out_range=tv,format=yuv420p,"
                         + "setparams=range=tv:color_primaries=bt709:color_trc=bt709:colorspace=bt709";
                List<string> args = [.. H264(ctx.Settings.VideoCrf, "medium", vf, "160k", info, toneMap: false),
                    "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709", "-color_range", "tv"];
                var output = await FfmpegBackend.TransformAsync(item, suffix, ctx, ctx.SubProgress(i, videos.Count), args, "mp4", duration: info.Duration);
                LogVideo.CopyFileDates(item.Path, output);
                outputs.Add(output);
            }
            finally
            {
                OutputPaths.TryDelete(temp);
            }
        }
        return outputs;
    }

    /// <summary>
    /// H.264 in MP4 for the video tools: all audio tracks (and text subtitles) kept, HDR tone-mapped to SDR,
    /// even dimensions.
    /// </summary>
    private static string[] H264(int crf, string preset, string? filter, string? audioBitrate, MediaInfo info, bool toneMap, bool subtitles = true)
    {
        var vf = (toneMap && info.Hdr ? FfmpegBackend.ToneMap + "," : "") + (filter is null ? "" : filter + ",") + "scale=trunc(iw/2)*2:trunc(ih/2)*2";
        var args = new List<string>(audioBitrate is null ? ["-map", "0:v:0", "-map_metadata", "0"] : FfmpegBackend.StreamMaps("mp4", info));
        if (!subtitles && args.IndexOf("0:s?") is var s and > 0)
            args.RemoveRange(s - 1, 4);
        args.AddRange(["-c:v", "libx264", "-preset", preset, "-crf", crf.ToString(CultureInfo.InvariantCulture), "-pix_fmt", "yuv420p", "-vf", vf, "-movflags", "+faststart"]);
        if (toneMap && info.Hdr)
            args.AddRange(["-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709"]);
        if (audioBitrate is not null)
            args.AddRange(["-c:a", "aac", "-b:a", audioBitrate]);
        return [.. args];
    }

    /// <summary>A per-file ffmpeg tool; the arguments may depend on what the file contains.</summary>
    /// <param name="skip">A reason to do nothing (e.g. "already 720p"), shown instead of making a pointless copy.</param>
    /// <param name="mustShrink">For Compress: a result that is not smaller than the original is thrown away.</param>
    private static void Ffmpeg(List<Operation> ops, string id, string label, string glyph, ISet<string> inputs, string suffix,
        Func<FileItem, OperationContext, MediaInfo, bool, IEnumerable<string>> args, Func<FileItem, string>? outputFormat = null, string? subtitle = null,
        Func<MediaInfo, string?>? skip = null, bool mustShrink = false, Func<MediaInfo, IEnumerable<string>?>? inputArgs = null) =>
        ops.Add(new PerFileOperation(id, WheelKind.Tools, label, glyph, inputs,
            async (item, ctx, p) =>
            {
                var info = await MediaProbe.ProbeAsync(ctx, item.Path);
                if (skip?.Invoke(info) is { } reason)
                    throw new ConversionException(reason, nothingToDo: true);
                var toneMap = info.Hdr && await MediaProbe.CanToneMapAsync(ctx);
                var result = await FfmpegBackend.TransformAsync(item, suffix, ctx, p, args(item, ctx, info, toneMap), outputFormat?.Invoke(item),
                    inputArgs: inputArgs?.Invoke(info), duration: info.Duration);
                if (mustShrink && new FileInfo(result).Length >= new FileInfo(item.Path).Length)
                {
                    OutputPaths.TryDelete(result);
                    throw new ConversionException(T("Soubor je už dobře zkomprimovaný, menší verze nevznikla.", "The file is already well compressed; no smaller copy was made."), nothingToDo: true);
                }
                return [result];
            },
            subtitle: subtitle)
        { Requires = [ExternalTool.Ffmpeg] });

    private static Operation Trim(string id, ISet<string> inputs) =>
        new PerFileOperation(id, WheelKind.Tools, T("Oříznout", "Trim"), Glyphs.Stopwatch, inputs, async (item, ctx, p) =>
        {
            var start = FfmpegBackend.ParseTime(ctx.Param("start", "0"));
            var endText = ctx.Param("end");
            double? length = endText.Length > 0 ? FfmpegBackend.ParseTime(endText) - start : null;
            if (length is <= 0)
                throw new ConversionException(T("Konec musí být až za začátkem.", "The end must come after the start."));
            var info = await MediaProbe.ProbeAsync(ctx, item.Path);
            var toneMap = info.Hdr && await MediaProbe.CanToneMapAsync(ctx);
            var format = FfmpegBackend.SafeContainer(item.Format);
            var video = Formats.Video.Contains(item.Format);
            var args = new List<string>();
            if (length is not null)
                args.AddRange(["-t", length.Value.ToString("0.###", CultureInfo.InvariantCulture)]);
            args.AddRange(video
                ? H264(ctx.Settings.VideoCrf, "medium", null, "160k", info, toneMap)
                : FfmpegBackend.CodecArgs(format, ctx.Settings, info, toneMap));
            return [await FfmpegBackend.TransformAsync(item, T("oříznuto", "trimmed"), ctx, p, args,
                video ? "mp4" : format,
                inputArgs: start > 0 ? ["-ss", start.ToString("0.###", CultureInfo.InvariantCulture)] : null,
                duration: length ?? (info.Duration - start))];
        }, subtitle: T("začátek–konec", "start–end"))
        {
            Requires = [ExternalTool.Ffmpeg],
            Parameters =
            [
                new ParameterSpec("start", T("Začátek", "Start"), "0:00", T("např. 0:30 nebo 1:02:03", "e.g. 0:30 or 1:02:03")),
                new ParameterSpec("end", T("Konec", "End"), "", T("prázdné = do konce", "empty = until the end")),
            ],
        };

    // ---------------------------------------------------------------- PDF

    private static void AddPdf(List<Operation> ops)
    {
        ops.Add(PdfDoc("docx", "Word"));
        foreach (var image in new[] { "jpg", "png" })
        {
            ops.Add(new PerFileOperation($"pdf.to.{image}", WheelKind.Convert, Formats.Display(image), Glyphs.Photo, Formats.Pdf,
                (item, ctx, p) => PdfBackend.RenderAsync(item, image, ctx, p), subtitle: T("všechny strany", "all pages")));
        }
        ops.Add(new PerFileOperation("pdf.to.pptx", WheelKind.Convert, "PPTX", Glyphs.Page, Formats.Pdf,
            async (item, ctx, _) => [await OfficeBackend.ConvertAsync(item, "pptx", ctx, "impress_pdf_import")], subtitle: "PowerPoint")
        { Requires = [ExternalTool.LibreOffice] });
        foreach (var text in new[] { "txt", "md" })
        {
            ops.Add(new PerFileOperation($"pdf.to.{text}", WheelKind.Convert, Formats.Display(text), Glyphs.Text, Formats.Pdf,
                async (item, ctx, _) => [await PdfBackend.ToTextAsync(item, text, ctx)], subtitle: text == "md" ? "Markdown" : "text"));
        }
        ops.Add(new PerFileOperation("pdf.to.xlsx", WheelKind.Convert, "XLSX", Glyphs.Table, Formats.Pdf,
            async (item, ctx, _) => [await PdfBackend.ToSpreadsheetAsync(item, ctx)], subtitle: T("tabulky", "tables")));
        ops.Add(PdfDoc("html", "web"));
        ops.Add(PdfDoc("epub", T("e-kniha", "e-book")));

        ops.Add(new BatchOperation("pdf.merge", WheelKind.Tools, T("Spojit PDF", "Merge PDFs"), Glyphs.Merge, BatchOperation.AllOf(Formats.Pdf, min: 2),
            async (items, ctx) =>
            {
                var sorted = items.Select(i => i.Path).OrderBy(p => p, NaturalComparer.Instance).ToList();
                return [await OutputPaths.ProduceAsync(ctx.OutputDirFor(sorted[0]), $"{Formats.BaseNameOf(sorted[0])} ({T("spojeno", "merged")})", "pdf",
                    tmp => Task.Run(() => PdfBackend.Merge(sorted, tmp), ctx.CancellationToken))];
            }));
        ops.Add(new PerFileOperation("pdf.split", WheelKind.Tools, T("Rozdělit", "Split"), Glyphs.Split, Formats.Pdf,
            (item, ctx, p) => PdfBackend.SplitAsync(item, ctx, p), subtitle: T("po stranách", "into pages")));
        ops.Add(new PerFileOperation("pdf.rotate", WheelKind.Tools, T("Otočit", "Rotate"), Glyphs.Rotate, Formats.Pdf,
            async (item, ctx, _) => [await OutputPaths.ProduceAsync(ctx.OutputDirFor(item.Path), $"{item.BaseName} ({T("otočeno", "rotated")})", "pdf",
                tmp => Task.Run(() => PdfBackend.Rotate(item.Path, tmp, 90), ctx.CancellationToken))], subtitle: T("doprava", "right")));
        ops.Add(new PerFileOperation("pdf.optimize", WheelKind.Tools, T("Optimalizovat", "Optimize"), Glyphs.Shrink, Formats.Pdf,
            async (item, ctx, _) => [await OutputPaths.ProduceAsync(ctx.OutputDirFor(item.Path), $"{item.BaseName} ({T("optimalizováno", "optimized")})", "pdf",
                tmp => Task.Run(() =>
                {
                    PdfBackend.Optimize(item.Path, tmp);
                    // A "smaller" copy that is bigger helps nobody.
                    if (new FileInfo(tmp).Length >= new FileInfo(item.Path).Length)
                        throw new ConversionException(T("PDF už je co nejmenší, menší verze nevznikla. Pro výrazné zmenšení zkuste převod na JPG a zpět, nebo Komprimovat u obrázků.",
                                                        "The PDF is already as small as it gets; no smaller copy was made."), nothingToDo: true);
                }, ctx.CancellationToken))]));
    }

    /// <summary>Needs pandoc, except PDF → Word, which Microsoft Word does on its own when it is installed.</summary>
    private static Operation PdfDoc(string target, string subtitle) =>
        new PerFileOperation($"pdf.to.{target}", WheelKind.Convert, Formats.Display(target), Glyphs.Document, Formats.Pdf,
            async (item, ctx, _) => [await PdfBackend.ToDocumentAsync(item, target, ctx)], subtitle: subtitle)
        {
            AvailableFor = (tools, _) => tools.Has(ExternalTool.Pandoc) || (target is "docx" or "odt" && PdfBackend.HasOfficeConverter),
        };

    // ---------------------------------------------------------------- documents

    private static void AddDocuments(List<Operation> ops)
    {
        foreach (var target in Formats.DocumentOutputs)
        {
            ops.Add(new PerFileOperation($"doc.to.{target}", WheelKind.Convert, Formats.Display(target), Glyphs.Document, Formats.Documents,
                async (item, ctx, _) => [await DocumentAsync(item, target, ctx)], targetFormat: target,
                subtitle: target switch { "docx" => "Word", "md" => "Markdown", "epub" => T("e-kniha", "e-book"), "txt" => "text", "html" => "web", _ => null })
            {
                AvailableFor = (tools, item) => DocumentRoute(item.Format, target, tools) is not null,
            });
        }
    }

    private enum DocRoute { Pandoc, Office, PandocThenOffice, OfficeThenPandoc, Browser, PandocThenBrowser }

    private static DocRoute? DocumentRoute(string input, string target, ToolLocator tools)
    {
        var pandoc = tools.Has(ExternalTool.Pandoc);
        var office = tools.Has(ExternalTool.LibreOffice);
        bool pandocReads = Formats.PandocInputs.Contains(input), officeReads = input is "doc" or "docx" or "odt" or "rtf" or "txt" or "html";
        bool pandocWrites = target != "pdf", officeWrites = target is "pdf" or "docx" or "odt" or "txt" or "html" || (target == "rtf" && input != "html");
        var browser = target == "pdf" && tools.Browser is not null;

        // A web page printed by the browser looks exactly as it does on screen; Markdown goes the same way through
        // pandoc's HTML (code blocks, tables and links come out as on GitHub, not as a Word document).
        if (browser && input == "html")
            return DocRoute.Browser;
        if (browser && pandoc && input == "md")
            return DocRoute.PandocThenBrowser;

        // HTML: pandoc embeds the pictures it links; LibreOffice would only link them (they vanish when the file moves).
        if (input == "html" && pandoc)
        {
            if (target != "pdf")
                return DocRoute.Pandoc;
            if (office)
                return DocRoute.PandocThenOffice;
        }
        // RTF: pandoc's RTF reader garbles styles and tables; LibreOffice reads it properly.
        if (input == "rtf" && office)
            return officeWrites ? DocRoute.Office : pandoc ? DocRoute.OfficeThenPandoc : null;
        // LibreOffice keeps layout better between word-processor formats; pandoc is better at everything else.
        if (office && officeReads && officeWrites && (target is "pdf" or "docx" or "odt" or "rtf" || !pandoc || !pandocReads))
            return DocRoute.Office;
        if (pandoc && pandocReads && pandocWrites)
            return DocRoute.Pandoc;
        if (pandoc && office && pandocReads && target == "pdf")
            return DocRoute.PandocThenOffice;
        if (pandoc && office && officeReads && pandocWrites)
            return DocRoute.OfficeThenPandoc;
        // Without LibreOffice the rest still reaches PDF through a web page.
        if (browser && pandoc && pandocReads)
            return DocRoute.PandocThenBrowser;
        return null;
    }

    private static async Task<string> DocumentAsync(FileItem item, string target, OperationContext ctx)
    {
        var route = DocumentRoute(item.Format, target, ctx.Tools) ?? throw new ConversionException(
            T($"Pro převod {Formats.Display(item.Format)} → {Formats.Display(target)} je potřeba Pandoc nebo LibreOffice.", $"Converting {Formats.Display(item.Format)} → {Formats.Display(target)} needs Pandoc or LibreOffice."));
        var dir = ctx.OutputDirFor(item.Path);
        switch (route)
        {
            case DocRoute.Office:
                return await OfficeBackend.ConvertAsync(item, target, ctx);
            case DocRoute.Pandoc:
                return await OutputPaths.ProduceAsync(dir, item.BaseName, target, tmp => PandocBackend.ConvertAsync(ctx, item.Path, item.Format, tmp, target));
            case DocRoute.Browser:
                return await OutputPaths.ProduceAsync(dir, item.BaseName, target, tmp => BrowserBackend.PrintToPdfAsync(ctx, item.Path, tmp));
            case DocRoute.PandocThenBrowser:
            {
                var temp = OutputPaths.TempDirectory();
                try
                {
                    var css = Path.Combine(temp, "print.css");
                    await File.WriteAllTextAsync(css, BrowserBackend.MarkdownCss, ctx.CancellationToken);
                    var html = Path.Combine(temp, item.BaseName + ".html");
                    await PandocBackend.ConvertAsync(ctx, item.Path, item.Format, html, "html", ["--css=" + css]);
                    return await OutputPaths.ProduceAsync(dir, item.BaseName, target, tmp => BrowserBackend.PrintToPdfAsync(ctx, html, tmp));
                }
                finally
                {
                    OutputPaths.TryDelete(temp);
                }
            }
            case DocRoute.PandocThenOffice:
            {
                var temp = OutputPaths.TempDirectory();
                try
                {
                    var docx = Path.Combine(temp, item.BaseName + ".docx");
                    await PandocBackend.ConvertAsync(ctx, item.Path, item.Format, docx, "docx");
                    return await OfficeBackend.ConvertAsync(new FileItem(docx), target, new OperationContextOverride(ctx, dir).Context);
                }
                finally
                {
                    OutputPaths.TryDelete(temp);
                }
            }
            default:
            {
                var docx = await OfficeBackend.ConvertToTempAsync(ctx, item.Path, "docx");
                try
                {
                    return await OutputPaths.ProduceAsync(dir, item.BaseName, target, tmp => PandocBackend.ConvertAsync(ctx, docx, "docx", tmp, target));
                }
                finally
                {
                    OutputPaths.TryDelete(Path.GetDirectoryName(docx)!);
                }
            }
        }
    }

    // ---------------------------------------------------------------- slides

    private static void AddSlides(List<Operation> ops)
    {
        foreach (var target in Formats.SlideOutputs)
        {
            ops.Add(new PerFileOperation($"slides.to.{target}", WheelKind.Convert, Formats.Display(target), target is "png" or "jpg" ? Glyphs.Photo : Glyphs.Page,
                Formats.Slides,
                async (item, ctx, p) =>
                {
                    if (target is "png" or "jpg")
                    {
                        var pdf = await OfficeBackend.ConvertToTempAsync(ctx, item.Path, "pdf");
                        try
                        {
                            // Render the temporary PDF but name the output after the presentation.
                            var renamed = Path.Combine(Path.GetDirectoryName(pdf)!, item.BaseName + ".pdf");
                            if (!string.Equals(renamed, pdf, StringComparison.Ordinal))
                                File.Move(pdf, renamed);
                            return await PdfBackend.RenderAsync(new FileItem(renamed), target, new OperationContextOverride(ctx, ctx.OutputDirFor(item.Path)).Context, p, T("snímky", "slides"));
                        }
                        finally
                        {
                            OutputPaths.TryDelete(Path.GetDirectoryName(pdf)!);
                        }
                    }
                    return [await OfficeBackend.ConvertAsync(item, target, ctx)];
                }, targetFormat: target, subtitle: target is "png" or "jpg" ? T("všechny snímky", "all slides") : null)
            { Requires = [ExternalTool.LibreOffice] });
        }
    }

    // ---------------------------------------------------------------- spreadsheets

    private static void AddSpreadsheets(List<Operation> ops)
    {
        var native = new HashSet<string>(["xlsx", "xlsm", "xls", "csv", "tsv"], StringComparer.OrdinalIgnoreCase);
        foreach (var target in Formats.SpreadsheetOutputs)
        {
            var nativeTarget = target is "xlsx" or "csv" or "tsv";
            ops.Add(new PerFileOperation($"sheet.to.{target}", WheelKind.Convert, Formats.Display(target), Glyphs.Table, Formats.Spreadsheets,
                async (item, ctx, _) => await SheetAsync(item, target, nativeTarget && native.Contains(item.Format), ctx),
                targetFormat: target, subtitle: target == "xlsx" ? "Excel" : null)
            {
                AvailableFor = (tools, item) => (nativeTarget && native.Contains(item.Format)) || tools.Has(ExternalTool.LibreOffice),
            });
        }
    }

    private static async Task<IEnumerable<string>> SheetAsync(FileItem item, string target, bool builtIn, OperationContext ctx)
    {
        if (builtIn)
            return await SpreadsheetBackend.ConvertAsync(item, target, ctx);
        var dir = ctx.OutputDirFor(item.Path);

        // CSV → ODS/PDF: LibreOffice would guess the separator and encoding (and split "1 234,50" into
        // columns), so the CSV is read here into a proper typed workbook first.
        if (item.Format is "csv" or "tsv")
        {
            var temp = OutputPaths.TempDirectory();
            try
            {
                var xlsx = Path.Combine(temp, item.BaseName + ".xlsx");
                var sheets = await Task.Run(() => SpreadsheetBackend.Read(item.Path), ctx.CancellationToken);
                await Task.Run(() => SpreadsheetBackend.WriteXlsx(sheets, xlsx), ctx.CancellationToken);
                return [await OfficeBackend.ConvertAsync(new FileItem(xlsx), target, new OperationContextOverride(ctx, dir).Context)];
            }
            finally
            {
                OutputPaths.TryDelete(temp);
            }
        }

        // ODS → CSV/TSV: every sheet, in the local CSV dialect (LibreOffice alone writes only the first sheet).
        if (target is "csv" or "tsv")
        {
            var xlsx = await OfficeBackend.ConvertToTempAsync(ctx, item.Path, "xlsx");
            try
            {
                var sheets = await Task.Run(() => SpreadsheetBackend.Read(xlsx), ctx.CancellationToken);
                return await SpreadsheetBackend.WriteDelimitedAsync(sheets, item.BaseName, dir, target, ctx.CancellationToken);
            }
            finally
            {
                OutputPaths.TryDelete(Path.GetDirectoryName(xlsx)!);
            }
        }
        return [await OfficeBackend.ConvertAsync(item, target, ctx)];
    }

    // ---------------------------------------------------------------- subtitles

    private static void AddSubtitles(List<Operation> ops)
    {
        foreach (var target in Formats.SubtitleOutputs)
        {
            ops.Add(new PerFileOperation($"subs.to.{target}", WheelKind.Convert, Formats.Display(target), Glyphs.Subtitles, Formats.Subtitles,
                async (item, ctx, _) => [await SubtitleBackend.ConvertAsync(item, target, ctx)], targetFormat: target,
                subtitle: target == "txt" ? T("jen text", "text only") : null));
        }
    }

    // ---------------------------------------------------------------- archives

    private static void AddArchives(List<Operation> ops)
    {
        ops.Add(new PerFileOperation("archive.extract", WheelKind.Convert, T("Rozbalit", "Extract"), Glyphs.Extract, Formats.Archives,
            async (item, ctx, p) => [await ArchiveBackend.ExtractAsync(item.Path, ctx.OutputDirFor(item.Path), ctx.CancellationToken, p)]));
        foreach (var target in Formats.ArchiveOutputs)
        {
            ops.Add(new PerFileOperation($"archive.to.{target}", WheelKind.Convert, Formats.Display(target), Glyphs.Pack, Formats.Archives,
                async (item, ctx, _) => [await ArchiveBackend.RepackAsync(item, target, ctx)], targetFormat: target));
        }

        foreach (var (target, label) in new[] { ("zip", "ZIP"), ("tar.gz", "TAR.GZ") })
        {
            ops.Add(new BatchOperation($"pack.{target}", WheelKind.Tools, T("Zabalit", "Archive"), Glyphs.Pack, _ => true,
                async (items, ctx) => [await OutputPaths.ProduceAsync(ctx.OutputDirFor(items[0].Path), ArchiveBackend.ArchiveNameFor(items), target,
                    tmp => ArchiveBackend.PackAsync(items.Select(i => i.Path).ToList(), tmp, target, ctx.CancellationToken, ctx.Progress))],
                subtitle: label));
        }
    }
}

internal static class Helpers
{
    public static Task<string> Produce(FileItem item, OperationContext ctx, string format, Func<string, Task> produce) =>
        OutputPaths.ProduceAsync(ctx.OutputDirFor(item.Path), item.BaseName, format, produce);
}

/// <summary>Copy of a context that writes into a fixed folder (for multi-step conversions via temp files).</summary>
internal sealed class OperationContextOverride(OperationContext source, string outputDirectory)
{
    public OperationContext Context { get; } = new()
    {
        Tools = source.Tools,
        Progress = source.Progress,
        CancellationToken = source.CancellationToken,
        OutputDirectory = outputDirectory,
        Parameters = source.Parameters,
        Settings = source.Settings,
    };
}

/// <summary>Sorts "page 2" before "page 10".</summary>
public sealed class NaturalComparer : IComparer<string>
{
    public static readonly NaturalComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x is null || y is null)
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var si = i;
                var sj = j;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                while (j < y.Length && char.IsDigit(y[j])) j++;
                var a = x[si..i].TrimStart('0');
                var b = y[sj..j].TrimStart('0');
                var c = a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
                if (c != 0)
                    return c;
            }
            else
            {
                var c = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[j]));
                if (c != 0)
                    return c;
                i++;
                j++;
            }
        }
        return (x.Length - i).CompareTo(y.Length - j);
    }
}
