﻿using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace storagecontroller
{
    internal class BlockStorageController: BlockGenericTypedContainer
    {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            
            StorageControllerMaster forstm = world.BlockAccessor.GetBlockEntity(pos) as StorageControllerMaster;
            if (forstm != null)
            {
                ItemStack itemStack = new ItemStack(world.BlockAccessor.GetBlock(pos),1);
                byte[] data = SerializerUtil.Serialize(forstm.ContainerList);
                if (data != null)
                {
                    itemStack.Attributes.SetBytes(StorageControllerMaster.containerlistkey, data);
                    return new ItemStack[] { itemStack };
                }
            }
            
            return base.GetDrops(world,pos,byPlayer, dropQuantityMultiplier);
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            StorageControllerMaster forstm = world.BlockAccessor.GetBlockEntity(pos) as StorageControllerMaster;
            if (forstm != null)
            {
                ItemStack itemStack = new ItemStack(world.BlockAccessor.GetBlock(pos), 1);
                byte[] data = SerializerUtil.Serialize(forstm.ContainerList);
                if (data != null)
                {
                    itemStack.Attributes.SetBytes(StorageControllerMaster.containerlistkey, data);
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
            if (blockSel?.Position == null || !(byPlayer.Entity is EntityPlayer entityPlayer))
                return false;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not StorageControllerMaster storageControllerMaster)
                return false;

            //let upgrade controller.
            if (TryApplyUpgrade(world, byPlayer, storageControllerMaster))
            {
                return true;
            }

            //open if the player hand is empty
            if(entityPlayer.RightHandItemSlot.Empty) 
            {
                return storageControllerMaster.OnPlayerRightClick(byPlayer, blockSel);
            }
            else
            {
                return false;   
            }

        }

        private bool TryApplyUpgrade(IWorldAccessor world, IPlayer byPlayer, StorageControllerMaster storageControllerMaster)
        {
            if (byPlayer?.InventoryManager?.ActiveHotbarSlot == null)
                return false;

            ItemStack hotbarItemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

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
            string upgradeBlockName = "storagecontroller:" + "storagecontroller" + metaltype + "-east";

            // Make sure it's not the same block
            if (upgradeBlockName.Equals(Code.ToString()))
                return false; // Storage controller already at the highest tier

            // Check if the block exists
            BlockGenericTypedContainer block = world.BlockAccessor.GetBlock(new AssetLocation(upgradeBlockName)) as BlockGenericTypedContainer;
            if (block == null)
                return false; // Block not found

            // Set the block and mark it as dirty
            world.BlockAccessor.SetBlock(block.Id, storageControllerMaster.Pos);
            storageControllerMaster.MarkDirty();

            // Consume one item from the player's hand

            if (api is ICoreClientAPI capi)
            {
                capi.World.PlaySoundAt(new AssetLocation("game:sounds/tool/reinforce"), byPlayer, capi.World.Player);
            }

            byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            byPlayer.InventoryManager.BroadcastHotbarSlot();

            return true;
        }
    }
}
