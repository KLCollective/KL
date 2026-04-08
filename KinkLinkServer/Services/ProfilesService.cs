using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;

namespace KinkLinkServer.Services;

public class KinkLinkProfilesService
{
    private readonly ProfilesSql _profilesSql;

    public KinkLinkProfilesService(Configuration config)
    {
        _profilesSql = new ProfilesSql(config.DatabaseConnectionString);
    }

    public async Task<bool> ExistsAsync(string uid)
    {
        var result = await _profilesSql.ProfileExistsAsync(new(uid));
        return result is { } row && row.Exists;
    }

    public async Task<int?> GetIdFromUidAsync(string uid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(uid));
        return profile?.Id;
    }

    public async Task<KinkLinkProfile?> GetProfileByUidAsync(string uid)
    {
        var result = await _profilesSql.GetProfileByUidAsync(new(uid));
        if (result is not { } row)
            return null;

        return new KinkLinkProfile(
            row.Uid,
            row.ChatRole,
            row.Alias,
            Enum.Parse<Title>(row.Title),
            row.Description,
            null,
            null
        );
    }

    public async Task<KinkLinkProfile?> UpdateDetailsByUidAsync(
        string uid,
        KinkLinkCommon.Domain.Network.Title title,
        string alias,
        string chatRole,
        string description
    )
    {
        var profileId = await GetIdFromUidAsync(uid);
        if (profileId is not { } id)
            return null;

        var result = await _profilesSql.UpdateDetailsForProfileAsync(
            new(title.ToString(), description, uid, id)
        );

        if (result is not { } row)
            return null;

        return new KinkLinkProfile(
            row.Uid,
            row.ChatRole,
            row.Alias,
            Enum.Parse<Title>(row.Title),
            row.Description,
            row.CreatedAt,
            row.UpdatedAt
        );
    }
}
