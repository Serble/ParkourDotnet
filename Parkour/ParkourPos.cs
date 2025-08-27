using Minecraft.Schemas.Vec;

namespace Parkour;

public record ParkourPos(Vec3<int> Pos, double Yaw = 0) {
    public static implicit operator Vec3<int>(ParkourPos p) => p.Pos;
    public static implicit operator ParkourPos(Vec3<int> v) => new(v);
    public static implicit operator ParkourPos[](ParkourPos v) => [v];
}
