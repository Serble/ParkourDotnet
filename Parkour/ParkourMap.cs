using Minecraft.Implementations.Server.Terrain;
using Minecraft.Schemas;
using Minecraft.Schemas.Vec;

namespace Parkour;

public record ParkourMap(
    ITerrainProvider Map,
    bool BlockPlacing,
    Identifier[] DeathBlocks,
    ParkourPos[][] Checkpoints) {
    
    public PlayerPosition Spawn => new(Checkpoints[0][0].Pos.BlockPosToDouble(), Vec3<double>.Zero, Angle.FromDegrees(Checkpoints[0][0].Yaw), Angle.Zero);
}
