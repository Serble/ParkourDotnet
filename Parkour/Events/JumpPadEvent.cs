using ManagedServer.Entities.Types;
using ManagedServer.Events.Types;
using ManagedServer.Worlds;
using Minecraft.Schemas.Vec;

namespace Parkour.Events;

public class JumpPadEvent : IPlayerEvent, IParkourEvent {
    public required PlayerEntity Player { get; init; }
    public required ParkourGame Game { get; init; }
    public required Vec3<int> PadLocation { get; init; }

    public World World {
        get => Game.World;
        init { }
    }
    
    public Entity Entity {
        get => Player;
        init { }
    }
}
