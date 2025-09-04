using ManagedServer.Entities.Types;
using ManagedServer.Events.Types;
using ManagedServer.Worlds;

namespace Parkour.Events;

public class ParkourFinishedEvent : IPlayerEvent, IParkourEvent {
    public required PlayerEntity Player { get; init; }
    public required ParkourGame Game { get; init; }
    public required TimeSpan Time { get; init; }

    public World World {
        get => Game.World;
        init { }
    }
    
    public Entity Entity {
        get => Player;
        init { }
    }
}
