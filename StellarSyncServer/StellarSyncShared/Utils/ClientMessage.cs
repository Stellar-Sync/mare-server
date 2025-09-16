using StellarSync.API.Data.Enum;

namespace StellarSyncShared.Utils;
public record ClientMessage(MessageSeverity Severity, string Message, string UID);
