using System.Globalization;
using System.Text;

namespace FileAction.Core.Backends;

/// <summary>
/// Turns a picture with a limited palette into vector shapes: real SVG paths, not a bitmap wrapped in an SVG.
/// Layers are stacked (each also covers the later colours it touches), so no seams show between neighbours.
/// </summary>
public static class ImageTracer
{
    /// <summary>Colours used by less than this share of the picture are edge pixels and go to the nearest colour.</summary>
    private const double MinColorShare = 0.0015;
    private const int SimilarColors = 14 * 14;
    private const int PhotoColors = 20;
    private const double Tolerance = 0.7;
    /// <summary>A turn sharper than this (in degrees) stays a corner; gentler ones become curves.</summary>
    private const double CornerAngle = 70;

    /// <param name="indexed">Palette-reduced pixels as RGBA, row by row.</param>
    /// <param name="outWidth">Size written on the SVG (the original picture's); the shapes keep the working size.</param>
    public static string Trace(byte[] indexed, int width, int height, int outWidth, int outHeight, CancellationToken ct = default)
    {
        var (labels, palette) = ReducePalette(indexed, width, height);
        // Photos fall apart into countless grains; in a drawing the same small areas are parts of letters.
        var speck = Math.Max(4, width * height / (palette.Count >= PhotoColors ? 30_000 : 400_000));
        RemoveSpecks(labels, width, height, palette.Count, speck, ct);

        // Largest colour first: it is usually the background, and the smaller shapes go on top of it.
        var counts = new long[palette.Count];
        foreach (var label in labels)
        {
            if (label >= 0)
                counts[label]++;
        }
        var order = Enumerable.Range(0, palette.Count).Where(i => counts[i] > 0).OrderByDescending(i => counts[i]).ToList();
        var rank = new int[palette.Count];
        for (var i = 0; i < order.Count; i++)
            rank[order[i]] = i;
        var ranks = new int[labels.Length];
        for (var i = 0; i < labels.Length; i++)
            ranks[i] = labels[i] < 0 ? -1 : rank[labels[i]];

        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{outWidth}\" height=\"{outHeight}\" viewBox=\"0 0 {width} {height}\">\n");
        var opaque = Array.TrueForAll(labels, l => l >= 0);
        for (var layer = 0; layer < order.Count; layer++)
        {
            ct.ThrowIfCancellationRequested();
            var color = Hex(palette[order[layer]]);
            if (layer == 0 && opaque)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<rect width=\"{width}\" height=\"{height}\" fill=\"{color}\"/>\n");
                continue;
            }
            var path = LayerPath(ranks, width, height, layer, speck);
            if (path.Length > 0)
                svg.Append("<path fill=\"").Append(color).Append("\" fill-rule=\"evenodd\" d=\"").Append(path).Append("\"/>\n");
        }
        svg.Append("</svg>\n");
        return svg.ToString();
    }

    private static string Hex(int rgb) => "#" + rgb.ToString("x6", CultureInfo.InvariantCulture);

    private static int Distance(int a, int b)
    {
        int dr = (a >> 16) - (b >> 16), dg = ((a >> 8) & 255) - ((b >> 8) & 255), db = (a & 255) - (b & 255);
        return dr * dr + dg * dg + db * db;
    }

    /// <summary>
    /// Pixels as palette labels (-1 = transparent). Anti-aliasing leaves rare in-between colours along every edge;
    /// near-duplicates join the nearest real colour, the in-between ones are settled by what lies next to them
    /// (see <see cref="SettleEdges"/>). That keeps logos crisp and files small.
    /// </summary>
    private static (int[] Labels, List<int> Palette) ReducePalette(byte[] rgba, int width, int height)
    {
        var counts = new Dictionary<int, int>();
        var total = 0;
        for (var i = 0; i < width * height; i++)
        {
            if (rgba[i * 4 + 3] < 128)
                continue;
            var rgb = (rgba[i * 4] << 16) | (rgba[i * 4 + 1] << 8) | rgba[i * 4 + 2];
            counts[rgb] = counts.GetValueOrDefault(rgb) + 1;
            total++;
        }
        var kept = new List<int>();
        foreach (var (rgb, count) in counts.OrderByDescending(c => c.Value))
        {
            if (kept.Count == 0 || (count >= total * MinColorShare && kept.All(k => Distance(k, rgb) > SimilarColors)))
                kept.Add(rgb);
        }
        DropEdgeColors(kept, rgba, width, height);
        var nearest = counts.Keys.ToDictionary(rgb => rgb, rgb =>
        {
            var best = 0;
            for (var k = 1; k < kept.Count; k++)
            {
                if (Distance(kept[k], rgb) < Distance(kept[best], rgb))
                    best = k;
            }
            return best;
        });
        int Rgb(int i) => (rgba[i * 4] << 16) | (rgba[i * 4 + 1] << 8) | rgba[i * 4 + 2];
        var labels = new int[width * height];
        var open = new List<int>();
        for (var i = 0; i < labels.Length; i++)
        {
            if (rgba[i * 4 + 3] < 128)
            {
                labels[i] = -1;
                continue;
            }
            var rgb = Rgb(i);
            labels[i] = nearest[rgb];
            if (Distance(kept[labels[i]], rgb) > SimilarColors)
                open.Add(i);
        }
        SettleEdges(labels, open, width, height, kept, nearest, Rgb);
        return (labels, kept);
    }

    /// <summary>
    /// A pixel between colours takes the closest colour actually next to it. Going by the palette alone, the grey edge
    /// of black text on white would turn green if the picture has some green elsewhere.
    /// </summary>
    private static void SettleEdges(int[] labels, List<int> open, int width, int height, List<int> palette, Dictionary<int, int> nearest, Func<int, int> rgb)
    {
        const int Unsettled = -2;
        foreach (var i in open)
            labels[i] = Unsettled;
        // Thin bands settle within a few rounds; what is left is a colour of its own too rare to keep.
        for (var round = 0; round < 32 && open.Count > 0; round++)
        {
            var settled = new List<(int Pixel, int Label)>();
            var rest = new List<int>();
            foreach (var i in open)
            {
                int x = i % width, y = i / width, color = rgb(i), best = -1, bestDistance = int.MaxValue;
                void Look(int q)
                {
                    if (labels[q] < 0)
                        return;
                    var d = Distance(palette[labels[q]], color);
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = labels[q];
                    }
                }
                if (x > 0) Look(i - 1);
                if (x < width - 1) Look(i + 1);
                if (y > 0) Look(i - width);
                if (y < height - 1) Look(i + width);
                if (best >= 0)
                    settled.Add((i, best));
                else
                    rest.Add(i);
            }
            if (settled.Count == 0)
                break;
            foreach (var (pixel, label) in settled)
                labels[pixel] = label;
            open = rest;
        }
        foreach (var i in open)
            labels[i] = nearest[rgb(i)];
    }

    /// <summary>
    /// Anti-aliasing of a long edge can cover enough pixels to pass as a colour. It gives itself away twice: it forms
    /// only thin bands, and it lies between two other colours. A thin black line in a drawing stays: nothing is
    /// on either side of black.
    /// </summary>
    private static void DropEdgeColors(List<int> kept, byte[] rgba, int width, int height)
    {
        if (kept.Count < 3)
            return;
        int Nearest(int offset)
        {
            var rgb = (rgba[offset] << 16) | (rgba[offset + 1] << 8) | rgba[offset + 2];
            var best = 0;
            for (var k = 1; k < kept.Count; k++)
            {
                if (Distance(kept[k], rgb) < Distance(kept[best], rgb))
                    best = k;
            }
            return best;
        }
        var labels = new int[width * height];
        for (var i = 0; i < labels.Length; i++)
            labels[i] = rgba[i * 4 + 3] < 128 ? -1 : Nearest(i * 4);
        var total = new long[kept.Count];
        var core = new long[kept.Count];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var label = labels[y * width + x];
                if (label < 0)
                    continue;
                total[label]++;
                if (x > 0 && x < width - 1 && y > 0 && y < height - 1 && labels[y * width + x - 1] == label && labels[y * width + x + 1] == label
                    && labels[(y - 1) * width + x] == label && labels[(y + 1) * width + x] == label)
                    core[label]++;
            }
        }
        var solid = Enumerable.Range(0, kept.Count).Where(k => core[k] * 2 >= total[k]).Select(k => kept[k]).ToList();
        for (var k = kept.Count - 1; k >= 0 && kept.Count > 1; k--)
        {
            if (core[k] * 2 < total[k] && Between(kept[k], solid))
                kept.RemoveAt(k);
        }
    }

    /// <summary>Whether a colour lies on the straight blend of two of the given colours.</summary>
    private static bool Between(int color, List<int> colors)
    {
        static (double R, double G, double B) Rgb(int c) => (c >> 16, (c >> 8) & 255, c & 255);
        var p = Rgb(color);
        for (var i = 0; i < colors.Count; i++)
        {
            for (var j = i + 1; j < colors.Count; j++)
            {
                var a = Rgb(colors[i]);
                var b = Rgb(colors[j]);
                (double R, double G, double B) d = (b.R - a.R, b.G - a.G, b.B - a.B);
                var t = ((p.R - a.R) * d.R + (p.G - a.G) * d.G + (p.B - a.B) * d.B) / Math.Max(1, d.R * d.R + d.G * d.G + d.B * d.B);
                if (t <= 0 || t >= 1)
                    continue;
                double er = a.R + d.R * t - p.R, eg = a.G + d.G * t - p.G, eb = a.B + d.B * t - p.B;
                if (er * er + eg * eg + eb * eb < 24 * 24)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Tiny islands (noise, JPEG artefacts) take the colour most common around them.</summary>
    private static void RemoveSpecks(int[] labels, int width, int height, int colors, int maxArea, CancellationToken ct)
    {
        var seen = new bool[labels.Length];
        var stack = new Stack<int>();
        var region = new List<int>();
        var around = new int[colors];
        for (var start = 0; start < labels.Length; start++)
        {
            if (seen[start] || labels[start] < 0)
                continue;
            if (start % 65536 == 0)
                ct.ThrowIfCancellationRequested();
            var label = labels[start];
            region.Clear();
            Array.Clear(around);
            stack.Push(start);
            seen[start] = true;
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                region.Add(p);
                int x = p % width, y = p / width;
                void Visit(int q)
                {
                    if (labels[q] == label)
                    {
                        if (!seen[q])
                        {
                            seen[q] = true;
                            stack.Push(q);
                        }
                    }
                    else if (labels[q] >= 0)
                    {
                        around[labels[q]]++;
                    }
                }
                if (x > 0) Visit(p - 1);
                if (x < width - 1) Visit(p + 1);
                if (y > 0) Visit(p - width);
                if (y < height - 1) Visit(p + width);
            }
            if (region.Count > maxArea)
                continue;
            var best = Array.IndexOf(around, around.Max());
            if (around[best] == 0)
                continue;
            foreach (var p in region)
                labels[p] = best;
        }
    }

    private const int East = 0, South = 1, West = 2, North = 3;
    private static readonly int[] Dx = [1, 0, -1, 0];
    private static readonly int[] Dy = [0, 1, 0, -1];

    /// <summary>The outline of everything at or above <paramref name="layer"/>, holes included.</summary>
    private static string LayerPath(int[] ranks, int width, int height, int layer, int minArea)
    {
        // Later colours are included only where they touch this one (to leave no seam under them); the shapes
        // that stand apart are drawn once, in their own layer.
        var inside = new bool[ranks.Length];
        var stack = new Stack<int>();
        for (var i = 0; i < ranks.Length; i++)
        {
            if (ranks[i] != layer)
                continue;
            inside[i] = true;
            stack.Push(i);
        }
        while (stack.Count > 0)
        {
            var p = stack.Pop();
            int px = p % width, py = p / width;
            void Spread(int q)
            {
                if (!inside[q] && ranks[q] > layer)
                {
                    inside[q] = true;
                    stack.Push(q);
                }
            }
            if (px > 0) Spread(p - 1);
            if (px < width - 1) Spread(p + 1);
            if (py > 0) Spread(p - width);
            if (py < height - 1) Spread(p + width);
        }

        // Each boundary between inside and outside is a directed unit edge with the inside on its right.
        var stride = width + 1;
        var edges = new byte[stride * (height + 1)];
        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && inside[y * width + x];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!Inside(x, y))
                    continue;
                if (!Inside(x, y - 1)) edges[y * stride + x] |= 1 << East;
                if (!Inside(x + 1, y)) edges[y * stride + x + 1] |= 1 << South;
                if (!Inside(x, y + 1)) edges[(y + 1) * stride + x + 1] |= 1 << West;
                if (!Inside(x - 1, y)) edges[(y + 1) * stride + x] |= 1 << North;
            }
        }

        var path = new StringBuilder();
        var midpoints = new List<(double X, double Y)>();
        for (var start = 0; start < edges.Length; start++)
        {
            while ((edges[start] & (1 << East)) != 0)
            {
                midpoints.Clear();
                int x = start % stride, y = start / stride, direction = East, doubled = 0;
                do
                {
                    edges[y * stride + x] &= (byte)~(1 << direction);
                    int nx = x + Dx[direction], ny = y + Dy[direction];
                    // The middle of each unit edge: stair steps of a slanted line fall onto a straight line.
                    midpoints.Add(((x + nx) / 2.0, (y + ny) / 2.0));
                    doubled += x * ny - nx * y;
                    x = nx;
                    y = ny;
                    if (y * stride + x == start)
                        break;
                    // Where two shapes touch at a corner, turning right stays on the same shape.
                    var outgoing = edges[y * stride + x];
                    foreach (var turn in new[] { (direction + 1) & 3, direction, (direction + 3) & 3 })
                    {
                        if ((outgoing & (1 << turn)) != 0)
                        {
                            direction = turn;
                            break;
                        }
                    }
                }
                while (true);
                if (Math.Abs(doubled) / 2 >= minArea)
                    AppendCurve(path, Simplify(midpoints));
            }
        }
        return path.ToString();
    }

    /// <summary>Douglas-Peucker for a closed loop, anchored at two far-apart points.</summary>
    private static List<(double X, double Y)> Simplify(List<(double X, double Y)> loop)
    {
        var far = 0;
        var farthest = -1.0;
        for (var i = 1; i < loop.Count; i++)
        {
            var d = Math.Pow(loop[i].X - loop[0].X, 2) + Math.Pow(loop[i].Y - loop[0].Y, 2);
            if (d > farthest)
            {
                farthest = d;
                far = i;
            }
        }
        var keep = new bool[loop.Count];
        keep[0] = keep[far] = true;
        var stack = new Stack<(int From, int To)>();
        stack.Push((0, far));
        stack.Push((far, loop.Count));
        while (stack.Count > 0)
        {
            var (from, to) = stack.Pop();
            if (to - from < 2)
                continue;
            var a = loop[from];
            var b = loop[to % loop.Count];
            double dx = b.X - a.X, dy = b.Y - a.Y, length = Math.Sqrt(dx * dx + dy * dy);
            var worst = -1;
            var worstDistance = Tolerance;
            for (var i = from + 1; i < to; i++)
            {
                var distance = length < 1e-9
                    ? Math.Sqrt(Math.Pow(loop[i].X - a.X, 2) + Math.Pow(loop[i].Y - a.Y, 2))
                    : Math.Abs(dy * (loop[i].X - a.X) - dx * (loop[i].Y - a.Y)) / length;
                if (distance > worstDistance)
                {
                    worstDistance = distance;
                    worst = i;
                }
            }
            if (worst < 0)
                continue;
            keep[worst] = true;
            stack.Push((from, worst));
            stack.Push((worst, to));
        }
        var result = new List<(double X, double Y)>();
        for (var i = 0; i < loop.Count; i++)
        {
            if (keep[i])
                result.Add(loop[i]);
        }
        return result;
    }

    /// <summary>A closed path through the points: straight between corners, smooth Bézier curves elsewhere.</summary>
    private static void AppendCurve(StringBuilder path, List<(double X, double Y)> p)
    {
        if (p.Count < 3)
            return;
        var n = p.Count;
        var tangents = new (double X, double Y)[n];
        for (var i = 0; i < n; i++)
        {
            var before = p[(i + n - 1) % n];
            var after = p[(i + 1) % n];
            double ax = p[i].X - before.X, ay = p[i].Y - before.Y, bx = after.X - p[i].X, by = after.Y - p[i].Y;
            var turn = Math.Abs(Math.Atan2(ax * by - ay * bx, ax * bx + ay * by)) * 180 / Math.PI;
            if (turn > CornerAngle)
                continue;
            // Longer sides weigh more: where a straight run meets a curve, the run stays straight.
            double al = Math.Sqrt(ax * ax + ay * ay), bl = Math.Sqrt(bx * bx + by * by);
            double tx = ax * al + bx * bl, ty = ay * al + by * bl;
            var length = Math.Sqrt(tx * tx + ty * ty);
            if (length > 1e-9)
                tangents[i] = (tx / length, ty / length);
        }
        path.Append('M').Append(N(p[0].X)).Append(' ').Append(N(p[0].Y));
        for (var i = 0; i < n; i++)
        {
            var next = (i + 1) % n;
            var a = p[i];
            var b = p[next];
            var third = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2)) / 3;
            if (tangents[i] == default && tangents[next] == default)
            {
                if (next != 0)
                    path.Append('L').Append(N(b.X)).Append(' ').Append(N(b.Y));
                continue;
            }
            path.Append('C').Append(N(a.X + tangents[i].X * third)).Append(' ').Append(N(a.Y + tangents[i].Y * third))
                .Append(' ').Append(N(b.X - tangents[next].X * third)).Append(' ').Append(N(b.Y - tangents[next].Y * third))
                .Append(' ').Append(N(b.X)).Append(' ').Append(N(b.Y));
        }
        path.Append('Z');
    }

    private static string N(double value) => Math.Round(value, 1).ToString("0.#", CultureInfo.InvariantCulture);
}
