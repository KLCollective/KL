using System;

namespace KinkLinkClient.Services;

public record SlotStatus(string SlotName, bool HasItem, string? ItemDisplay, Guid? PieceId);
