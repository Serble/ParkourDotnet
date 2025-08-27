using ManagedServer;
using ManagedServer.Events;
using Minecraft.Data.Generated;
using Minecraft.Implementations.AnvilWorld;
using Minecraft.Implementations.Server.Terrain;
using Minecraft.Schemas.Vec;
using Parkour;

ManagedMinecraftServer server = ManagedMinecraftServer.NewBasic();

ITerrainProvider terrain = new AnvilLoader("overworld", VanillaRegistry.Data);
ParkourMap map = new(terrain, false, [
    Block.Water.Identifier
], [
    new ParkourPos(new Vec3<int>(46, 26, 42), 90),
    new ParkourPos(new Vec3<int>(1, 22, 42), 90),
    new ParkourPos(new Vec3<int>(-43, 28, 23), 180),
    new ParkourPos(new Vec3<int>(-43, 28, -30), 180),
    new ParkourPos(new Vec3<int>(-16, 32, -57), -90),
    new ParkourPos(new Vec3<int>(59, 32, -55)),
    new ParkourPos(new Vec3<int>(44, 32, -33), 14),
    new ParkourPos(new Vec3<int>(24, 32, -11), 90),
    new ParkourPos(new Vec3<int>(13, 32, -11), 90),
    new ParkourPos(new Vec3<int>(0, 32, 0))
]);

ParkourGame game = new(map);
game.Initialise(server);

server.Events.AddListener<PlayerPreLoginEvent>(e => {
    e.World = game.World;
});

server.Start();

Console.WriteLine("Server started.");
await server.ListenTcp(25565, CancellationToken.None);
