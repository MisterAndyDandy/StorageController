using System.Collections.Generic;
using Microsoft.VisualBasic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static System.Reflection.Metadata.BlobBuilder;

namespace storagecontroller
{
    public class ItemStorageLinker : Item
    {
        public static string posValue = "linkto";

        public static string linkedValue = "linktodesc";

        public WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {

            if (api.Side != EnumAppSide.Client)
            {
                return;
            }
            _ = api;

            interactions = ObjectCacheUtil.GetOrCreate(api, "linkerInteractions", delegate
            {
                List<ItemStack> itemStacks = new List<ItemStack>();


                foreach (Block block in api.World.Blocks)
                {
                    if (block is BlockStorageController)
                    {
                        itemStacks.Add(new ItemStack(block));
                    }
                }

                return new WorldInteraction[2]
                {
                        new WorldInteraction
                        {
                            ActionLangCode = "storagecontroller:heldhelp-linker",
                            MouseButton = EnumMouseButton.Left
                        },
                        new WorldInteraction
                        {
                            ActionLangCode = "storagecontroller:heldhelp-linker-highlight",
                            MouseButton = EnumMouseButton.Left,
                            HotKeyCode = "ctrl",
                            Itemstacks = itemStacks.ToArray(),
                        }
                };
            });

        }

        public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
        {
            return "interactstatic";
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null || slot == null) return;

            if (byEntity is not EntityPlayer entityPlayer) return;

            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
                return;

            if (!byEntity.World.Claims.TryAccess(entityPlayer?.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                return;

            if (slot.Empty || slot.Itemstack == null) return;

            BlockEntity targetEntity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);

            handling = EnumHandHandling.PreventDefault;

            ITreeAttribute attributes = slot.Itemstack.Attributes;

            // maybe have to move it over to OnPlayerRightClick on BlockEntityStorageController
            if (entityPlayer.Controls.CtrlKey)
            {
                if (api is ICoreClientAPI)
                {   // if player is pressing down ctrlkey and is looking at Storage Controller show high light blocks.
                    if (targetEntity is BlockEntityStorageController blockEntityStorageController)
                    {
                        blockEntityStorageController.ShowHighLight = !blockEntityStorageController.ShowHighLight;
                        blockEntityStorageController.ToggleHighLight(entityPlayer.Player, blockEntityStorageController.ShowHighLight);
                        return;
                    }

                    if (targetEntity is BlockEntitySignalTower) 
                    {
                        //add it own show highlight blocks.
                        return;
                    }
                }
            }
            else
            {   // If the block is a storage controller or signal tower, set it as the target.
                if (targetEntity is BlockEntityStorageController)
                {
                    attributes.SetBlockPos(posValue, blockSel.Position);
                    attributes.SetString(linkedValue, blockSel.Position.ToLocalPosition(api).ToString());
                    slot.MarkDirty();
                    return;
                }

                // Quit if attributes is not set
                if (!attributes.HasAttribute(posValue + "X"))
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(!attributes.HasAttribute(posValue + "X"), $"Use {slot.Itemstack.GetName()} on storage controller first", Lang.Get("storagecontroller:helditem-error-linker {0}", slot.Itemstack.GetName()));
                    return;
                }

                if (targetEntity is BlockEntitySignalTower blockEntitySignalTower) 
                {
                    blockEntitySignalTower.StorageControllerPos = attributes.GetBlockPos(posValue);
                    (api as ICoreClientAPI)?.ShowChatMessage($"Storage Controller is linked to {blockEntitySignalTower.Block.GetPlacedBlockName(api.World, blockEntitySignalTower.Pos)}");
                }

                // Check for valid Type
                BlockPos Pos = slot.Itemstack.Attributes.GetBlockPos(posValue);

                if (Pos == null)
                    return;

                if (api.World.BlockAccessor.GetBlockEntity(Pos) is BlockEntityStorageController blockEntityStorageController)
                {
                    blockEntityStorageController.ToggleContainer(byEntity, blockSel);
                }
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            if (itemStack != null && itemStack.Attributes.HasAttribute(linkedValue))
            {

                return $"Storage Linker (Linked to ({itemStack.Attributes.GetString(linkedValue)})";
            }

            return base.GetHeldItemName(itemStack);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
