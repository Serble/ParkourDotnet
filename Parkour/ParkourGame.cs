using System.Diagnostics;
using ManagedServer;
using ManagedServer.Entities.Types;
using ManagedServer.Events;
using ManagedServer.Features;
using ManagedServer.Viewables;
using ManagedServer.Worlds;
using Minecraft;
using Minecraft.Data.Blocks;
using Minecraft.Data.Generated;
using Minecraft.Implementations.Tags;
using Minecraft.Packets.Config.ClientBound;
using Minecraft.Schemas;
using Minecraft.Schemas.Shapes;
using Minecraft.Schemas.Vec;
using Minecraft.Text;
using Parkour.Events;

namespace Parkour;

public class ParkourGame(ParkourMap map) {
    private static readonly Tag<int> CheckpointTag = new("parkour:current_checkpoint");
    private static readonly Tag<DateTime> JumpPadCooldownTag = new("parkour:jump_pad_cooldown");
    private static readonly Tag<Stopwatch> TimerTag = new("parkour:timer");
    
    public World World { get; private set; } = null!;
    
    public World Initialise(ManagedMinecraftServer server) {
        World = server.CreateWorld(map.Map);
        World.AddFeature(new ParkourItemsFeature(this));

        int[] climbableBlockIds = VanillaTags.BlockClimbable
            .Select(b => VanillaRegistry.Data.Blocks[b].ProtocolId)
            .ToArray();

        World.Events.AddListener<PlayerPlaceBlockEvent>(e => {
            e.Cancelled = true;
        });

        World.Events.AddListener<PlayerBreakBlockEvent>(e => {
            e.Cancelled = true;
        });

        World.Events.AddListener<PlayerEnteringWorldEvent>(e => {
            e.Player.Teleport(map.Spawn with {
                Position = map.Spawn.Position + new Vec3<double>(0, 2, 0)
            });
            e.Player.GameMode = map.BlockPlacing ? GameMode.Survival : GameMode.Adventure;
            e.Player.SendPacket(new ClientBoundUpdateTagsPacket {
                Tags = [
                    new ClientBoundUpdateTagsPacket.TagSet("block", [
                        new ClientBoundUpdateTagsPacket.Tag("climbable", climbableBlockIds)
                    ])
                ]
            });
        });

        World.Events.AddListener<PlayerBreakBlockEvent>(e => e.Cancelled = true);

        World.Events.AddListener<ServerTickEvent>(_ => {
            foreach (PlayerEntity player in World.Players) {
                Stopwatch sw = player.GetTagOrDefault(TimerTag, new Stopwatch());
                player.SendActionBar(TextComponent.FromLegacyString($@"&6{sw.Elapsed:mm\:ss\.ff}"));
            }
        });

        World.Events.AddListener<EntityMoveEvent>(e => {
            if (e.Entity is not PlayerEntity player) {
                return;
            }

            if (e.NewPos.Y < World.Dimension.MinY) {
                Respawn(player);
                return;  // We can't get blocks if they are too low
            }
            
            if (!World.IsBlockLoaded(e.NewPos.ToBlockPos())) {
                return;  // don't do anything if the chunk isn't loaded
            }

            if (e.NewPos.WithY(0) != player.Position.WithY(0)) {
                // they moved vertically
                if (!player.HasTag(TimerTag)) {
                    player.SetTag(TimerTag, Stopwatch.StartNew());
                }
            }

            int currentCheckpoint = player.GetTagOrDefault(CheckpointTag, 0);
            if (player.OnGround && currentCheckpoint+1 != map.Checkpoints.Length) {
                // checkpoint check
                // Where are they trying to get
                ParkourPos[] validBlocks = map.Checkpoints[currentCheckpoint+1];
            
                // check if they are in any of those blocks
                bool gotCheckpoint = false;
                foreach (ParkourPos block in validBlocks) {
                    ICollisionBox playerBox = player.BoundingBox.Add(player.Position);
                    Aabb blockBox = new(block.Pos, Vec3<double>.One);
                    if (!playerBox.CollidesWithAabb(blockBox)) continue;
                
                    gotCheckpoint = true;
                    break;
                }

                if (gotCheckpoint) {
                    Checkpoint(player, currentCheckpoint + 1);
                }
            }
            
            // death check
            bool dead = e.NewPos.Y < World.Dimension.MinY + 10;
            if (!dead) foreach (Vec3<int> block in GetPlayerCollidingBlocks(player, e.NewPos)) {
                IBlock type = World.GetBlock(block);
                if (map.DeathBlocks.Contains(type.Identifier)) {
                    dead = true;
                    break;
                }
            }

            if (dead) {
                Respawn(player);
                return;
            }
            
            // jump pad check
            IBlock blockBelow = World.GetBlock(player.Position - new Vec3<double>(0, 0.1, 0));
            if (blockBelow.Equals(Block.EmeraldBlock) && player.GetTagOrDefault(JumpPadCooldownTag, DateTime.MinValue) < DateTime.UtcNow) {
                player.SetVelocity(new Vec3<double>(0, 1.19, 0));
                player.SetTag(JumpPadCooldownTag, DateTime.UtcNow.AddSeconds(5D/20D));  // 5 ticks
                
                JumpPadEvent jumpPadEvent = new() {
                    Player = player,
                    World = player.World.ThrowIfNull(),
                    Game = this,
                    PadLocation = (player.Position - new Vec3<double>(0, 0.1, 0)).ToBlockPos()
                };
                World.Events.CallEvent(jumpPadEvent);
            }
        });
        
        return World;
    }

