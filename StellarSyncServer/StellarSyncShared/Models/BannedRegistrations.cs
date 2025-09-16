using System.ComponentModel.DataAnnotations;

namespace StellarSyncShared.Models;

public class BannedRegistrations
{
    [Key]
    [MaxLength(100)]
    public string DiscordIdOrLodestoneAuth { get; set; }
}
