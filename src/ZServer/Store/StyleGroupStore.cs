using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ZMap;
using ZMap.Style;
using ZServer.Extensions;

namespace ZServer.Store;

public class StyleGroupStore : IStyleGroupStore
{
    private static Dictionary<string, StyleGroup> _cache = new();

    public Task Refresh(List<JObject> configurations)
    {
        var dict = new Dictionary<string, StyleGroup>();
        foreach (var configuration in configurations)
        {
            var sections = configuration.SelectToken("styleGroups");
            if (sections == null)
            {
                continue;
            }

            foreach (var section in sections.Children<JProperty>())
            {
                var obj = section.Value as JObject;
                if (obj == null)
                {
                    continue;
                }

                var descriptionToken = obj["description"];
                var minZoomToken = obj["minZoom"];
                var maxZoomToken = obj["maxZoom"];
                var zoomUnitValue = obj["zoomUnit"]?.ToString();
                var filter = section.GetFilterExpression();
                var zoomUnit = Enum.TryParse(zoomUnitValue, out ZoomUnits v) ? v : ZoomUnits.Scale;
                var stylesToken = obj.SelectToken("styles") as JArray;
                var styleGroup = new StyleGroup
                {
                    Name = section.Name,
                    Filter = filter,
                    Description = descriptionToken?.Value<string>(),
                    MinZoom = minZoomToken?.Value<float>() ?? 0,
                    MaxZoom = maxZoomToken?.Value<float>() ?? 20,
                    ZoomUnit = zoomUnit,
                    Styles = GetStyles(stylesToken)
                };

                dict.TryAdd(section.Name, styleGroup);
            }
        }

        _cache = dict;

        return Task.CompletedTask;
    }

    public async Task<StyleGroup> FindAsync(string name)
    {
        if (_cache.TryGetValue(name, out var styleGroup))
        {
            return await Task.FromResult(styleGroup);
        }

        return null;
    }

    public Task<List<StyleGroup>> GetAllAsync()
    {
        var items = _cache.Values.Select(x => x.Clone()).ToList();
        return Task.FromResult(items);
    }

    private List<Style> GetStyles(JArray section)
    {
        var styles = new List<Style>();
        if (section == null)
        {
            return styles;
        }

        foreach (var token in section)
        {
            if (token is not JObject styleSection)
            {
                continue;
            }

            var style = styleSection["type"]?.ToObject<string>();
            var result = style switch
            {
                "fill" => GetFillStyle(styleSection),
                "line" => GetLineStyle(styleSection),
                "spriteLine" => GetSpriteLineStyle(styleSection),
                "spriteFill" => GetSpriteFillStyle(styleSection),
                "text" => GetTextStyle(styleSection),
                "symbol" => GetSymbolStyle(styleSection),
                "Raster" => GetRasterStyle(styleSection),
                _ => null
            };

            if (result == null)
            {
                continue;
            }

            var minZoomToken = styleSection["minZoom"];
            var maxZoomToken = styleSection["maxZoom"];
            var zoomUnitValue = styleSection["zoomUnit"]?.ToString();

            result.Filter = styleSection.GetFilterExpression();
            result.MinZoom = minZoomToken?.Value<float>() ?? 0;
            result.MaxZoom = maxZoomToken?.Value<float>() ?? 0;
            result.ZoomUnit = Enum.TryParse(zoomUnitValue, out ZoomUnits zoomUnit) ? zoomUnit : ZoomUnits.Scale;
            styles.Add(result);
        }

        return styles;
    }

    private Style GetSymbolStyle(JObject section)
    {
        return new SymbolStyle
        {
            Size = section.GetExpression<int?>("size"),
            Uri = section.GetExpression<string>("uri")
        };
    }

    private Style GetRasterStyle(JObject section)
    {
        return new RasterStyle
        {
            Opacity = section.GetExpression<float?>("opacity"),
            HueRotate = section.GetExpression<float?>("hueRotate"),
            BrightnessMin = section.GetExpression<float?>("brightnessMin"),
            BrightnessMax = section.GetExpression<float?>("brightnessMax"),
            Saturation = section.GetExpression<float?>("saturation"),
            Contrast = section.GetExpression<float?>("contrast")
        };
    }

    private Style GetLineStyle(JObject section)
    {
        return new LineStyle
        {
            Opacity = section.GetExpression<float?>("opacity"),
            Width = section.GetExpression<int?>("width"),
            Color = section.GetExpression<string>("color"),
            DashArray = section.GetExpression<float[]>("dashArray"),
            DashOffset = section.GetExpression<float?>("dashOffset"),
            LineJoin = section.GetExpression<string>("lineJoin"),
            LineCap = section.GetExpression<string>("lineCap"),
            Translate = section.GetExpression<double[]>("translate"),
            TranslateAnchor = section.GetExpression<TranslateAnchor>("translateAnchor"),
            GapWidth = section.GetExpression<int?>("gapWidth"),
            Offset = section.GetExpression<int?>("offset"),
            Blur = section.GetExpression<int?>("blur"),
            Gradient = section.GetExpression<int?>("gradient"),
            Pattern = section.GetExpression<string>("pattern")
        };
    }

