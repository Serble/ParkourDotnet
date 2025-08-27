using ManagedServer.Events.Types;

namespace Parkour.Events;

public interface IParkourEvent : IWorldEvent {
    public ParkourGame Game { get; init; }
}
