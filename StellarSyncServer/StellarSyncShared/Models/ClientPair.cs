using System.ComponentModel.DataAnnotations;

namespace StellarSyncShared.Models;

public class ClientPair
{
    [MaxLength(10)]
    public string UserUID { get; set; }
    public User User { get; set; }
    [MaxLength(10)]
    public string OtherUserUID { get; set; }
    public User OtherUser { get; set; }
    [Timestamp]
    public byte[] Timestamp { get; set; }
}
