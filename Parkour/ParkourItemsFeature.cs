using ManagedServer.Events;
using ManagedServer.Features;
using Minecraft.Data.Generated;
using Minecraft.Schemas.Items;
using Parkour.Events;

namespace Parkour;

public class ParkourItemsFeature(ParkourGame game) : ScopedFeature {
    private static readonly ItemStack RespawnItem = new ItemStack(Item.Stick)
        .With(DataComponent.ItemName, "Respawn");
    private static readonly ItemStack RestartItem = new ItemStack(Item.DeadBush)
        .With(DataComponent.ItemName, "Restart");
    private static readonly ItemStack PlayerVisibilityItem = new ItemStack(Item.EnderEye)
        .With(DataComponent.ItemName, "Toggle Player Visibility");
    private static readonly ItemStack LeaveItem = new ItemStack(Item.JungleDoor)
        .With(DataComponent.ItemName, "Leave");
    
    public override void Register() {
        game.World.Events.AddListener<PlayerEnteringWorldEvent>(e => {
            // give item
            e.Player.Inventory.AddItem(RespawnItem);
            e.Player.Inventory.AddItem(RestartItem);
            e.Player.Inventory.AddItem(LeaveItem);
        });

        game.World.Events.AddListener<PlayerUseItemEvent>(e => {
            if (e.Item == RespawnItem) {
                game.Respawn(e.Player);
            }
            else if (e.Item == RestartItem) {
                game.Reset(e.Player);
            }
            else if (e.Item == PlayerVisibilityItem) {
                
            }
            else if (e.Item == LeaveItem) {
                ParkourLeaveEvent leaveEvent = new() {
                    Player = e.Player,
                    Game = game
                };
                game.World.Events.CallEvent(leaveEvent);
            }
        });

        game.World.Events.AddListener<PlayerDropItemEvent>(e => e.Cancelled = true);
    }
}
