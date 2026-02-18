using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain;

/// <summary>
///     Stores the primary and linkshell permissions that make up the total permissions a user can grant another
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserPermissions
{
    public int Id;
    public int PairId;
    public string? PairUid;
    public DateTime? Expires;
    public RelationshipPriority Priority;
    public bool ControlsPerm;
    public bool ControlsConfig;
    public bool DisableSafeword;
    public InteractionPerms Perms;

    public UserPermissions() { }

    /// <summary>
    ///     <inheritdoc cref="UserPermissions"/>
    /// </summary>
    public UserPermissions(Pair pair)
    {
        Id = pair.Id;
        PairId = pair.PairId;
        Expires = pair.Expires;
        Priority = (RelationshipPriority)(pair.Priority ?? 0);
        ControlsPerm = pair.ControlsPerm ?? false;
        ControlsConfig = pair.ControlsConfig ?? false;
        DisableSafeword = pair.DisableSafeword ?? false;
        Perms = (InteractionPerms)(pair.Interactions ?? 0);
    }

    public UserPermissions(bool temporary, RelationshipPriority priority, InteractionPerms perms)
    {
        if (temporary)
        {
            Expires = DateTime.UtcNow.AddDays(1);
        }
        Perms = perms;
    }

    public UserPermissions(
        string uid,
        DateTime? expires,
        RelationshipPriority priority,
        bool controlsPerm,
        bool controlsConfig,
        bool disableSafeword,
        int? interactions
    )
    {
        PairUid = uid;
        Expires = expires;
        Priority = priority;
        ControlsPerm = controlsPerm;
        ControlsConfig = controlsConfig;
        DisableSafeword = disableSafeword;
        Perms = (InteractionPerms)(interactions ?? 0);
    }

    public UserPermissions(
        DateTime? expires,
        RelationshipPriority priority,
        bool controlsPerm,
        bool controlsConfig,
        bool disableSafeword,
        int? interactions,
        int? wardrobe,
        int? moodles,
        string? profileUid,
        string? pairUid
    )
    {
        Expires = expires;
        Priority = priority;
        ControlsPerm = controlsPerm;
        ControlsConfig = controlsConfig;
        DisableSafeword = disableSafeword;
        Perms = (InteractionPerms)(interactions ?? 0);
        PairUid = pairUid;
    }

    public UserPermissions(
        int id,
        int pairId,
        DateTime? expires,
        RelationshipPriority priority,
        bool controlsPerm,
        bool controlsConfig,
        bool disableSafeword,
        int? interactions
    )
    {
        Id = id;
        PairId = pairId;
        Expires = expires;
        Priority = priority;
        ControlsPerm = controlsPerm;
        ControlsConfig = controlsConfig;
        DisableSafeword = disableSafeword;
        Perms = (InteractionPerms)(interactions ?? 0);
    }
}
