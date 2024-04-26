using System;
using System.IO;
using NetTopologySuite.Geometries;
using SkiaSharp;
using ZMap.Extensions;
using ZMap.Infrastructure;
using ZMap.Renderer.SkiaSharp.Utilities;
using ZMap.Style;

namespace ZMap.Renderer.SkiaSharp;

public class SymbolRenderer(SymbolStyle style) : SkiaRenderer, ISymbolRenderer<SKCanvas>
{
    private static readonly SKBitmap DefaultImage;

    static SymbolRenderer()
    {
        DefaultImage = SKBitmap.Decode("108.png");
    }

    public override void Render(SKCanvas graphics, Geometry geometry, Envelope extent, int width, int height)
    {
        //   *---    top     ---*
        //   |                  |
        //   left   center    right 
        //   |                  |
        //   *---   bottom   ---*

        var interiorPoint = geometry.InteriorPoint;
        var centroid = new Coordinate(interiorPoint.X, interiorPoint.Y);

        if (!extent.Contains(centroid))
        {
            return;
        }

        var half = (style.Size.Value ?? 14) / 2;

        var centroidPoint = CoordinateTransformUtility.WordToExtent(extent,
            width, height, centroid);

        var left = centroidPoint.X - half;
        var top = centroidPoint.Y - half;
        var right = centroidPoint.X + half;
        var bottom = centroidPoint.Y + half;

        // comment: 通过前端 gutter/buffer 计算来处理边界问题

        // if (left < 0)
        // {
        //     right += Math.Abs(left);
        //     left = 0;
        // }
        //
        // if (top < 0)
        // {
        //     bottom += Math.Abs(top);
        //     top = 0;
        // }
        //
        // if (right > width)
        // {
        //     left -= right - width;
        //     right = width;
        // }
        //
        // if (bottom > height)
        // {
        //     top -= bottom - height;
        //     bottom = height;
        // }

        var rect = new SKRect(left, top, right, bottom);

        var image = GetImage();
        graphics.DrawBitmap(image, rect, new SKPaint());
    }

    private SKBitmap GetImage()
    {
        SKBitmap image;
        var uri = style.Uri.Value;
        if (string.IsNullOrEmpty(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out var u))
        {
            image = DefaultImage;
        }
        else
        {
            image = Cache.GetOrCreate($"SSI_{style.Uri.Value}", _ =>
            {
                switch (u.Scheme)
                {
                    case "file":
                    {
                        var path = u.ToPath();
                        return File.Exists(path) ? SKBitmap.Decode(path) : DefaultImage;
                    }
                    default:
                    {
                        return DefaultImage;
                    }
                }
            });
        }

        return image;
    }

    protected override SKPaint CreatePaint()
    {
        return null;
    }
}