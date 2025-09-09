using ManagedServer.Events;
using ManagedServer.Features;
using ManagedServer.Viewables;
using Minecraft.Data.Generated;
using Minecraft.Schemas.Items;
using Minecraft.Text;
using Parkour.Events;

namespace Parkour;

public class ParkourItemsFeature(ParkourGame game) : ScopedFeature {
    private static readonly ItemStack RespawnItem = new ItemStack(Item.Stick)
        .With(DataComponent.ItemName, "Respawn");
    private static readonly ItemStack RestartItem = new ItemStack(Item.DeadBush)
        .With(DataComponent.ItemName, "Restart");
    private static readonly ItemStack EnablePlayerVisibilityItem = new ItemStack(Item.EnderPearl)
        .With(DataComponent.ItemName, "Enable Player Visibility");
    private static readonly ItemStack DisablePlayerVisibilityItem = new ItemStack(Item.EnderEye)
        .With(DataComponent.ItemName, "Disable Player Visibility");
    private static readonly ItemStack LeaveItem = new ItemStack(Item.JungleDoor)
        .With(DataComponent.ItemName, "Leave");
    
    public override void Register() {
        game.World.Events.AddListener<PlayerEnteringWorldEvent>(e => {
            // give item
            e.Player.Inventory.AddItem(RespawnItem);
            e.Player.Inventory.AddItem(RestartItem);
            e.Player.Inventory.AddItem(DisablePlayerVisibilityItem);
            e.Player.Inventory.AddItem(LeaveItem);
        });

        game.World.Events.AddListener<PlayerUseItemEvent>(e => {
            if (e.Item == RespawnItem) {
                game.Respawn(e.Player);
            }
            else if (e.Item == RestartItem) {
                game.Reset(e.Player);
            }
            else if (e.Item == DisablePlayerVisibilityItem) {
                ParkourGame.SetPlayerVisibility(e.Player, false);
                e.Player.SetItemInHand(e.Hand, EnablePlayerVisibilityItem);
                e.Player.SendMessage(TextComponent.FromLegacyString("&cYou have disabled player visibility."));
            }
            else if (e.Item == EnablePlayerVisibilityItem) {
                ParkourGame.SetPlayerVisibility(e.Player, true);
                e.Player.SetItemInHand(e.Hand, DisablePlayerVisibilityItem);
                e.Player.SendMessage(TextComponent.FromLegacyString("&aYou have enabled player visibility."));
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
