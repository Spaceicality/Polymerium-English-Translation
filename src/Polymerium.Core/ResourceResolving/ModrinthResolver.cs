﻿using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.Modrinth.Labrinth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("modrinth")]
public class ModrinthResolver : ResourceResolverBase
{
#pragma warning disable S1075 // URIs should not be hardcoded
    private const string MODRINTH_PROJECT_URL = "https://modrinth.com/{0}/{1}";
#pragma warning restore S1075 // URIs should not be hardcoded

    private readonly IMemoryCache _cache;

    public ModrinthResolver(IMemoryCache cache)
    {
        _cache = cache;
    }

    private string MembersToLine(IEnumerable<LabrinthTeamMember> members)
    {
        return string.Join(
            ", ",
            members.Select(x => !string.IsNullOrEmpty(x.User.Name) ? x.User.Name : x.User.Username)
        );
    }

    private async Task<Result<ResolveResult, ResolveResultError>> GetProjectAsync(
        ResourceType type,
        string projectId,
        string version,
        Func<LabrinthProject, LabrinthVersion, IEnumerable<LabrinthTeamMember>, ResourceBase> cast
    )
    {
        var project = await ModrinthHelper.GetProjectAsync(projectId, _cache);
        var file = await ModrinthHelper.GetVersionAsync(version, _cache);
        if (project.HasValue && file.HasValue)
        {
            var teams = await ModrinthHelper.GetTeamMembersAsync(project.Value.Team, _cache);
            return new ResolveResult(cast(project.Value, file.Value, teams), type);
        }

        return Err(ResolveResultError.NotFound);
    }

    [ResourceType(ResourceType.Modpack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModpackAsync(
        string projectId,
        string version
    )
    {
        return await GetProjectAsync(
            ResourceType.Modpack,
            projectId,
            version,
            (project, file, members) =>
                new Modpack(
                    project.Id ?? project.Slug,
                    project.Title,
                    file.VersionNumber,
                    MembersToLine(members),
                    project.IconUrl,
                    new Uri(
                        MODRINTH_PROJECT_URL.Replace("{0}", "modpack").Replace("{1}", project.Slug)
                    ),
                    project.Description,
                    version,
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.Update,
                        projectId,
                        version,
                        ResourceType.Modpack
                    ),
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.File,
                        projectId,
                        version,
                        ResourceType.Modpack
                    )
                )
        );
    }

    [ResourceType(ResourceType.Mod)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModAsync(
        string projectId,
        string version
    )
    {
        return await GetProjectAsync(
            ResourceType.Mod,
            projectId,
            version,
            (project, file, members) =>
                new Mod(
                    project.Id ?? project.Slug,
                    project.Title,
                    file.VersionNumber,
                    MembersToLine(members),
                    project.IconUrl,
                    new Uri(
                        MODRINTH_PROJECT_URL.Replace("{0}", "mod").Replace("{1}", project.Slug)
                    ),
                    project.Description,
                    version,
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.Update,
                        projectId,
                        version,
                        ResourceType.Mod
                    ),
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.File,
                        projectId,
                        version,
                        ResourceType.Mod
                    )
                )
        );
    }

    [ResourceType(ResourceType.ResourcePack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetResourcePackAsync(
        string projectId,
        string version
    )
    {
        return await GetProjectAsync(
            ResourceType.ResourcePack,
            projectId,
            version,
            (project, file, members) =>
                new ResourcePack(
                    project.Id ?? project.Slug,
                    project.Title,
                    file.VersionNumber,
                    MembersToLine(members),
                    project.IconUrl,
                    new Uri(
                        MODRINTH_PROJECT_URL
                            .Replace("{0}", "resourcepack")
                            .Replace("{1}", project.Slug)
                    ),
                    project.Description,
                    version,
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.Update,
                        projectId,
                        version,
                        ResourceType.ResourcePack
                    ),
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.File,
                        projectId,
                        version,
                        ResourceType.ResourcePack
                    )
                )
        );
    }

    [ResourceType(ResourceType.ShaderPack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetShaderPackAsync(
        string projectId,
        string version
    )
    {
        return await GetProjectAsync(
            ResourceType.ShaderPack,
            projectId,
            version,
            (project, file, members) =>
                new ResourcePack(
                    project.Id ?? project.Slug,
                    project.Title,
                    file.VersionNumber,
                    MembersToLine(members),
                    project.IconUrl,
                    new Uri(
                        MODRINTH_PROJECT_URL.Replace("{0}", "shader").Replace("{1}", project.Slug)
                    ),
                    project.Description,
                    version,
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.Update,
                        projectId,
                        version,
                        ResourceType.ShaderPack
                    ),
                    ModrinthHelper.MakeResourceUrl(
                        ResourceType.File,
                        projectId,
                        version,
                        ResourceType.ShaderPack
                    )
                )
        );
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{dir}/{version}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetFileAsync(
        string dir,
        string version
    )
    {
        var file = await ModrinthHelper.GetVersionAsync(version, _cache);
        if (file.HasValue)
        {
            var first = file.Value.Files.First();
            return Ok(
                new File(
                    file.Value.ProjectId,
                    file.Value.Name,
                    file.Value.VersionNumber,
                    file.Value.Id,
                    $"{dir}/{first.Filename}",
                    first.Hashes.Sha1,
                    first.Url
                ),
                ResourceType.File
            );
        }

        return Err(ResolveResultError.NotFound);
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{version}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModpackFileAsync(string version)
    {
        var file = await ModrinthHelper.GetVersionAsync(version, _cache);
        if (file.HasValue)
        {
            var first = file.Value.Files.First();
            return Ok(
                new File(
                    file.Value.ProjectId,
                    file.Value.Name,
                    file.Value.VersionNumber,
                    file.Value.Id,
                    first.Filename,
                    first.Hashes.Sha1,
                    first.Url
                ),
                ResourceType.File
            );
        }

        return Err(ResolveResultError.NotFound);
    }

    [ResourceType(ResourceType.Update)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetVersionsAsync(
        string projectId,
        string current
    )
    {
        var project = await ModrinthHelper.GetProjectAsync(projectId, _cache);
        var version = await ModrinthHelper.GetVersionAsync(current, _cache);
        var versions = await ModrinthHelper.GetProjectVersionsAsync(projectId, _cache);
        if (project.HasValue && version.HasValue && versions.Any())
        {
            var type = ModrinthHelper.GetResourceTypeFromString(project.Value.ProjectType);
            var updates = versions
                .OrderByDescending(x => x.DatePublished)
                .Select(x => ModrinthHelper.MakeResourceUrl(type, projectId, x.Id, null));
            var result = new Update(
                projectId,
                project.Value.Title,
                version.Value.Name,
                version.Value.Id,
                updates
            );
            return Ok(result, ResourceType.Update);
        }

        return Err(ResolveResultError.NotFound);
    }
}