﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using ProtoBuf;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vintagestory.API.Config;

namespace storagecontroller
{
    public class StorageControllerMaster:BlockEntityGenericTypedContainer
    {
        public static string containerlistkey = "containerlist";
        List<BlockPos> containerlist; //TODO: add to treeattributes
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }
        }

        //stuff to do every so often
        public void OnServerTick(float dt)
        {
            //Check if we have any inventory to bother with
            if (this.Inventory == null || this.Inventory.Empty) { return; }
            if (containerlist==null||containerlist.Count==0) { return; }
            //Manage linked container list
            // - only check so many blocks per tick
            List<BlockPos> prunelist = new List<BlockPos>(); //This is a list of invalid blockpos that should be deleted from list
            
            if (containerlist != null) {
                
                foreach (BlockPos pos in containerlist)
                {
                    BlockEntity be= Api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer;
                    BlockEntityContainer thiscont = be as BlockEntityContainer;
                    //no valid container here so set to remove this location ***Should we do that or just let new containers be placed later??
                    if (thiscont == null)
                    {
                        prunelist.Add(pos);
                    }
    
                }
                
                foreach(BlockPos pos in prunelist)
                {
                    containerlist.RemoveAll(x=>x.Equals(pos));
                }
            }
            //Need to populate containers if this container has inventory
            // - from list of container first find a stack or locked container with inventory
            // - then push in inventory if possible
            // - otherwise look for empty slots we can put our inventory into
            // TO FIX:
            //  - issues with crates:
            //     - crates do have multiple stacks, and will let you force in items, but really everything should be the same stack

            List<ItemSlot> populatedslots = new List<ItemSlot>();
            List<ItemSlot> emptyslots = new List<ItemSlot>();
            Dictionary<ItemSlot,BlockEntityContainer>slotreference=new Dictionary<ItemSlot,BlockEntityContainer>();
            List<ItemSlot> slotiscrate = new List<ItemSlot>(); //this records slots that are part of a crate for checks
            foreach (BlockPos p in containerlist)
            {
                BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(p) as BlockEntityContainer;
                BlockEntityContainer cont = be as BlockEntityContainer;
                BlockEntityCrate crate = be as BlockEntityCrate;
                if (cont == null||cont.Inventory==null) { continue; }

                    //if the inventory is empty we'll just add all the slots to emptyslots, not sure if this is any more efficient
                    if (cont.Inventory.Empty)
                    {
                        foreach (ItemSlot slot in cont.Inventory)
                        {
                            emptyslots.Add(slot);
                            slotreference[slot] = cont;
                        }
                    }
                    else
                    {
                        foreach (ItemSlot slot in cont.Inventory)
                        {
                            if (slot == null || slot.Inventory == null) { continue; }
                            //add empty slots
                            if (slot.Empty || slot.Itemstack == null) {
                                if (crate != null) { slotiscrate.Add(slot); }
                                emptyslots.Add(slot);
                                slotreference[slot]=cont; 
                            }
                            //ignore full slots
                            else if (slot.Itemstack.StackSize >= slot.MaxSlotStackSize) { continue; }
                            //this is a filled slot with space so add it
                            else { populatedslots.Add(slot); slotreference[slot] = cont; }
                        }
                    }
                
            }
            
            //now we need to try and move out inventory
            foreach (ItemSlot ownslot in Inventory)
            {
                //skip empty slots
                if (ownslot==null||ownslot.Itemstack==null|| ownslot.Empty) { continue; }

                var validslots = populatedslots.Where(x => x.Itemstack.Collectible == ownslot.Itemstack.Collectible).ToList<ItemSlot>();
                bool placedsome = false;
                if (validslots != null && validslots.Count > 0)
                {
                    foreach (ItemSlot validslot in validslots)
                    {
                        if (validslot != null)
                        {
                            //ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, EnumModifierKey.SHIFT, EnumMergePriority.DirectMerge);
                            int startamt = ownslot.StackSize;
                            ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, ownslot.StackSize);
                            int rem = ownslot.TryPutInto(validslot, ref op);
                            if (ownslot.StackSize != startamt)
                            {

                                if (rem == 0) { ownslot.Itemstack = null; }
                                MarkDirty(false);
                                validslot.MarkDirty();
                                slotreference[validslot].MarkDirty(true);
                                placedsome = true;
                                break; //only do one transfer per tick
                            }
                        }
                    }
                }
                if (placedsome) { return; }
                if (emptyslots != null && emptyslots.Count > 0)
                {
                    foreach (ItemSlot emptyslot in emptyslots)
                    {
                        placedsome = false;
                        //for crates we have to verify that the empty slots are valid
                        if (slotiscrate.Contains(emptyslot))
                        {
                            
                            if (!slotreference[emptyslot].Inventory.Empty)
                            {
                                //the slot is empty but the containing crate doesn't match items so skip
                                if (slotreference[emptyslot].Inventory[0].Itemstack.Collectible != ownslot.Itemstack.Collectible){
                                    continue;
                                }
                            }
                        }
                        int startamt = ownslot.StackSize;
                        ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, ownslot.StackSize);
                        int rem = ownslot.TryPutInto(emptyslot, ref op);
                        
                        if (ownslot.StackSize != startamt)
                        {
                            placedsome = true;
                            if (rem == 0) { ownslot.Itemstack = null; }
                            MarkDirty(false);
                            emptyslot.MarkDirty();
                            slotreference[emptyslots[0]].MarkDirty(true);
                            break ; //only do one transfer per tick
                        }
                    }
                }
                if (placedsome) { return; }
            }
        }

        //add a container to the list of managed containers (usually called by a storage linker)
        public void AddContainer(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            //don't want to link to ourself!
            if (blockSel.Position == this.Pos) { return; } 
            //check for valid container
            BlockEntityContainer cont = Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityContainer;
            if (cont == null) { return; }
            //ensure block isn't reinforced

            //ensure player as access rights
            
            //if container isn't on list then add it
            if (containerlist==null) { containerlist = new List<BlockPos>();}
            if (!containerlist.Contains(blockSel.Position)) {
                if (Api is ICoreServerAPI)
                {
                    containerlist.Add(blockSel.Position); MarkDirty();
                }
            }

        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            var asString = tree.GetString(containerlistkey);
            if (asString!=null)
            {
                containerlist=JsonConvert.DeserializeObject<List<BlockPos>>(asString);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            var asString = JsonConvert.SerializeObject(containerlist);
            tree.SetString(containerlistkey, asString);
            base.ToTreeAttributes(tree);
        }
    }
}