    public void Checkpoint(PlayerEntity player, int checkpoint) {
        player.SetTag(CheckpointTag, checkpoint);
        if (checkpoint + 1 == map.Checkpoints.Length) {
            player.SendMessage(TextComponent.FromLegacyString("&aYou have completed the parkour!"));
            player.PlaySound(SoundType.PlayerLevelup, player);

            if (player.HasTag(TimerTag)) {
                player.GetTag(TimerTag).Stop();
            }

            ParkourFinishedEvent finishEvent = new() {
                Player = player,
                World = player.World.ThrowIfNull(),
                Game = this,
                Time = player.GetTagOrDefault(TimerTag, new Stopwatch()).Elapsed
            };
            World.Events.CallEvent(finishEvent);
        }
        else {
            player.SendMessage(TextComponent.FromLegacyString($"&aCheckpoint &6{checkpoint}&a reached!"));
            player.SendTitle(TextComponent.Text(""), TextComponent.FromLegacyString($"&aCheckpoint &6{checkpoint} &areached!"));
            player.PlaySound(SoundType.ExperienceOrbPickup, player);
        }
                    
        ParkourCheckpointEvent checkpointEvent = new() {
            Player = player,
            World = player.World.ThrowIfNull(),
            Game = this,
            Checkpoint = checkpoint,
            Time = player.GetTagOrDefault(TimerTag, new Stopwatch()).Elapsed
        };
        World.Events.CallEvent(checkpointEvent);
    }

    public void Respawn(PlayerEntity player, int currentCheckpoint = -1) {
        if (currentCheckpoint == -1) {
            currentCheckpoint = player.GetTagOrDefault(CheckpointTag, 0);
        }
        ParkourPos respawnPos = map.Checkpoints[currentCheckpoint][0];
        player.Teleport(respawnPos.Pos.BlockPosToDouble(), Angle.FromDegrees(respawnPos.Yaw), Angle.Zero);

        if (currentCheckpoint == 0) {
            player.RemoveTag(TimerTag);
        }
                
        ParkourRespawnEvent respawnEvent = new() {
            Player = player,
            World = player.World.ThrowIfNull(),
            Game = this,
            Checkpoint = currentCheckpoint
        };
        World.Events.CallEvent(respawnEvent);
    }

    public void Reset(PlayerEntity player) {
        player.SetTag(CheckpointTag, 0);
        Respawn(player);
    }
    
    private static Vec3<int>[] GetPlayerCollidingBlocks(PlayerEntity player, Vec3<double> pos) {
        HashSet<Vec3<int>> blocks = [];

        double playerX = player.BoundingBox.Size.X;
        double playerZ = player.BoundingBox.Size.Z;
        
        // add all 4 corners
        blocks.Add(new Vec3<int>((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z)));
        blocks.Add(new Vec3<int>((int)Math.Floor(pos.X + playerX), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z)));
        blocks.Add(new Vec3<int>((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z + playerZ)));
        blocks.Add(new Vec3<int>((int)Math.Floor(pos.X + playerX), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z + playerZ)));
        
        return blocks.ToArray();
    }
}
