using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System;
using System.Linq;
using Vintagestory.API.Config;
using System.Collections.Generic;
using Vintagestory.API.Util;

namespace storagecontroller
{
    public class GuiElementIcon : GuiElement
    {
        private readonly LoadedTexture texture;
        public ActionConsumable onClick;
        public Action<int> OnSlotOver;
        public ICoreClientAPI capi;
        public static double unscaledButtonSize = 21.0;

        public GuiElementIcon(ICoreClientAPI capi, LoadedTexture texture, ActionConsumable onClick, ElementBounds bounds) : base(capi, bounds)
        {
            this.texture = texture;
            this.onClick = onClick;
            this.capi = capi;

            // Calculate and set fixed height and width using unscaled values
            this.Bounds.fixedHeight = GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementIcon.unscaledButtonSize;
            this.Bounds.fixedWidth = GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementIcon.unscaledButtonSize;
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            double num = scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
            double num2 = scaled(GuiElementIcon.unscaledButtonSize);
            double num3 = scaled(GuiElementIcon.unscaledButtonSize);
            int num4 = this.api.Input.MouseX - (int)this.Bounds.absX;
            int num5 = this.api.Input.MouseY - (int)this.Bounds.absY;
            Vec4f shadow = new Vec4f(0.231f, 0.188f, 0.145f, 1.0f);
            Vec4f highline = new Vec4f(0.75f, 0.055f, 0.055f, 0.7f);
            if (this.texture != null)
            {
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX, (float)this.Bounds.renderY, (float)num2, (float)num3, 50f, shadow);
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX, (float)this.Bounds.renderY, (float)num2, (float)num3, 50f);
            }
            bool flag = num4 >= 0 && num5 >= 0 && (double)num4 < num2 + num && (double)num5 < num3 + num;
            if (flag)
            {
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX + -0.5f, (float)this.Bounds.renderY + -1f, (float)num2, (float)num3, 51f, highline);
                if (flag)
                {
                    Action<int> onSlotOver = this.OnSlotOver;
                    if (onSlotOver == null)
                    {
                        return;
                    }
                    onSlotOver(0);
                }
            }
        }

    
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            onClick?.Invoke();
        }
    }

    public static class GuiComposerHelpers
    {
        public static GuiComposer AddRefreshButton(this GuiComposer composer, ICoreClientAPI capi, LoadedTexture texture, ActionConsumable onClick, ElementBounds bounds, string key = null)
        {
            if (!composer.Composed)
            {
                composer.AddInteractiveElement(new GuiElementIcon(composer.Api, texture, onClick, bounds), key);
            }
            return composer;
        }
    }

    public class GUIDialogStorageAccess : GuiDialogBlockEntity
    {
        private EnumPosFlag screenPos;

        protected string currentSearchText;
        private byte[] data { get; set; }

        public ElementBounds mainDialogBound;

        public ElementBounds gridSlots;

        public ElementBounds inputslots;

        public ElementBounds mainElement;
        private LoadedTexture refreshButtonTexture => capi.Gui.LoadSvg(new AssetLocation("game:textures/icons/refresh.svg"), 55, 55, 55, 55, new int?(ColorUtil.ColorFromRgba(206, 221, 233, 255)));

        public StorageVirtualInv StorageVirtualInv;

        public InventoryBase InventoryBase;

        public bool toggle = false;
        public override AssetLocation CloseSound { get => entityStorageController.CloseSound; set => entityStorageController.CloseSound = value; }

        public override AssetLocation OpenSound { get => entityStorageController.OpenSound; set => entityStorageController.OpenSound = value; }

        public InventoryBase inventoryBin;

        public BlockPos BlockPos;

        public ItemSlot ItemSlot => inventoryBin[0];

        public BlockEntityStorageController entityStorageController;

        protected int curTab = 0;
        public override double DrawOrder => 0.2;

        public bool IsDuplicateInv;

        public bool IsDuplicateVirInv;

        public string gridCompKey => "storageGridCompo";
        public string mainCompKey => "storageMainCompo";

        public GUIDialogStorageAccess(string dialogTitle, InventoryBase Inventory, StorageVirtualInv storageVirtualInv, BlockPos BlockEntityPosition, ICoreClientAPI capi)
             : base(dialogTitle, Inventory, BlockEntityPosition, capi)
        {
            IsDuplicateInv = capi.World.Player.InventoryManager.Inventories.ContainsValue(Inventory);

            IsDuplicateVirInv = capi.World.Player.InventoryManager.Inventories.ContainsValue(storageVirtualInv);

            if (!IsDuplicateInv && !IsDuplicateVirInv)
            {
                StorageVirtualInv = storageVirtualInv;
                InventoryBase = Inventory;
                BlockPos = BlockEntityPosition;
            }

            if (capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) is BlockEntityStorageController storageController)
            {
                entityStorageController = storageController;
            }

            inventoryBin = new InventoryGeneric(1, "bin", "1" + BlockEntityPosition, capi);

            inventoryBin[0].BackgroundIcon = "trash-can";

            InventoryBase[0].BackgroundIcon = "input";

            InventoryBase[1].BackgroundIcon = "input";

            curTab = 1;
        }

        public GUIDialogStorageAccess(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, blockEntityPos, capi)
        {
            IsDuplicateInv = capi.OpenedGuis.FirstOrDefault((object dlg) => (dlg as GuiDialogBlockEntity)?.BlockEntityPosition == blockEntityPos) != null;
            IsDuplicateVirInv = capi.OpenedGuis.FirstOrDefault((object dlg) => (dlg as GuiDialogBlockEntity)?.BlockEntityPosition == blockEntityPos) != null;

            if (!IsDuplicate && !IsDuplicateVirInv)
            {
                BlockPos = BlockEntityPosition;
            }
        }

        protected override double FloatyDialogAlign
        {
            get
            {
                return 0.8;
            }
        }

        protected override double FloatyDialogPosition
        {
            get
            {
                return 0.6;
            }
        }

        protected void ComposersDialog()
        {
            //Main Element
            mainElement = ElementBounds.Fixed(0, 0, 650, 600);

            ElementBounds buttonlist = ElementBounds.Fixed(0, 0, -200, -590).FixedUnder(mainElement, 10);

            gridSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, 10)
           .FixedUnder(mainElement, 10)
           .WithFixedPosition(8, 80);

            ElementBounds button1 = ElementBounds.Fixed(150, -25) //Clear All
                .FixedUnder(buttonlist, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button2 = ElementBounds.Fixed(235, -10) //Link All Chests
                .FixedUnder(button1, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button3 = ElementBounds.Fixed(312, -10) //Highlight
                .FixedUnder(button2, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button4 = ElementBounds.Fixed(540, 85) //PreviousGrid
                .FixedUnder(button3, 10)
                .WithFixedSize(26, 48)
                .WithAlignment(EnumDialogArea.LeftFixed);

            ElementBounds button5 = ElementBounds.Fixed(540, 403) //NextGrid
                .FixedUnder(button4, 10)
                .WithFixedSize(26, 48)
                .WithAlignment(EnumDialogArea.LeftFixed);

            ElementBounds refreshButtonBounds = ElementBounds.Fixed(EnumDialogArea.None, 0, 0, 10.0, 26.0)
               .WithFixedPosition(610, 5)
               .WithFixedPadding(0, -1)
               .WithFixedMargin(1, 1);

            ElementBounds searchBarBounds = ElementBounds.Fixed(EnumDialogArea.None, 0, 0, 500, 30)
             .WithFixedPosition(31, 60)
             .WithFixedPadding(0, -1)
             .WithFixedMargin(1, 1);

            ElementBounds inputslots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 560, 250, 2, 4);

            ElementBounds binslots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 585, 50, 1, 1);

            ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds7.BothSizing = ElementSizing.FitToChildren;
            elementBounds7.WithChildren(mainElement, buttonlist, gridSlots);
            //Main Gui
            mainDialogBound = ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(IsRight(screenPos) ? (0.0 - GuiStyle.DialogToScreenPadding) : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
            var option = Composers[mainCompKey] =
                 capi.Gui
                 .CreateCompo(mainCompKey + base.BlockEntityPosition, mainDialogBound)
                 .AddShadedDialogBG(elementBounds7, true, 10.0, 0.80f)
                 .AddDialogTitleBar(Lang.Get("storagecontroller:gui-storageinventory"), CloseIconPressed)
                 .AddButton(Lang.Get("storagecontroller:gui-button-clear-all"), OnClickClearAll, button1, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "clearAll")
                 .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-clear-all"), CairoFont.WhiteSmallText(), 300, button1)
                 .AddButton(Lang.Get("storagecontroller:gui-button-link-all"), OnClickLinkAllChests, button2, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "linkAll")
                 .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-link-all"), CairoFont.WhiteSmallText(), 300, button2)
                 .AddButton(Lang.Get("storagecontroller:gui-button-highlight"), OnClickHighlightAttached, button3, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, Lang.Get("highLight"))
                 .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-highlight"), CairoFont.WhiteSmallText(), 300, button3)
                 .AddRefreshButton(capi, refreshButtonTexture, OnRefresh, refreshButtonBounds, "refresh")
                 .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-refresh"), CairoFont.WhiteSmallText(), 300, refreshButtonBounds)
                 .AddItemSlotGrid(inventoryBin, SendBinPacket, 1, new int[1] { 0 }, binslots.BelowCopy(1, 24), "binslot")
                 .AddDynamicText(Lang.Get("Bin"), CairoFont.WhiteSmallishText(), binslots.BelowCopy(10, 2))
                 .AddInset(binslots.BelowCopy(0, 0, 0, 22), 8, 0.7f)
                 .AddItemSlotGrid(InventoryBase, SendInvPacket, 2, new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 }, inputslots, "inputslotgrid")
                 .AddDynamicText(Lang.Get("Input"), CairoFont.WhiteSmallishText(), inputslots.BelowCopy(25, -230))
                 .AddInset(inputslots.BelowCopy(-1, -230, 0, 25), 8, 0.7f)
                 .AddTextInput(searchBarBounds, FilterItemsBySearchText, CairoFont.TextInput(), "search")
                 .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-searchbar"), CairoFont.WhiteSmallText(), 300, searchBarBounds)
                 .AddIconButton("arrow-up", PreviousGrid, button4, "pregrid")
                 .AddIconButton("arrow-down", NextGrid, button5, "nextgrid");

            option.GetTextInput("search").SetPlaceHolderText(Lang.Get("Search..."));
       

            GridSlots();

            option.EndChildElements();
            option.Compose();
        }

        protected void FilterItemsBySearchText(string text)
        {
            if (!(currentSearchText == text))
            {
                currentSearchText = text;
                FilterItems();
            }
        }

        public void FilterItems()
        {
            // Convert the search text to lowercase for case-insensitive comparison
            string text = currentSearchText?.RemoveDiacritics().ToLowerInvariant();

            List<ItemSlot> listSlots = new List<ItemSlot>();

            // Retrieve the list of all items from the inventory
            if (StorageVirtualInv == null) return;

            foreach (ItemSlot itemSlot in StorageVirtualInv)
            {
                if (itemSlot.Itemstack == null || itemSlot.Empty) continue;
                listSlots.Add(itemSlot);
            }

            List<ItemSlot> filteredSlots = new List<ItemSlot>();

            foreach (ItemSlot itemSlot in listSlots)
            {
                if (itemSlot.Itemstack?.Collectible != null)
                {
                    
                    bool Attributes = itemSlot.Itemstack.Attributes.Any(type => type.Value.Equals(text));

                    if (itemSlot.Itemstack.Attributes != null && Attributes) // fix to find bookshelf wood types etc
                    {
                        filteredSlots.Add(itemSlot);
                    }
                    else if (itemSlot.Itemstack.MatchesSearchText(capi.World, text))
                    {
                        filteredSlots.Add(itemSlot);
                    }
                }
            }

            // Determine the indices of filtered slots
            int[] slotIndices = filteredSlots.Select(StorageVirtualInv.GetSlotId).ToArray();

            // Limit the number of slots to a maximum of 100
            if (slotIndices.Length > 100)
            {
                Array.Resize(ref slotIndices, 100);
            }

            var compKey = Composers[gridCompKey];
            if (compKey.GetSlotGrid("slotgrid") != null)
            {
                compKey.GetSlotGrid("slotgrid")
                    .DetermineAvailableSlots(slotIndices);
                compKey.Compose();
            }
        }

        public override void OnMouseWheel(MouseWheelEventArgs args)
        {
            // Get the mouse position
            int mouseX = capi.Input.MouseX;
            int mouseY = capi.Input.MouseY;

            // Check if the mouse is within the grid area
            if (gridSlots.PointInside(mouseX, mouseY))
            {
                if (args.delta > 0)
                {
                    PreviousGrid(true);
                }
                else
                {
                    NextGrid(true);
                }
            }

            base.OnMouseWheel(args);
        }

        private void PreviousGrid(bool value)
        {
            curTab = Math.Max(1, curTab - 1);
            GridPage();
        }

        private void NextGrid(bool value)
        {
            StorageVirtualInv storageVirtualInv = StorageVirtualInv;
            int slots = (storageVirtualInv != null) ? storageVirtualInv.Count : 0;
            int maxTabs = (int)Math.Ceiling(slots / 100.0);
            curTab = Math.Min(maxTabs, curTab + 1);
            GridPage();
        }

        //Only get called when the player press refresh button
        private void UpdateInv()
        {
            if (Composers[gridCompKey] != null)
            {
                entityStorageController.SetVirtualInventory();
                StorageVirtualInv = entityStorageController.StorageVirtualInv;
                Composers[gridCompKey].Compose(true);
            }
        }

        //Only get called when the player press refresh button
        public bool OnRefresh()
        {
            UpdateInv();
            GridSlots();
            return true;
        }

        // Only get called when you mouse wheel up and down
        private void GridPage()
        {
            StorageVirtualInv storageVirtualInv = StorageVirtualInv;

            int slots = (storageVirtualInv != null) ? storageVirtualInv.Count : 0;
            int startIndex = (curTab - 1) * 100;
            startIndex = Math.Max(0, startIndex);

            GuiComposer guiComposer = Composers[gridCompKey];

            if (guiComposer.GetSlotGrid("slotgrid") != null)
            {
                int remainingSlots = Math.Max(0, slots - startIndex);
                int minSlots = Math.Min(100, remainingSlots);
                guiComposer.GetSlotGrid("slotgrid").DetermineAvailableSlots(Enumerable.Range(startIndex, minSlots).ToArray());
            }
            guiComposer.Compose(true);
        }

        private bool OnClickClearAll()
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, BlockEntityStorageController.clearInventoryPacket, null);
            entityStorageController?.ClearHighlighted(capi.World.Player);
            StorageVirtualInv?.Clear();
            GridSlots();
            return true;
        }

        //Not sure how to fix this so it update the gride.
        private bool OnClickLinkAllChests()
        {
            entityStorageController?.LinkAll(BlockEntityStorageController.enLinkTargets.ALL, capi.World.Player);
            return true;
        }

        private bool OnClickHighlightAttached()
        {
            entityStorageController.ShowHighLight = !entityStorageController.ShowHighLight;
            entityStorageController.ToggleHighLight(capi.World.Player, entityStorageController.ShowHighLight);
            return true;
        }

        public override bool TryOpen()
        {
            if (IsDuplicateInv && IsDuplicateVirInv)
            {
                return false;
            }

            return base.TryOpen();
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            FreePos("storagecontrollergui", screenPos);
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            screenPos = GetFreePos("storagecontrollergui");
            OccupyPos("storagecontrollergui", screenPos);
            ComposersDialog();
        }

        private void SendBinPacket(object packet)
        {
            if (packet == null || ItemSlot?.Itemstack == null) return;

            ItemSlot.Itemstack = null;

            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, BlockEntityStorageController.binItemStackPacket, data);
        }

        private void SendInvPacket(object packet)
        {
            if (packet == null) return;

            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
        }

        private void SendVirtualPacket(object packet)
        {
            if (packet == null) return;

            ItemStack itemStack = capi.World.Player.InventoryManager.MouseItemSlot?.Itemstack;

            if (itemStack == null)
            {
                return;
            }

            data = itemStack.ToBytes();

            if (data != null)
            {
                capi.World.Player.InventoryManager.MouseItemSlot.Itemstack = null;
                capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, BlockEntityStorageController.itemStackPacket, data);
            }
        }

        private void GridSlots()
        {
            var stoCompKey = Composers[gridCompKey] = capi.Gui.CreateCompo(gridCompKey + base.BlockEntityPosition, mainDialogBound);

            if (StorageVirtualInv != null && !StorageVirtualInv.Empty)
            {
                stoCompKey
                .AddItemSlotGrid(StorageVirtualInv, SendVirtualPacket, 10, gridSlots, "slotgrid")
                .AddInset(gridSlots, 10, 0.7f)
                .Compose(true);

                int slotsCount = StorageVirtualInv?.Count ?? 0;

                if (curTab > 1)
                {
                    GridPage();
                }
                else
                {
                    stoCompKey.GetSlotGrid("slotgrid")
                        .DetermineAvailableSlots(Enumerable.Range(0, Math.Min(100, slotsCount)).ToArray());
                }
            }
            else
            {
                stoCompKey.AddInset(gridSlots, 10, 0.7f);
            }

            stoCompKey.Compose(true);
        }
    }
}
