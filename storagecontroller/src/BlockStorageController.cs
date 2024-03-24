using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace storagecontroller
{
    internal class BlockStorageController: BlockGenericTypedContainer
    {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {

            BlockEntityStorageController forstm = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityStorageController;
            if (forstm != null)
            {
                ItemStack itemStack = new ItemStack(world.BlockAccessor.GetBlock(pos),1);
                byte[] data = SerializerUtil.Serialize(forstm.ContainerList);
                if (data != null)
                {
                    itemStack.Attributes.SetBytes(BlockEntityStorageController.containerlistkey, data);
                    return new ItemStack[] { itemStack };
                }
            }
            
            return base.GetDrops(world,pos,byPlayer, dropQuantityMultiplier);
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            BlockEntityStorageController blockEntityStorageController = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityStorageController;
            if (blockEntityStorageController != null)
            {
                ItemStack itemStack = new ItemStack(world.BlockAccessor.GetBlock(pos), 1);
                byte[] data = SerializerUtil.Serialize(blockEntityStorageController.ContainerList);
                if (data != null)
                {
                    itemStack.Attributes.SetBytes(BlockEntityStorageController.containerlistkey, data);
                    return itemStack;
                }
            }
            return base.OnPickBlock(world, pos);
        }

        //check to see if we are in range of the linked blocks.
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel?.Position == null)
                return false;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityStorageController entityStorageController)
                return false;

            //let upgrade controller.
            if (TryApplyUpgrade(world, byPlayer, entityStorageController))
            {
                return true;
            }

            if (byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible is ItemStorageLinker) {
                return false;
            }
            
            return entityStorageController.OnPlayerRightClick(byPlayer, blockSel);
        }

        private float meshangle = 0f;

        private string type = string.Empty;

        private List<BlockPos> Pos = new List<BlockPos>();

        private bool TryApplyUpgrade(IWorldAccessor world, IPlayer byPlayer, BlockEntityStorageController storageControllerMaster)
        {
            if (byPlayer.InventoryManager.ActiveHotbarSlot?.Empty == null ? false : true)
                return false;

            ItemStack hotbarItemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

            // Check to see if this is a storagecontrollupgrade if not return false.
            if (!hotbarItemStack.Collectible.Code.FirstCodePart().Equals("storagecontrollerupgrade")) return false;

            // Check if the itemstack is null or if it has a 'tier' attribute
            if (hotbarItemStack == null || !hotbarItemStack.Collectible.Attributes?.KeyExists("tier") == true)
                return false;

            string[] tierArray = hotbarItemStack.Collectible.Attributes["tier"].AsArray<string>();

            // Look for a match between the tiers
            string metaltype = string.Empty;

            // Iterate through each tier in the array
            foreach (string tier in tierArray)
            {
                // Check if the tier matches the code's first part
                if (tier.Equals(Code.FirstCodePart()))
                {
                    // Extract the metal type from the item's code
                    metaltype = hotbarItemStack.Collectible.Code.EndVariant();
                }
            }

            if (string.IsNullOrEmpty(metaltype))
                return false; // No matching metal type found

            // Construct the name for the upgraded block
            string upgradeBlockName = "storagecontroller:" + "storagecontroller" + metaltype + "-" + Code.EndVariant();

            // Make sure it's not the same block
            if (upgradeBlockName.Equals(Code.ToString()))
                return false; // Storage controller already at the highest tier

            // Check if the block exists
            meshangle = storageControllerMaster.MeshAngle;
            type = storageControllerMaster.type;
            BlockGenericTypedContainer block = world.BlockAccessor.GetBlock(new AssetLocation(upgradeBlockName)) as BlockGenericTypedContainer;
            if (block == null)
                return false; // Block not found

            // Make sure if there is anything in Inventory it will get drop before upgrading
            storageControllerMaster.Inventory.DropAll(byPlayer.Entity.Pos.AsBlockPos.ToVec3d());

            // Let see if we got any linked Containers
            Pos = storageControllerMaster.ContainerList;

            // Set the block
            api.World.BlockAccessor.SetBlock(block.Id, byPlayer.CurrentBlockSelection.Position);
            BlockEntityStorageController storageController = api.World.BlockAccessor.GetBlockEntity(byPlayer.CurrentBlockSelection.Position) as BlockEntityStorageController;

            //Make sure angle is the same as before and same for type.
            storageController.MeshAngle = meshangle;
            storageController.type = type;


            // Let add linked Container back to the new block
            storageController.SetContainers(Pos);

            if (api is ICoreClientAPI capi)
            {
                capi.World.PlaySoundAt(new AssetLocation("game:sounds/tool/reinforce"), byPlayer, capi.World.Player);
            }

            // Consume one item from the player's hand
            byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            byPlayer.InventoryManager.BroadcastHotbarSlot();

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (pos == null) return;

            BlockEntityStorageController blockEntityStorageController = (api as ICoreClientAPI)?.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityStorageController;

            if (blockEntityStorageController != null) {
                blockEntityStorageController.ClearHighlighted(byPlayer);
            }


            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}
