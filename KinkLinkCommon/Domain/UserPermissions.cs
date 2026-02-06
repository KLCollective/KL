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
    public DateTime? Expires;
    public RelationshipPriority Priority;
    public GagPermissions Gags;
    public WardrobePermissions Wardrobe;
    public MoodlesPermissions Moodles;

    public UserPermissions()
    {

    }
    /// <summary>
    ///     <inheritdoc cref="UserPermissions"/>
    /// </summary>
    public UserPermissions(Pair pair)
    {
        // Reimplement when the new bitmasks are enabled
    }
    public UserPermissions(bool temporary, RelationshipPriority priority, GagPermissions gags, WardrobePermissions wardrobe, MoodlesPermissions moodles) {
        if (temporary) {
            Expires = DateTime.UtcNow.AddDays(1);
        }
        Gags = gags;
        Wardrobe = wardrobe;
        Moodles = moodles;
    }
}
