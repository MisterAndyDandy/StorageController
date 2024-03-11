﻿using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System;
using Vintagestory.GameContent;
using Newtonsoft.Json.Linq;
using System.Numerics;
using System.Linq;

namespace storagecontroller
{
    public class GUIDialogStorageAccess : GuiDialogBlockEntity
    {
        ICoreClientAPI clientAPI;

        ElementBounds mainDialogBound;

        ElementBounds gridSlots;

        DummyInventory virtualInventory;

        protected int curTab;

        StorageControllerMaster storageControllerMaster;
        public GUIDialogStorageAccess(string dialogTitle, StorageControllerMaster ownBlock, DummyInventory inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (IsDuplicate)
            {
                return;
            }

            clientAPI = capi;

            curTab = 1;

            virtualInventory = inventory;

            storageControllerMaster = ownBlock;

            SetupDialog();
        }


        public void SetupDialog()
        {
            ComposersDialog();
        }

        public string storageCompKey => "storageCompo";
        public string optionCompKey => "storageOptionCompo";

        protected void ComposersDialog()
        {

        
            ElementBounds element = ElementBounds.Fixed(0, 0, 500, 400);
            ElementBounds buttonlist = ElementBounds.Fixed(0, 0, -200, -390).FixedUnder(element, 10);
            gridSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, 10)
                .FixedUnder(element, 10)
                .WithFixedPosition(-5, 25)
                .WithFixedPadding(2, 2);

            ElementBounds button1 = ElementBounds.Fixed(100, -25) //Clear All
                .FixedUnder(buttonlist, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button2 = ElementBounds.Fixed(185, -10) //Link All Chests
                .FixedUnder(button1, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button3 = ElementBounds.Fixed(312, -10) //Highlight
                .FixedUnder(button2, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button4 = ElementBounds.Fixed(415, -10) //Highlight
                .FixedUnder(button3, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button5 = ElementBounds.Fixed(450, -10) //Highlight
                .FixedUnder(button3, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);


            ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds7.BothSizing = ElementSizing.FitToChildren;
            elementBounds7.WithChildren(element, buttonlist, gridSlots);

            mainDialogBound = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftFixed).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
            Composers[optionCompKey] =
                capi.Gui
                .CreateCompo(optionCompKey, mainDialogBound)
                .AddShadedDialogBG(elementBounds7)
                .AddDialogTitleBar("Controls", CloseIconPressed)
                .AddButton("Clear All", () => { return OnClickClearAll(); }, button1, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "clearactive")
                .AddAutoSizeHoverText("Clears all connections from controller", CairoFont.WhiteSmallText(), 300, button1)
                .AddIf(storageControllerMaster?.ContainerList?.Count > 1)
                .AddButton("Clear All", () => { return OnClickClearAll(); }, button1, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "cleardeactivate")
                .AddAutoSizeHoverText("Clears all connections from controller", CairoFont.WhiteSmallText(), 300, button1)
                .EndIf()
                .AddButton("Link All Chests", () => { return OnClickLinkAllChests(); }, button2, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "linkbutton")
                .AddAutoSizeHoverText("Links all chests in range", CairoFont.WhiteSmallText(), 300, button2)
                .AddButton("Highlight", () => { return OnClickHighlightAttached(); }, button3, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "highlightbutton")
                .AddAutoSizeHoverText("Highlight linked containers and range", CairoFont.WhiteSmallText(), 300, button3)
                .AddButton("<", PreviousPage, button4, CairoFont.WhiteSmallText(), EnumButtonStyle.Small)
                .AddButton(">", NextPage, button5, CairoFont.WhiteSmallText(), EnumButtonStyle.Small)
                .Compose();

            var compKey = Composers[storageCompKey] = capi.Gui.CreateCompo(storageCompKey, mainDialogBound);


            if (virtualInventory != null && !virtualInventory.Empty)
            {
                compKey
                .AddItemSlotGrid(virtualInventory, SendPacket, 10, gridSlots, "slotgrid")
                .AddInset(gridSlots, 10, 0.7f)
                .Compose();

                //Fixed number are slot showed
                Composers[storageCompKey].GetSlotGrid("slotgrid")
                    .DetermineAvailableSlots(Enumerable.Range(0, 100).ToArray());
            }
            else 
            {
                compKey
                .AddInset(gridSlots, 10, 0.7f)
                .Compose();
            }

         
            compKey.Compose(true);
            Composers[optionCompKey].Compose(true);
        }


        private void SendPacket(object obj)
        {
            IClientPlayer byPlayer = capi.World.Player;

            foreach (ItemStack liststacks in storageControllerMaster.ListStacks) 
            {
                if (byPlayer.InventoryManager.MouseItemSlot.Itemstack.Id == liststacks.Id)
                {
                    byPlayer.InventoryManager.MouseItemSlot.Itemstack = null;
                    byte[] data = liststacks?.ToBytes();
                    if (data != null)
                    {
                        clientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.inventoryPacket, data);
                    
                        break;
                    }
                };
            }

        }


        private void GridPage()
        {
            int slots = virtualInventory.Count;
            int startIndex = (curTab - 1) * 50; // Calculate the starting slot index
            startIndex = Math.Max(0, startIndex); // Ensure startIndex doesn't go below 0
            startIndex = Math.Min(slots - 50, startIndex); // Ensure startIndex doesn't exceed the maximum index - 50

            if (startIndex >= 0)
            {
                Composers[storageCompKey].GetSlotGrid("slotgrid")
                    .DetermineAvailableSlots(Enumerable.Range(startIndex, 100).ToArray());
            }
            Composers[storageCompKey].Compose();
        }



        private bool PreviousPage()
        {
            this.curTab--;
            this.curTab = Math.Max(1, curTab); // Ensure curTab doesn't go below 1
            GridPage();
            return true;
        }

        private bool NextPage()
        {
            curTab++; // Increase curTab by 1
            GridPage(); // Update the grid
            return true;
        }

        private bool OnClickClearAll()
        {
            clientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.clearInventoryPacket, null);
            storageControllerMaster?.SystemInventory?.Clear();
            storageControllerMaster?.ClearHighlighted();
            storageControllerMaster?.MarkDirty();
            SetupDialog();
            return true;
        }

        private bool OnClickLinkAllChests()
        {
            TryClose();
            storageControllerMaster?.LinkAll(StorageControllerMaster.enLinkTargets.ALL, clientAPI.World.Player);
            return true;
        }
        private bool OnClickHighlightAttached()
        {
            if (storageControllerMaster != null)
            {
                storageControllerMaster.ToggleHightlights();
            }

            return true;
        }

    }
}
