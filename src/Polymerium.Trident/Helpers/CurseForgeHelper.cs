﻿using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PackageUrl;
using Polymerium.Trident.Models.Eternal;
using Polymerium.Trident.Repositories;
using Trident.Abstractions.Resources;
using static Trident.Abstractions.Metadata.Layer;

namespace Polymerium.Trident.Helpers;

public static class CurseForgeHelper
{
    private const string API_KEY = "$2a$10$cjd5uExXA6oMi3lSnylNC.xsFJiujI8uQ/pV1eGltFe/hlDO2mjzm";
    private const string ENDPOINT = "https://api.curseforge.com/v1";
    private const string PROJECT_URL = "https://www.curseforge.com/minecraft/{0}/{1}";

    private const uint GAME_ID = 432;
    private const uint CLASSID_MODPACK = 4471;
    private const uint CLASSID_MOD = 6;
    private const uint CLASSID_WORLD = 17;
    private const uint CLASSID_RESOURCEPACK = 12;

    public static readonly IReadOnlyDictionary<string, string> MODLOADERS_MAPPINGS = new Dictionary<string, string>
    {
        { "Forge", Loader.COMPONENT_FORGE },
        { "NeoForge", Loader.COMPONENT_NEOFORGE },
        { "Fabric", Loader.COMPONENT_FABRIC },
        { "Quilt", Loader.COMPONENT_QUILT }
    }.AsReadOnly();

    public static string MakePurl(uint projectId, uint? versionId = null)
    {
        return new PackageURL(RepositoryLabels.CURSEFORGE, null, projectId.ToString(), versionId?.ToString(), null,
            null).ToString();
    }

    public static string GetUrlTypeStringFromKind(ResourceKind kind)
    {
        return kind switch
        {
            ResourceKind.Modpack => "modpacks",
            ResourceKind.Mod => "mods",
            ResourceKind.World => "worlds",
            ResourceKind.ResourcePack => "texture-packs",
            ResourceKind.ShaderPack => "shaders",
            _ => "unknown"
        };
    }

    public static ResourceKind GetResourceTypeFromClassId(uint classId)
    {
        return classId switch
        {
            6 => ResourceKind.Mod,
            12 => ResourceKind.ResourcePack,
            17 => ResourceKind.World,
            4546 => ResourceKind.ShaderPack,
            4471 => ResourceKind.Modpack,
            6552 => ResourceKind.ShaderPack,
            _ => throw new NotImplementedException()
        };
    }

    private static async Task<T?> GetResourceAsync<T>(ILogger logger, IHttpClientFactory factory, string service,
        IMemoryCache cache, CancellationToken token = default)
        where T : struct
    {
        if (token.IsCancellationRequested)
            return null;
        return await cache.GetOrCreateAsync(
            service,
            async entry =>
            {
                var found = false;
                T? result = default;
                using var client = factory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                try
                {
                    var response = await client.GetFromJsonAsync<ResponseWrapper<T>>(ENDPOINT + service, token);
                    if (response?.Data != null)
                    {
                        result = response.Data;
                        found = true;
                    }
                }
                catch (Exception e)
                {
                    logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
                }

                entry.SetSlidingExpiration(TimeSpan.FromSeconds(found ? 60 * 60 : 1));
                return result;
            }
        );
    }

    private static async Task<string?> GetStringAsync(ILogger logger, IHttpClientFactory factory, IMemoryCache cache,
        string service, CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
            return null;
        return await cache.GetOrCreateAsync(
            service,
            async entry =>
            {
                var found = false;
                string? result = default;
                using var client = factory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                try
                {
                    var response = await client.GetFromJsonAsync<ResponseWrapper<string>>(ENDPOINT + service, token);
                    if (response?.Data != null)
                    {
                        result = response.Data;
                        found = true;
                    }
                }
                catch (Exception e)
                {
                    logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
                }

                entry.SetSlidingExpiration(TimeSpan.FromSeconds(found ? 60 * 60 : 1));
                return result;
            }
        );
    }

