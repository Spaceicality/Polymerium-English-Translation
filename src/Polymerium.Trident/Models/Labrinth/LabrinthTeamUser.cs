﻿namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthTeamUser
{
    public string Username { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Bio { get; set; }
    public object PayoutData { get; set; }
    public string Id { get; set; }
    public int? GithubId { get; set; }
    public Uri AvatarUrl { get; set; }
    public DateTimeOffset Created { get; set; }
    public string Role { get; set; }
    public int Badges { get; set; }
}