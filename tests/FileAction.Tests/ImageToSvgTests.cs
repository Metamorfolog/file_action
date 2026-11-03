using System.Xml.Linq;
using FileAction.Core;
using FileAction.Core.Operations;
using ImageMagick;
using ImageMagick.Drawing;

namespace FileAction.Tests;

public class ImageToSvgTests : IDisposable
{
    private readonly string _dir = OutputPaths.TempDirectory();

    public void Dispose() => OutputPaths.TryDelete(_dir);

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private async Task<(string Path, XElement Root)> Convert(string input)
    {
        var op = Catalog.Build().Single(o => o.Id == "image.to.svg");
        Assert.True(op.AppliesTo([new FileItem(input)]));
        var ctx = new OperationContext { Tools = new ToolLocator(), OutputDirectory = Path.Combine(_dir, "out") };
        var output = Assert.Single((await op.ExecuteAsync([new FileItem(input)], ctx)).Outputs);
        return (output, XElement.Load(output));
    }

    /// <summary>A white logo sheet: a blue disc with a hole, and a red square.</summary>
    private string Logo(int size = 400, bool transparent = false)
    {
        var path = Path.Combine(_dir, $"logo{size}{(transparent ? "t" : "")}.png");
        using var image = new MagickImage(transparent ? MagickColors.Transparent : MagickColors.White, (uint)size, (uint)size);
        var s = size / 400.0;
        new Drawables()
            .FillColor(new MagickColor("#1040c0")).Circle(130 * s, 130 * s, 130 * s, 40 * s)
            .FillColor(MagickColors.White).Circle(130 * s, 130 * s, 130 * s, 90 * s)
            .FillColor(new MagickColor("#d02020")).Rectangle(240 * s, 240 * s, 360 * s, 360 * s)
            .Draw(image);
        image.Write(path);
        return path;
    }

    private static double Difference(string a, string b)
    {
        using var left = new MagickImage(a);
        var settings = new MagickReadSettings { Format = MagickFormat.Svg, BackgroundColor = MagickColors.White };
        using var right = new MagickImage(b, settings);
        left.BackgroundColor = MagickColors.White;
        left.Alpha(AlphaAction.Remove);
        right.Alpha(AlphaAction.Remove);
        right.Resize(new MagickGeometry(left.Width, left.Height) { IgnoreAspectRatio = true });
        return left.Compare(right, ErrorMetric.RootMeanSquared);
    }

    [Fact]
    public async Task A_logo_becomes_real_shapes_in_its_own_colours()
    {
        var png = Logo();
        var (svg, root) = await Convert(png);
        Assert.Equal("logo400.svg", Path.GetFileName(svg));
        Assert.Equal("400", root.Attribute("width")?.Value);
        Assert.Empty(root.Descendants(Svg + "image"));
        var fills = root.Elements().Select(e => e.Attribute("fill")?.Value).ToList();
        Assert.Equal(3, fills.Count);
        Assert.Equal("#ffffff", fills[0]);
        Assert.Contains(fills, f => f is not null && f.StartsWith("#1") && f.EndsWith("c0"));
        Assert.Contains(fills, f => f is not null && f.StartsWith("#d0"));
        // Curves on the disc, and small: a bitmap would weigh far more.
        Assert.Contains(root.Elements(Svg + "path"), p => p.Attribute("d")!.Value.Contains('C'));
        Assert.True(new FileInfo(svg).Length < 12_000, $"{new FileInfo(svg).Length} bytes");
        Assert.True(Difference(png, svg) < 0.04, $"difference {Difference(png, svg)}");
    }

    [Fact]
    public async Task Edges_of_black_shapes_do_not_take_a_colour_from_elsewhere_in_the_picture()
    {
        var path = Path.Combine(_dir, "text.png");
        using (var image = new MagickImage(MagickColors.White, 500, 300))
        {
            new Drawables()
                .FillColor(new MagickColor("#118833")).Rectangle(380, 20, 480, 120)
                // Black shapes with grey anti-aliased edges; no text, the build servers may have no font for it.
                .FillColor(MagickColors.Black).Circle(110, 170, 110, 90)
                .Polygon(new PointD(230, 250), new PointD(300, 60), new PointD(340, 250))
                .Draw(image);
            image.Write(path);
        }
        var (svg, root) = await Convert(path);
        var green = root.Elements(Svg + "path").Single(p => p.Attribute("fill")!.Value.StartsWith("#11"));
        // Only the square: four sides, not the outlines of the black shapes as well.
        Assert.True(green.Attribute("d")!.Value.Length < 200, green.Attribute("d")!.Value);
        Assert.True(Difference(path, svg) < 0.06, $"difference {Difference(path, svg)}");
    }

    [Fact]
    public async Task Small_pictures_stay_their_size_on_the_outside()
    {
        var (svg, root) = await Convert(Logo(64));
        Assert.Equal("64", root.Attribute("height")?.Value);
        Assert.Equal("0 0 512 512", root.Attribute("viewBox")?.Value);
    }

    [Fact]
    public async Task Transparency_stays_transparent()
    {
        var (_, root) = await Convert(Logo(transparent: true));
        Assert.Empty(root.Elements(Svg + "rect"));
        Assert.All(root.Elements(Svg + "path"), p => Assert.Equal("evenodd", p.Attribute("fill-rule")?.Value));
    }

    [Fact]
    public async Task A_noisy_picture_still_converts_and_svg_is_not_offered_for_svg()
    {
        var path = Path.Combine(_dir, "plasma.jpg");
        using (var image = new MagickImage("plasma:red-blue", 300, 200))
            image.Write(path);
        var (svg, root) = await Convert(path);
        Assert.NotEmpty(root.Elements(Svg + "path"));
        Assert.False(Catalog.Build().Single(o => o.Id == "image.to.svg").AppliesTo([new FileItem(svg)]));
    }
}