    private static async Task<IEnumerable<T>> GetResourcesAsync<T>(ILogger logger, IHttpClientFactory factory,
        IMemoryCache cache, string service, CancellationToken token = default)
        where T : struct
    {
        if (token.IsCancellationRequested)
            return Enumerable.Empty<T>();
        return await cache.GetOrCreateAsync(
            service,
            async entry =>
            {
                IEnumerable<T>? results = null;
                using var client = factory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                try
                {
                    var response =
                        await client.GetFromJsonAsync<ResponseWrapper<IEnumerable<T>>>(ENDPOINT + service, token);
                    if (response?.Data != null) results = response.Data;
                }
                catch (Exception e)
                {
                    logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
                }

                entry.SetSlidingExpiration(TimeSpan.FromSeconds(results != null ? 60 * 60 : 1));
                return results ?? Enumerable.Empty<T>();
            }
        ) ?? Enumerable.Empty<T>();
    }

    public static async Task<IEnumerable<EternalProject>> SearchProjectsAsync(ILogger logger,
        IHttpClientFactory factory, IMemoryCache cache, string query, ResourceKind kind, string? gameVersion = null,
        string? modLoaderId = null, uint offset = 0, uint limit = 10, CancellationToken token = default)
    {
        var modLoaderType = modLoaderId switch
        {
            Loader.COMPONENT_FORGE => 1,
            Loader.COMPONENT_FABRIC => 4,
            Loader.COMPONENT_QUILT => 5,
            _ => 0
        };
        var service =
            $"/mods/search?gameId={GAME_ID}&classId={kind switch
            {
                ResourceKind.Modpack => CLASSID_MODPACK,
                ResourceKind.Mod => CLASSID_MOD,
                ResourceKind.ResourcePack => CLASSID_RESOURCEPACK,
                ResourceKind.World => CLASSID_WORLD,
                _ => throw new NotSupportedException()
            }}&index={offset}&pageSize={limit}&searchFilter={HttpUtility.UrlPathEncode(query)}&sortField=2&sortOrder=desc"
            + (gameVersion != null ? $"&gameVersion={gameVersion}" : "")
            + (
                (kind == ResourceKind.Mod || kind == ResourceKind.Modpack) && modLoaderId != null
                    ? $"&modLoaderType={modLoaderType}"
                    : ""
            );
        return await GetResourcesAsync<EternalProject>(logger, factory, cache, service, token);
    }

    public static async Task<EternalProject?> GetModInfoAsync(ILogger logger, IHttpClientFactory factory,
        IMemoryCache cache, uint projectId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}";
        return await GetResourceAsync<EternalProject>(logger, factory, service, cache, token);
    }

    public static async Task<string?> GetModDescriptionAsync(ILogger logger, IHttpClientFactory factory,
        IMemoryCache cache, uint projectId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/description";
        return await GetStringAsync(logger, factory, cache, service, token);
    }

    public static async Task<string?> GetModDownloadUrlAsync(ILogger logger, IHttpClientFactory factory,
        IMemoryCache cache, uint projectId, uint fileId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{fileId}/download-url";
        return await GetStringAsync(logger, factory, cache, service, token);
    }

    public static async Task<string?> GetModFileChangelogAsync(ILogger logger, IHttpClientFactory factory,
        IMemoryCache cache, uint projectId, uint fileId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{fileId}/changelog";
        return await GetStringAsync(logger, factory, cache, service, token);
    }

    public static async Task<IEnumerable<EternalVersion>> GetModFilesAsync(ILogger logger, IHttpClientFactory factory,
        IMemoryCache cache, uint projectId, CancellationToken token = default)
    {
        var services = $"/mods/{projectId}/files";
        return await GetResourcesAsync<EternalVersion>(logger, factory, cache, services, token);
    }

    public static async Task<EternalVersion?> GetModFileInfoAsync(ILogger logger, IHttpClientFactory factory,
        IMemoryCache cache, uint projectId, uint versionId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{versionId}";
        return await GetResourceAsync<EternalVersion>(logger, factory, service, cache, token);
    }

    public static async Task<Project?> GetProjectAsync(ILogger logger, IHttpClientFactory factory, IMemoryCache cache,
        uint projectId, CancellationToken token = default)
    {
        var mod = await GetModInfoAsync(logger, factory, cache, projectId, token);
        var modDesc = await GetModDescriptionAsync(logger, factory, cache, projectId, token);
        if (mod.HasValue && modDesc != null)
        {
            var files = await GetModFilesAsync(logger, factory, cache, projectId, token);
            if (files.Any())
            {
                var versionTasks = files.Where(x => x is { IsAvailable: true, IsServerPack: false, FileStatus: 4 })
                    .OrderByDescending(x => x.FileDate).Select(async x =>
                    {
                        var changelog = await GetModFileChangelogAsync(logger, factory, cache, projectId, x.Id, token);
                        if (changelog != null)
                            return new Project.Version(x.DisplayName, changelog, x.ExtractReleaseType(), x.FileDate,
                                x.FileName, x.ExtractSha1(), x.ExtractDownloadUrl(),
                                x.ExtractRequirement(), ExtractDependencies(x, mod.Value.Id));

                        return null;
                    });
                await Task.WhenAll(versionTasks);
                var versions = versionTasks.Where(x => x.IsCompletedSuccessfully && x.Result != null)
                    .Select(x => x.Result!);
                var kind = GetResourceTypeFromClassId(mod.Value.ClassId);
                return new Project(
                    mod.Value.Id.ToString(),
                    mod.Value.Name,
                    mod.Value.Logo?.ThumbnailUrl,
                    string.Join(", ", mod.Value.Authors.Select(x => x.Name)),
                    mod.Value.Summary,
                    new Uri(PROJECT_URL.Replace("{0}", GetUrlTypeStringFromKind(kind)).Replace("{1}", mod.Value.Slug)),
                    kind,
                    mod.Value.DateCreated,
                    mod.Value.DateModified,
                    mod.Value.DownloadCount, modDesc,
                    mod.Value.Screenshots.Select(x => new Project.Screenshot(x.Title, x.Url)),
                    versions);
            }

            return null;
        }

        return null;
    }

    public static async Task<Package?> GetPackageAsync(ILogger logger, IHttpClientFactory factory, IMemoryCache cache,
        uint projectId, uint versionId, CancellationToken token = default)
    {
        var mod = await GetModInfoAsync(logger, factory, cache, projectId, token);
        if (mod.HasValue)
        {
            var kind = GetResourceTypeFromClassId(mod.Value.ClassId);
            var file = await GetModFileInfoAsync(logger, factory, cache, projectId, versionId, token);
            if (file.HasValue)
            {
                var package = new Package(
                    mod.Value.Id.ToString(),
                    mod.Value.Name,
                    file.Value.Id.ToString(),
                    file.Value.DisplayName, mod.Value.Logo?.Url,
                    string.Join(", ", mod.Value.Authors.Select(x => x.Name)),
                    mod.Value.Summary,
                    new Uri(PROJECT_URL.Replace("{0}", GetUrlTypeStringFromKind(kind))
                        .Replace("{1}", mod.Value.Slug)),
                    kind,
                    file.Value.ExtractReleaseType(),
                    file.Value.FileDate,
                    file.Value.FileName,
                    file.Value.ExtractDownloadUrl(),
                    file.Value.ExtractSha1(),
                    file.Value.ExtractRequirement(),
                    ExtractDependencies(file.Value, mod.Value.Id)
                );
                return package;
            }
        }
        return null;
    }

    private static IEnumerable<Dependency> ExtractDependencies(EternalVersion file, uint projectId)
    {
        return file.Dependencies
            .Where(x => x.RelationType == 3 || x.RelationType == 2)
            .Select(x => new Dependency(MakePurl(projectId), x.RelationType == 3));
    }

    private static IEnumerable<Dependency> ExtractDependencies(EternalProjectLatestFile file, uint projectId)
    {
        return file.Dependencies
            .Where(x => x.RelationType == 3 || x.RelationType == 2)
            .Select(x => new Dependency(MakePurl(projectId), x.RelationType == 3));
    }

    public class ResponseWrapper<T>
    {
        public T? Data { get; set; }
    }
}