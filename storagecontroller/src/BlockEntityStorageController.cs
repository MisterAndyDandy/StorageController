using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Newtonsoft.Json;
using System.Reflection;
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace storagecontroller
{
    public class BlockEntityStorageController : BlockEntityGenericTypedContainer
    {
        /// <summary>
        /// TODO Bugs
        /// CRATES - won't fill up crates properly with same item
        /// </summary>
        public static string containerlistkey = "containerlist";

        private List<BlockPos> containerlist;

        public List<BlockPos> ContainerList => containerlist;

        private List<string> supportedChests;

        public virtual List<string> SupportedChests => supportedChests;

        private List<string> supportedCrates;

        public virtual List<string> SupportedCrates => supportedCrates;

        private int tickTime = 250;
        public virtual int TickTime => tickTime; //how many ms between ticks

        private int maxTransferPerTick = 1;
        public virtual int MaxTransferPerTick => maxTransferPerTick; // The maximum of items to transfer

        private int maxRange = 10;
        public virtual int MaxRange => maxRange; //maximum distance (in blocks) that this controller will link to

        public int MaxPlayerRange => MaxRange + MaxRange;

        //bool dopruning = false; //should invalid locations be moved every time?

        public float lastSecondsPast;

        private GUIDialogStorageAccess clientDialog;

        internal StorageVirtualInv storageVirtualInv;

        public bool ShowHighLight = false;

        public virtual StorageVirtualInv StorageVirtualInv => storageVirtualInv;

        private HashSet<ItemStack> allItemStackSet;

        private HashSet<ItemStack> AllItemStackSet
        {
            get => allItemStackSet;
            set => allItemStackSet = value;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            supportedChests = new List<string> { "GenericTypedContainer", "BEGenericSortableTypedContainer", "BESortableLabeledChest", "LabeledChest", "StorageControllerMaster" };
            supportedCrates = new List<string> { "BBetterCrate", "BEBetterCrate", "Crate" };

            if (Block.Attributes != null)
            {
                maxTransferPerTick = Block.Attributes["maxTransferPerTick"].AsInt(maxTransferPerTick);
                maxRange = Block.Attributes["maxRange"].AsInt(maxRange);
                tickTime = Block.Attributes["tickTime"].AsInt(TickTime);
            }

            if (Api is ICoreServerAPI ICoreServerAPI)
            {
                RegisterGameTickListener(OnServerTick, TickTime);
            }
            else if (Api is ICoreClientAPI ICoreClientAPI)
            {
                RegisterGameTickListener(OnClientTick, 200);
            }
        }

        private void OnClientTick(float dt)
        {
            IPlayer byPlayer = (Api as ICoreClientAPI)?.World.Player;

            if (byPlayer == null) return;

            IInventory playeHotBar = byPlayer.InventoryManager.GetHotbarInventory();

            IInventory playerInv = byPlayer.InventoryManager.GetInventory(GlobalConstants.characterInvClassName);

            if (playerInv != null && playerInv.Empty)
            {
                foreach (ItemSlot invSlots in playerInv)
                {
                    if (invSlots.Empty) continue;

                    if (invSlots.Itemstack.Collectible is ItemStorageLinker)
                    {
                        if (invSlots.Itemstack.Attributes.HasAttribute(ItemStorageLinker.posValue + "X"))
                        {
                            if (Pos == invSlots.Itemstack.Attributes.GetBlockPos(ItemStorageLinker.posValue))
                            {
                                if (!IsPlayerInRange(byPlayer.Entity.Pos.AsBlockPos))
                                {
                                    ShowHighLight = false;
                                    ClearHighlighted(byPlayer);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (playeHotBar != null)
            {
                foreach (ItemSlot hotBarSlots in playeHotBar)
                {
                    if (hotBarSlots.Empty) continue;

                    if (hotBarSlots.Itemstack.Collectible is ItemStorageLinker)
                    {
                        if (hotBarSlots.Itemstack.Attributes.HasAttribute(ItemStorageLinker.posValue + "X"))
                        {
                            if (Pos == hotBarSlots.Itemstack.Attributes.GetBlockPos(ItemStorageLinker.posValue))
                            {
                                if (!IsPlayerInRange(byPlayer.Entity.Pos.AsBlockPos))
                                {
                                    ShowHighLight = false;
                                    ClearHighlighted(byPlayer);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        //Better crates: BBetterCrate, BEBetterCrate, Crate, GenericTypedContainer

        //stuff to do every so often
        public void OnServerTick(float dt)
        {
            try
            {
                //Check if we have any inventory to bother with
                if (Inventory == null || Inventory.Empty)
                {
                    return;
                }

                if (ContainerList == null || ContainerList.Count == 0 || SupportedChests == null)
                {
                    return;
                }

                //Manage linked container list
                //// - only check so many blocks per tick
                //List<BlockPos> prunelist = new List<BlockPos>(); //This is a list of invalid blockpos that should be deleted from list

                //foreach (BlockPos pos in containerlist)
                //{

                //    if (pos == null || Api == null || Api.World == null || Api.World.BlockAccessor == null) { continue; }
                //    if (!IsInRange(pos)) { prunelist.Add(pos); continue; }
                //    Block b = Api.World.BlockAccessor.GetBlock(pos);
                //    if (b == null || b.EntityClass == null) { prunelist.Add(pos); continue; }
                //    if (!(SupportedChests.Contains(b.EntityClass) || SupportedCrates.Contains(b.EntityClass)))
                //    {
                //        prunelist.Add(pos); continue;
                //    }
                //    BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
                //    if (be == null || !(be is BlockEntityContainer)) { prunelist.Add(pos); continue; }
                //}
                //int removecount = 0;

                //if (dopruning)
                //{
                //    foreach (BlockPos pos in prunelist)
                //    {
                //        if (pos == null) { continue; }
                //        removecount += containerlist.RemoveAll(x => x.Equals(pos));

                //    }
                //    if (removecount > 0) { MarkDirty(); }
                //}
                //if (containerlist == null || containerlist.Count == 0) { return; }

                //Priority slots: filtered crate slots with space, other populated crates with space
                Dictionary<ItemStack, List<ItemSlot>> priorityslots = new Dictionary<ItemStack, List<ItemSlot>>();
                //Partial Chest slots - slots in chests that have space
                List<ItemSlot> populatedslots = new List<ItemSlot>();
                //Empty slots with nothing, including the first slot of an empty, unfiltered crate
                List<ItemSlot> emptyslots = new List<ItemSlot>();
                //This slotreference is to match the slot back up to its originating container
                Dictionary<ItemSlot, BlockEntityContainer> slotreference = new Dictionary<ItemSlot, BlockEntityContainer>();
                //Cycle thru our blocks to find containers we can use
                foreach (BlockPos pos in ContainerList)
                {
                    if (pos == null) { continue; }
                    BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(pos);
                    Block block = Api.World.BlockAccessor.GetBlock(pos);
                    BlockEntityContainer blockEntityContainer = blockEntity as BlockEntityContainer;

                    if (blockEntity == null || blockEntityContainer == null || blockEntityContainer.Inventory == null) { continue; }//this shouldn't happen, but better to check
                                                                                                                                    //find better crates
                    FieldInfo bettercratelock = blockEntity.GetType().GetField("lockedItemInventory");
                    //ah, we have discovered a better crate!
                    //HANDLE BETTER CRATES
                    if (bettercratelock != null)
                    {
                        InventoryGeneric bettercratelockingslot = bettercratelock.GetValue(blockEntity) as InventoryGeneric;
                        bool lockedcrate = false;
                        bool emptycrate = false;
                        ItemStack inslot = null;
                        //if there's a valid lock set to inslot
                        if (bettercratelockingslot != null && bettercratelockingslot[0] != null && bettercratelockingslot[0].Itemstack != null)
                        {
                            lockedcrate = true;
                            inslot = bettercratelockingslot[0].Itemstack.GetEmptyClone();
                        }
                        else if (blockEntityContainer.Inventory == null || blockEntityContainer.Inventory.Empty) { emptycrate = true; }
                        else if (blockEntityContainer.Inventory[0] == null || blockEntityContainer.Inventory[0].Itemstack == null) { emptycrate = true; }//Hmmm this is an odd situation
                        else { inslot = blockEntityContainer.Inventory[0].Itemstack.GetEmptyClone(); } //otherwise set inslot to the first item in crate
                        //case one - filtered or not empty - add first slot with space to priority list
                        if (lockedcrate || !emptycrate)
                        {
                            foreach (ItemSlot crateslot in blockEntityContainer.Inventory)
                            {
                                if (crateslot.StackSize < inslot.Collectible.MaxStackSize)
                                {
                                    if (priorityslots.ContainsKey(inslot))
                                    {
                                        priorityslots[inslot].Add(crateslot);
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                    else
                                    {
                                        priorityslots[inslot] = new List<ItemSlot> { crateslot };
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                }
                            }
                        }
                        //If create is empty and unfiltered then we add first slot to empty slots
                        else if (emptycrate)
                        {
                            emptyslots.Add(blockEntityContainer.Inventory[0]);
                            slotreference[blockEntityContainer.Inventory[0]] = blockEntityContainer;
                        }
                        ///*** ADD NONE EMPTY CRATE CODE ***

                    }
                    //NOT A BETTER CRATE So check if it's another crate and deal with it accordingly
                    else if (SupportedCrates.Contains(block.EntityClass))
                    {
                        //add to empty list if empty
                        if (blockEntityContainer.Inventory.Empty)
                        {
                            emptyslots.Add(blockEntityContainer.Inventory[0]);
                            slotreference[blockEntityContainer.Inventory[0]] = blockEntityContainer;
                        }
                        else
                        {
                            //use the contents of the first slot to set what this crate should contain
                            ItemStack inslot = blockEntityContainer.Inventory[0].Itemstack.GetEmptyClone();
                            foreach (ItemSlot crateslot in blockEntityContainer.Inventory)
                            {
                                //if (crateslot.Itemstack == null || crateslot.Itemstack.Collectible == null) { continue; }
                                if (crateslot.StackSize < inslot.Collectible.MaxStackSize)
                                {
                                    if (priorityslots.ContainsKey(inslot))
                                    {
                                        priorityslots[inslot].Add(crateslot);
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                    else
                                    {
                                        priorityslots[inslot] = new List<ItemSlot> { crateslot };
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    //last of all deal with chests - slot by slot
                    else if (supportedChests.Contains(block.EntityClass))
                    {
                        foreach (ItemSlot slot in blockEntityContainer.Inventory)
                        {
                            if (slot == null || slot.Inventory == null) { continue; }
                            //add empty slots
                            if (slot.Empty || slot.Itemstack == null)
                            {
                                emptyslots.Add(slot);
                                slotreference[slot] = blockEntityContainer;
                            }
                            //ignore full slots
                            else if (slot.Itemstack.StackSize >= slot.Itemstack.Collectible.MaxStackSize) { continue; }
                            //this is a filled slot with space so add it
                            else { populatedslots.Add(slot); slotreference[slot] = blockEntityContainer; }
                        }

                    }

                }

                //NEXT CYCLE THRU OWN STACKS AND DISTRIBUTE
                //  *Note we only do one transfer operation per tick, so the first successful one gets done then it returns
                foreach (ItemSlot ownslot in Inventory)
                {
                    //skip empty slots
                    if (ownslot == null || ownslot.Itemstack == null || ownslot.Empty) { continue; }
                    ItemStack myitem = ownslot.Itemstack.GetEmptyClone();
                    if (myitem == null) { continue; }

                    //start trying to find an empty slot
                    ItemSlot outputslot = null;
                    if (priorityslots != null)
                    {
                        List<ItemSlot> foundslots = priorityslots.FirstOrDefault(x => x.Key.Satisfies(myitem)).Value;
                        if (foundslots != null && foundslots.Count > 0)
                        {
                            outputslot = foundslots[0];
                        }
                    }
                    //we didn't find anything in a priority slot, next check for other populated slots to fill in
                    if (outputslot == null)
                    {
                        outputslot = populatedslots.FirstOrDefault(x => (x.Itemstack != null) && (x.Itemstack.Satisfies(myitem)));

                    }
                    //if we didn't anything still, try and find an empty slot
                    if (outputslot == null)
                    {
                        if (emptyslots == null || emptyslots.Count == 0 || emptyslots[0] == null) { continue; } //we found nothing for this object
                        outputslot = emptyslots[0];
                    }

                    //Finally we can attempt to transfer some inventory and then return out of function if sucessful (or move ot next stack)
                    int startamt = ownslot.StackSize;
                    ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, Math.Min(ownslot.StackSize, maxTransferPerTick));
                    int rem = ownslot.TryPutInto(outputslot, ref op);
                    if (ownslot.StackSize != startamt)
                    {

                        if (rem == 0) { ownslot.Itemstack = null; }
                        MarkDirty(false);
                        outputslot.MarkDirty();
                        if (slotreference[outputslot] != null)
                        {
                            slotreference[outputslot].MarkDirty(true);
                        }

                        return;
                    }

                }
            }
            catch (Exception)
            {

            }
        }

        public bool IsPlayerInRange(BlockPos checkpos)
        {
            int xdiff = Math.Abs(Pos.X - checkpos.X);
            if (xdiff >= MaxPlayerRange) { return false; }
            int ydiff = Math.Abs(Pos.Y - checkpos.Y);
            if (ydiff >= MaxPlayerRange) { return false; }
            int zdiff = Math.Abs(Pos.Z - checkpos.Z);
            if (zdiff >= MaxPlayerRange) { return false; }
            return true;
        }

        public bool IsInRange(BlockPos checkpos)
        {
            int xdiff = Math.Abs(Pos.X - checkpos.X);
            if (xdiff >= MaxRange) { return false; }
            int ydiff = Math.Abs(Pos.Y - checkpos.Y);
            if (ydiff >= MaxRange) { return false; }
            int zdiff = Math.Abs(Pos.Z - checkpos.Z);
            if (zdiff >= MaxRange) { return false; }
            return true;
        }

        //Adds a position if not included, or removes it if its
        public void ToggleContainer(EntityAgent byEntity, BlockSelection blockSel)
        {
            if (ContainerList == null)
            {
                containerlist = new List<BlockPos>();
            }

            if (ContainerList.Contains(blockSel.Position))
            {
                RemoveContainer(byEntity, blockSel);
            }
            else
            {
                AddContainer(byEntity, blockSel);
            }
        }

        //add a container to the list of managed containers (usually called by a storage linker)
        public void AddContainer(EntityAgent byEntity, BlockSelection blockSel)
        {
            BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(blockSel.Position);

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

            if (blockEntity is BlockEntityContainer blockEntityContainer)
            {
                // Should we let people do this?

                if (blockEntityContainer is BlockEntityGroundStorage) { return; }

                //if container isn't on list then add it

                if (ContainerList == null)
                {
                    containerlist = new List<BlockPos>();
                }

                if (!ContainerList.Contains(blockSel.Position))
                {
                    ContainerList.Add(blockSel.Position);

                    if (Api is ICoreClientAPI)
                    {
                        HighLightBlocks(byPlayer);
                    }

                    if (Api is ICoreServerAPI)
                    {
                        Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), byPlayer);
                    }

                    MarkDirty();
                }
            }
        }

        //Remove a Container Location from the list
        public void RemoveContainer(EntityAgent byEntity, BlockSelection blockSel)
        {
            if (ContainerList == null) { return; }

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

            if (ContainerList.Contains(blockSel.Position))
            {
                ContainerList.Remove(blockSel.Position);

                if (Api is ICoreClientAPI)
                {
                    HighLightBlocks(byPlayer);
                }

                if (Api is ICoreServerAPI)
                {
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), byPlayer);
                }

                MarkDirty();
            }

        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                SetVirtualInventory();
                toggleInventoryDialogClient(byPlayer, delegate
                {
                    clientDialog = new GUIDialogStorageAccess(DialogTitle, Inventory, StorageVirtualInv, Pos, Api as ICoreClientAPI);
                    return clientDialog;
                });
            }

            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            var asString = tree.GetString(containerlistkey);
            if (asString != null)
            {
                containerlist = JsonConvert.DeserializeObject<List<BlockPos>>(asString);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (byItemStack != null)
            {
                if (byItemStack.Attributes.HasAttribute(containerlistkey))
                {
                    byte[] savedlistdata = byItemStack.Attributes.GetBytes(containerlistkey);
                    if (savedlistdata != null)
                    {
                        List<BlockPos> savedcontainerlist = SerializerUtil.Deserialize<List<BlockPos>>(savedlistdata);
                        if (savedcontainerlist != null)
                        {
                            containerlist = new List<BlockPos>(savedcontainerlist);
                            if (Api is ICoreServerAPI) { MarkDirty(true); }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set a new list of containers (as block positions where those containers should be)
        /// </summary>
        /// <param name="newlist"></param>
        public void SetContainers(List<BlockPos> newlist)
        {
            if (newlist != null)
            {
                containerlist = new List<BlockPos>(newlist);
            }
            else
            {
                containerlist = new List<BlockPos>();
            }

            MarkDirty();
        }

        /// <summary>
        /// Builds a giant virtual inventory of all populated, linked containers
        /// </summary>
        public virtual void SetVirtualInventory()
        {
            HashSet<ItemStack> newItemStackSet = new HashSet<ItemStack>();

            if (ContainerList == null || ContainerList.Count == 0)
            {
                storageVirtualInv = null;
                return;
            }

            // Iterate through each container in the list
            foreach (BlockPos pos in ContainerList)
            {
                BlockEntityContainer blockEntityContainer = Api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer;

                if (blockEntityContainer == null || blockEntityContainer.Inventory == null || blockEntityContainer.Inventory.Empty)
                {
                    continue;
                }

                // Iterate through each slot in the container's inventory
                foreach (ItemSlot slot in blockEntityContainer.Inventory)
                {
                    // Check if the slot contains an item stack
                    if (!slot.Empty && slot.Itemstack != null && slot.StackSize > 0)
                    {
                        // Add the item stack to the new set
                        newItemStackSet.Add(slot.Itemstack);
                    }
                }
            }

            if (!HashSet<ItemStack>.CreateSetComparer().Equals(newItemStackSet, AllItemStackSet))
            {
                // Convert the new set to a list and sort it
                List<ItemStack> allItems = newItemStackSet.OrderBy(item => item.GetName()).ToList();

                // Create a new StorageVirtualInv instance and populate it with the item stacks
                storageVirtualInv = new StorageVirtualInv(Api, allItems.Count);

                for (int i = 0; i < allItems.Count; i++)
                {
                    storageVirtualInv[i].Itemstack = allItems[i].Clone();
                }

                // Update the AllItemStackSet property
                AllItemStackSet = new HashSet<ItemStack>(newItemStackSet.OrderBy(item => item.GetName()));
            }
            else
            {
                // Use the existing sorted list of item stacks
                List<ItemStack> allItems = AllItemStackSet.OrderBy(item => item.GetName()).ToList();

                // Create a new StorageVirtualInv instance and populate it with the item stacks
                storageVirtualInv = new StorageVirtualInv(Api, allItems.Count);

                for (int i = 0; i < allItems.Count; i++)
                {
                    storageVirtualInv[i].Itemstack = allItems[i].Clone();
                }
            }
        }

        public static int itemStackPacket = 320000;
        public static int clearInventoryPacket = 320001;
        public static int linkAllChestsPacket = 320002;
        public static int linkChestPacket = 320003;
        public static int binItemStackPacket = 320004;

        public override void OnReceivedClientPacket(IPlayer byPlayer, int packetid, byte[] data)
        {
            if (packetid == binItemStackPacket)
            {
                byPlayer.InventoryManager.MouseItemSlot.Itemstack = null;
                return;
            }

            //How to handle taking multiple stacks?
            //just search and grab/relieve the first stack we find 
            if (packetid == itemStackPacket)
            {
                if (data == null)
                {
                    return;
                }

                ItemStack virtualStack = new ItemStack(data);

                if (virtualStack == null)
                {
                    return;
                }

                virtualStack.ResolveBlockOrItem(Api.World);

                // we got the stack now let's see if we can send it to the player

                int stacksize = ReturnStack(virtualStack);

                if (stacksize == 0) return;

                virtualStack.StackSize = stacksize;

                //no valid slot
                if (!byPlayer.InventoryManager.TryGiveItemstack(virtualStack, true))
                {
                    Api.World.SpawnItemEntity(virtualStack, byPlayer.Entity.Pos.XYZ);
                }

                return;
            }
            else
            if (packetid == clearInventoryPacket)
            {
                ClearConnections();
                return;
            }
            else
            if (packetid == linkAllChestsPacket)
            {

                LinkAll(enLinkTargets.ALL, byPlayer);
                return;
            }
            else
            if (packetid == linkChestPacket) //link a particular chest
            {
                BlockPos blockPos = SerializerUtil.Deserialize<BlockPos>(data);

                if (blockPos == null || blockPos == Pos)
                {
                    return;
                }

                if (ContainerList == null)
                {
                    containerlist = new List<BlockPos>();
                }

                //do nothing if container in list
                if (ContainerList.Contains(blockPos))
                {
                    return;
                }

                //don't link if container is reinforced
                if (Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockPos) == true)
                {
                    return;
                }

                //ensure player as access rights
                if (!byPlayer.Entity.World.Claims.TryAccess(byPlayer, blockPos, EnumBlockAccessFlags.BuildOrBreak))
                {
                    return;
                }

                // ensure that the storage controller doesn't link to a other storage controller
                if (Api.World.BlockAccessor.GetBlock(blockPos).Equals(Block)) return;

                ContainerList.Add(blockPos);

                MarkDirty();
            }


            base.OnReceivedClientPacket(byPlayer, packetid, data);

        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
        }

        /// <summary>
        /// Attempts to find the item in the connected inventory, relieves it and returns the amount found
        /// </summary>
        /// <param name="findstack"></param>
        /// <returns></returns>
        public int ReturnStack(ItemStack VirtualStack)
        {
            int stacksize = 0;

            foreach (BlockPos blockPos in containerlist)
            {
                if (stacksize != 0) { break; }

                Block block = Api.World.BlockAccessor.GetBlock(blockPos);

                BlockEntityContainer blockEntityContainer = Api.World.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityContainer;

                if (block == null || blockEntityContainer == null || blockEntityContainer.Inventory == null || blockEntityContainer.Inventory.Empty) continue;
                //search inventory of this container if it exists and isn't empty
                foreach (ItemSlot slot in blockEntityContainer.Inventory)
                {
                    if (slot == null || slot.Empty || slot.Itemstack == null || slot.StackSize == 0) { continue; }
                    //if we don't have one yet then add one

                    if (MatchItemStack(slot.Itemstack, VirtualStack)) // < this works
                    {
                        stacksize = slot.Itemstack.StackSize;
                        slot.Itemstack = null;
                        slot.MarkDirty();
                        blockEntityContainer.MarkDirty();
                        break;
                    }
                }
            }

            return stacksize;
        }

        public bool MatchItemStack(ItemStack containerStack, ItemStack virtualStack)
        {
            if (containerStack.Satisfies(virtualStack))
            {
                return true;
            }
            else
            if (containerStack?.ItemAttributes?.Equals(virtualStack?.ItemAttributes) ?? false)
            {
                return true;
            }
            else
            if (containerStack.Id.Equals(virtualStack.Id))
            {
                return true;
            }

            return false;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            var asString = JsonConvert.SerializeObject(containerlist);
            tree.SetString(containerlistkey, asString);
            base.ToTreeAttributes(tree);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);// Adding it to Gui
            dsc.AppendLine($"Range: {MaxRange}");
            if (MaxTransferPerTick <= 512)
            {
                dsc.AppendLine($"Transfer Speed: {MaxTransferPerTick} Items at a time.");
            }
            else
            { dsc.AppendLine("Transfers full Stacks at a time"); }
            if (!(ContainerList == null) && ContainerList.Count > 0)
            {
                dsc.AppendLine($"Linked to {ContainerList.Count} containers.");
            }
            else
            {
                dsc.AppendLine("Not linked to any containers");
            }
        }

        /// <summary>
        /// removes all connections to this controller
        /// </summary>
        public void ClearConnections()
        {
            if (containerlist == null) return;

            containerlist?.Clear();

            MarkDirty(true);
        }

        /// <summary>
        /// Clears all highlighted blocks on client
        /// </summary>
        /// <param name="byPlayer"></param>
        public void ClearHighlighted(IPlayer byPlayer)
        {
            ShowHighLight = false;

            Api.World.HighlightBlocks(byPlayer, 1, new List<BlockPos>());
            Api.World.HighlightBlocks(byPlayer, 2, new List<BlockPos>());
            Api.World.HighlightBlocks(byPlayer, 3, new List<BlockPos>());
            Api.World.HighlightBlocks(byPlayer, 4, new List<BlockPos>());
            Api.World.HighlightBlocks(byPlayer, 5, new List<BlockPos>());
        }

        public void ToggleHighLight(IPlayer byPlayer, bool toggle)
        {
            if (toggle)
            {
                HighLightBlocks(byPlayer);
                return;
            }

            if (!toggle)
            {
                ClearHighlighted(byPlayer);
                return;
            }
        }


        public void HighLightBlocks(IPlayer byPlayer)
        {
            ShowHighLight = true;

            List<int> colors = new List<int>
            {
                ColorUtil.ColorFromRgba(0, 255, 0, 128)
            };

            // If list isn't 0 let show the block that we are linking to the storage controller
            if (ContainerList?.Count > 0)
            {
                Api.World.HighlightBlocks(byPlayer, 1, ContainerList, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
            }
            else // Fixed for when there is one left it won't go away so we will do it like this instead.
            if (ContainerList?.Count == 0)
            {
                Api.World.HighlightBlocks(byPlayer, 1, new List<BlockPos>());
            }

            List<BlockPos> range = new List<BlockPos>
            {
                new BlockPos(Pos.X - maxRange, Pos.Y - maxRange, Pos.Z - maxRange, 0)
            };

            colors[0] = ColorUtil.ColorFromRgba(255, 255, 0, 128);

            range.Add(new BlockPos(Pos.X + maxRange, Pos.Y + maxRange, Pos.Z + maxRange, 0));

            Api.World.HighlightBlocks(byPlayer, 2, range, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);

            colors[0] = ColorUtil.ColorFromRgba(255, 0, 255, 128);

            List<BlockPos> storagecontroller = new List<BlockPos>
            {
                Pos
            };

            Api.World.HighlightBlocks(byPlayer, 3, storagecontroller, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);

            ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

            if (itemStack?.Attributes?.HasAttribute("sigaltowerlinktoX") ?? false)
            {

               BlockPos sigalTowerPos = itemStack?.Attributes.GetBlockPos("sigaltowerlinkto");

                List<BlockPos> sigalTowerRange = new List<BlockPos>
                {
                    new BlockPos(sigalTowerPos.X - 5, sigalTowerPos.Y - 5, sigalTowerPos.Z - 5, 0),
                    new BlockPos(sigalTowerPos.X + 5, sigalTowerPos.Y + 5, sigalTowerPos.Z + 5, 0)
                };

                colors[0] = ColorUtil.ColorFromRgba(255, 200, 0, 128);

                Api.World.HighlightBlocks(byPlayer, 4, sigalTowerRange, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);

                colors[0] = ColorUtil.ColorFromRgba(255, 0, 255, 128);

                List<BlockPos> sigaltower = new List<BlockPos>
                {
                    sigalTowerPos
                };

                Api.World.HighlightBlocks(byPlayer, 5, sigaltower, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);
            }

        }


        public enum enLinkTargets { ALL }
        /// <summary>
        /// Attempt to link all chests in range
        /// will build a list of valid chests
        /// Logic:
        /// - (CLIENT) WalkBlocks which calls LinkChestPos for each block basically
        /// - (CLIENT) LinkChestPos checks for chests and if it finds one sends a message to server for linking
        /// - (SERVER) On Receiving the packet (which encodes the blockpos), the server just ensures that the block
        ///            location is not reinforced or claimed, and if it's ok adds it to the list and then marks the list dirty
        /// </summary>
        /// <param name="targets"></param>
        public void LinkAll(enLinkTargets targets, IPlayer forplayer)
        {
            BlockPos startPos = Pos.Copy();
            startPos.X -= MaxRange;
            startPos.Y -= MaxRange;
            startPos.Z -= MaxRange;
            BlockPos endPos = Pos.Copy();
            endPos.X += MaxRange;
            endPos.Y += MaxRange;
            endPos.Z += MaxRange;

            Api.World.BlockAccessor.WalkBlocks(startPos, endPos, LinkChestPos, true);
        }

        /// <summary>
        /// Check supplied position for relevant blocks and 
        /// if linkable block found send request to link to the server
        /// </summary>
        /// <param name="toblock"></param>
        /// <param name="tox"></param>
        /// <param name="toy"></param>
        /// <param name="toz"></param>
        public void LinkChestPos(Block toblock, int tox, int toy, int toz)
        {
            if (Api is not ICoreClientAPI capi) { return; }
            if (toblock == null) { return; }
            if (toblock.EntityClass == null) { return; }

            if (toblock.EntityClass != "StorageControllerMaster" && !SupportedChests.Contains(toblock.EntityClass) && !SupportedCrates.Contains(toblock.EntityClass)) { return; }
            BlockPos blockPos = new BlockPos(tox, toy, toz, 0);
            byte[] data = SerializerUtil.Serialize(blockPos);
            capi.Network.SendBlockEntityPacket(Pos, linkChestPacket, data);
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            //Api.World.UnregisterGameTickListener(storageInterfaceTickListenerId);

            if (clientDialog?.IsOpened() ?? false)
            {
                clientDialog?.TryClose();
            }

            clientDialog?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (clientDialog?.IsOpened() ?? false)
            {
                clientDialog?.TryClose();
            }

            clientDialog?.Dispose();
        }
    }
}