    private Style GetSpriteFillStyle(JObject section)
    {
        return new SpriteFillStyle
        {
            Opacity = section.GetExpression<float?>("opacity"),
            Color = section.GetExpression<string>("color"),
            Translate = section.GetExpression<double[]>("translate"),
            TranslateAnchor = section.GetExpression<TranslateAnchor?>("translateAnchor"),
            Pattern = section.GetExpression<string>("pattern"),
            Uri = section.GetExpression<string>("uri")
        };
    }

    private Style GetSpriteLineStyle(JObject section)
    {
        return new SpriteLineStyle
        {
            Opacity = section.GetExpression<float?>("opacity"),
            Width = section.GetExpression<int?>("width"),
            Color = section.GetExpression<string>("color"),
            DashArray = section.GetExpression<float[]>("dashArray"),
            DashOffset = section.GetExpression<float?>("dashOffset"),
            LineJoin = section.GetExpression<string>("lineJoin"),
            LineCap = section.GetExpression<string>("lineCap"),
            Translate = section.GetExpression<double[]>("translate"),
            TranslateAnchor = section.GetExpression<TranslateAnchor>("translateAnchor"),
            GapWidth = section.GetExpression<int?>("gapWidth"),
            Offset = section.GetExpression<int?>("offset"),
            Blur = section.GetExpression<int?>("blur"),
            Gradient = section.GetExpression<int?>("gradient"),
            Pattern = section.GetExpression<string>("pattern"),
            Uri = section.GetExpression<string>("uri")
        };
    }

    private Style GetTextStyle(JObject section)
    {
        var label = section.GetExpression<string>("property");

        if (label == null || (string.IsNullOrEmpty(label.Value) && string.IsNullOrEmpty(label.Expression)))
        {
            label = section.GetExpression<string>("label");
        }

        return new TextStyle
        {
            Label = label,
            Align = section.GetExpression<string>("align"),
            Color = section.GetExpression<string>("color"),
            Opacity = section.GetExpression<float?>("opacity"),
            BackgroundColor = section.GetExpression<string>("backgroundColor"),
            BackgroundOpacity = section.GetExpression<float?>("backgroundOpacity"),
            Radius = section.GetExpression<float?>("radius"),
            RadiusColor = section.GetExpression<string>("radiusColor"),
            RadiusOpacity = section.GetExpression<float?>("radiusOpacity"),
            Style = section.GetExpression<string>("style"),
            Font = section.GetExpression<List<string>>("font"),
            Size = section.GetExpression<int?>("size"),
            Weight = section.GetExpression<string>("weight"),
            Rotate = section.GetExpression<float?>("rotate"),
            Transform = section.GetExpression<TextTransform>("transform"),
            Offset = section.GetExpression<float[]>("offset"),
            OutlineSize = section.GetExpression<int?>("outlineSize")
        };
    }

    private Style GetFillStyle(JObject section)
    {
        FillStyle fillStyle;
        var pattern = section["pattern"]?.ToObject<string>();
        var patternExpression = section.GetExpression<string>("pattern");

        var resource = section["resource"]?.ToObject<string>();
        var resourceExpression = section.GetExpression<string>("resource");

        if (patternExpression != null && (!string.IsNullOrEmpty(patternExpression.Expression) ||
                                          !string.IsNullOrEmpty(patternExpression.Value)))
        {
            fillStyle = new SpriteFillStyle
            {
                Pattern = patternExpression,
                Uri = section.GetExpression<string>("uri")
            };
        }
        else if (!string.IsNullOrEmpty(pattern))
        {
            fillStyle = new SpriteFillStyle
            {
                Pattern = CSharpExpression<string>.New(pattern),
                Uri = section.GetExpression<string>("uri")
            };
        }
        else if (resourceExpression != null && (!string.IsNullOrEmpty(resourceExpression.Expression) ||
                                                !string.IsNullOrEmpty(resourceExpression.Value)))
        {
            fillStyle = new ResourceFillStyle
            {
                Uri = resourceExpression
            };
        }
        else if (!string.IsNullOrEmpty(resource))
        {
            fillStyle = new ResourceFillStyle
            {
                Uri = CSharpExpression<string>.New(resource)
            };
        }
        else
        {
            fillStyle = new FillStyle();
        }

        fillStyle.Opacity = section.GetExpression<float?>("opacity");
        fillStyle.Pattern = section.GetExpression<string>("pattern");
        fillStyle.Antialias = section["antialias"]?.ToObject<bool>() ?? true;
        fillStyle.Color = section.GetExpression<string>("color");
        // symbol.OutlineColor = section.GetExpression<string>("outlineColor");
        fillStyle.Translate = section.GetExpression<double[]>("translate");
        fillStyle.TranslateAnchor = section.GetExpression<TranslateAnchor?>("translateAnchor");
        return fillStyle;
    }
}