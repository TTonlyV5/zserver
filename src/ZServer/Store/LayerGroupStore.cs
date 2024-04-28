using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ZMap;
using ZMap.Infrastructure;

namespace ZServer.Store;

public class LayerGroupStore(
    IResourceGroupStore resourceGroupStore,
    ILayerStore layerStore)
    : ILayerGroupStore
{
    private static Dictionary<string, LayerGroup> _cache = new();
    private static readonly ILogger Logger = Log.CreateLogger<LayerGroupStore>();

    public async Task Refresh(List<JObject> configurations)
    {
        var dict = new Dictionary<string, LayerGroup>();

        foreach (var configuration in configurations)
        {
            var sections = configuration.SelectToken("layerGroups");
            if (sections == null)
            {
                continue;
            }

            foreach (var section in sections.Children<JProperty>())
            {
                var name = section.Name;
                var obj = section.Value as JObject;
                if (obj == null)
                {
                    continue;
                }

                var resourceGroupName = obj["resourceGroup"]?.ToObject<string>();

                var resourceGroup = string.IsNullOrWhiteSpace(resourceGroupName)
                    ? null
                    : await resourceGroupStore.FindAsync(resourceGroupName);
                var servicesToken = obj["services"];
                var services = servicesToken == null
                    ? new HashSet<ServiceType>()
                    : servicesToken.ToObject<HashSet<ServiceType>>();

                var layerGroup = Activator.CreateInstance<LayerGroup>();
                layerGroup.Services = services;
                layerGroup.Name = name;
                layerGroup.ResourceGroup = resourceGroup;
                layerGroup.Layers = new List<Layer>();
                await RestoreAsync(layerGroup, obj);
                dict.Add(name, layerGroup);
            }
        }

        _cache = dict;
    }

    private async Task RestoreAsync(LayerGroup layerGroup, JObject section)
    {
        var layerNames = section["layers"]?.ToObject<HashSet<string>>();
        if (layerNames != null)
        {
            foreach (var layerName in layerNames)
            {
                var parts = layerName.Split(':');
                Layer layer = null;
                switch (parts.Length)
                {
                    case 1:
                        layer = await layerStore.FindAsync(null, layerName);
                        break;
                    case 2:
                        layer = await layerStore.FindAsync(parts[0], parts[1]);
                        break;
                    default:
                        Logger.LogError("图层组 {LayerGroupName} 中的图层 {LayerName} 不存在", layerGroup.Name, layerName);
                        break;
                }

                if (layer != null)
                {
                    layerGroup.Layers.Add(layer);
                }
            }
        }
    }

    // public async Task Refresh(IEnumerable<IConfiguration> configurations)
    // {
    //     var existKeys = Cache.Keys.ToList();
    //     var keys = new List<string>();
    //
    //     foreach (var configuration in configurations)
    //     {
    //         var sections = configuration.GetSection("layerGroups");
    //         foreach (var section in sections.GetChildren())
    //         {
    //             var resourceGroupName = section.GetValue<string>("resourceGroup");
    //
    //             var resourceGroup = string.IsNullOrWhiteSpace(resourceGroupName)
    //                 ? null
    //                 : await resourceGroupStore.FindAsync(resourceGroupName);
    //
    //             var layerGroup = Activator.CreateInstance<LayerGroup>();
    //             layerGroup.Services = section.GetSection("ogcWebServices").Get<HashSet<ServiceType>>();
    //             layerGroup.Name = section.Key;
    //             layerGroup.ResourceGroup = resourceGroup;
    //             layerGroup.Layers = new List<Layer>();
    //             await RestoreAsync(layerGroup, section);
    //
    //             keys.Add(layerGroup.Name);
    //             Cache.AddOrUpdate(layerGroup.Name, layerGroup, (_, _) => layerGroup);
    //         }
    //     }
    //
    //     var removedKeys = existKeys.Except(keys);
    //     foreach (var removedKey in removedKeys)
    //     {
    //         Cache.TryRemove(removedKey, out _);
    //     }
    // }

    public async Task<LayerGroup> FindAsync(string resourceGroupName, string layerGroupName)
    {
        if (string.IsNullOrWhiteSpace(resourceGroupName) && string.IsNullOrWhiteSpace(layerGroupName))
        {
            return null;
        }

        if (!_cache.TryGetValue(layerGroupName, out var layerGroup))
        {
            return null;
        }

        if (string.IsNullOrEmpty(resourceGroupName))
        {
            return await Task.FromResult(layerGroup.Clone());
        }

        if (layerGroup.ResourceGroup?.Name != resourceGroupName)
        {
            return null;
        }

        return await Task.FromResult(layerGroup.Clone());

        // var item = await Cache.GetOrCreate($"{GetType().FullName}:{resourceGroupName}:{layerGroupName}",
        //     async entry =>
        //     {
        //         ResourceGroup resourceGroup = null;
        //         // 若传的资源组不为空，才需要查询资源组信息
        //         if (!string.IsNullOrEmpty(resourceGroupName))
        //         {
        //             // 若资源组不存在，则返回空
        //             resourceGroup = await _resourceGroupStore.FindAsync(resourceGroupName);
        //             if (resourceGroup == null)
        //             {
        //                 _logger.LogError("资源组 {ResourceGroupName} 不存在", resourceGroupName);
        //                 return null;
        //             }
        //         }
        //
        //         var section =
        //             _configuration.GetSection($"layerGroups:{layerGroupName}");
        //
        //         var configResourceGroupName = section.GetSection("resourceGroup").Get<string>();
        //
        //         // 若传的资源组不为空，并且代码执行到此处，说明资源组存在
        //         if (!string.IsNullOrEmpty(resourceGroupName))
        //         {
        //             // 若配置的资源组与查询资源组不一致，则返回空
        //             if (configResourceGroupName != resourceGroupName)
        //             {
        //                 return null;
        //             }
        //         }
        //
        //         if (resourceGroup == null && !string.IsNullOrEmpty(configResourceGroupName))
        //         {
        //             resourceGroup = await _resourceGroupStore.FindAsync(configResourceGroupName);
        //         }
        //
        //         var layerGroup = Activator.CreateInstance<LayerGroup>();
        //         layerGroup.Services = section.GetSection("ogcWebServices").Get<HashSet<ServiceType>>();
        //         layerGroup.Name = layerGroupName;
        //         layerGroup.ResourceGroup = resourceGroup;
        //         layerGroup.Layers = new List<ILayer>();
        //
        //         await RestoreAsync(layerGroup, section);
        //
        //         if (layerGroup.Layers.Count == 0)
        //         {
        //             _logger.LogWarning("图层组 {LayerGroupName} 中的没有有效图层", layerGroupName);
        //         }
        //
        //         entry.SetValue(layerGroup);
        //         entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.ConfigurationCacheTtl));
        //         return layerGroup;
        //     });
        // // 1. 从 configuration 中解析对象非常耗时，因此需要使用缓存
        // // 2. 但是若是直接返回对象，会导致对象被复用（修改），状态可能不正确
        // return item?.Clone();
    }

    public Task<List<LayerGroup>> GetAllAsync()
    {
        var items = _cache.Values.Select(x => x.Clone()).ToList();
        return Task.FromResult(items);
    }

    public Task<List<LayerGroup>> GetListAsync(string resourceGroup)
    {
        var result = new List<LayerGroup>();
        if (string.IsNullOrWhiteSpace(resourceGroup))
        {
            return Task.FromResult(result);
        }

        foreach (var value in _cache.Values)
        {
            if (value.ResourceGroup?.Name == resourceGroup)
            {
                result.Add(value.Clone());
            }
        }

        return Task.FromResult(result);
    }

    // private async Task RestoreAsync(LayerGroup layerGroup, IConfigurationSection section)
    // {
    //     var layerNames = section.GetSection("layers").Get<HashSet<string>>();
    //     if (layerNames != null)
    //     {
    //         foreach (var layerName in layerNames)
    //         {
    //             var parts = layerName.Split(':');
    //             Layer layer = null;
    //             switch (parts.Length)
    //             {
    //                 case 1:
    //                     layer = await layerStore.FindAsync(null, layerName);
    //                     break;
    //                 case 2:
    //                     layer = await layerStore.FindAsync(parts[0], parts[1]);
    //                     break;
    //                 default:
    //                     Logger.LogError("图层组 {LayerGroupName} 中的图层 {LayerName} 不存在", layerGroup.Name, layerName);
    //                     break;
    //             }
    //
    //             if (layer != null)
    //             {
    //                 layerGroup.Layers.Add(layer);
    //             }
    //         }
    //     }
    // }
}