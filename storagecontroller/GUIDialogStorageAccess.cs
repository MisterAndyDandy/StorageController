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
using Vintagestory.API.Config;
using System.Reflection;
using Vintagestory.API.Util;
using System.Xml.Linq;

namespace storagecontroller
{
    public class GUIDialogStorageAccess : GuiDialogBlockEntity
    {
        public GUIDialogStorageAccess(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory((IInventory)inventory);
            SetupDialog();
        }

        protected virtual void SetupDialog()
        {
            double SSB = (GuiElementPassiveItemSlot.unscaledSlotSize);
            double SSP = (GuiElementItemSlotGridBase.unscaledSlotPadding);
            int itemsincolumn = 10;
            int columns = (this.Inventory.Count - 2) / itemsincolumn;
            double mainWindowWidth = SSB * (columns > 1 ? columns - 1 : 2) + columns * (SSB * 3 + SSP * 4);
            double mainWindowHeight = SSB + SSB + itemsincolumn * SSB + (itemsincolumn + 1) * SSP + SSB;
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds tradeSlotsBounds = ElementBounds.FixedPos(EnumDialogArea.LeftBottom, 0, 100)
                .WithFixedWidth(mainWindowWidth)
                .WithFixedHeight(mainWindowHeight);
            bgBounds.WithChildren(tradeSlotsBounds);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("storagecontrollercompo", dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("storagecontroller:storageinventory"), OnTitleBarCloseClicked);

            int maxRaws = 8;
            int curColumn = 0;
            for (int i = 0; i < (Inventory.Count - 2) / 3; i++)
            {
                if (i != 0 && i % maxRaws == 0)
                {
                    curColumn++;
                }
                var tm = new int[] { 2 + i * 3, 3 + i * 3, 4 + i * 3 };
                
                var tmp = ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX + 30 + curColumn * 200, (i % maxRaws) * 60)
                    .WithFixedWidth(((162)))
                 .WithFixedHeight(48);
                
                tradeSlotsBounds.WithChild(tmp);
                SingleComposer.AddItemSlotGrid(this.Inventory,
                    new Action<object>((this).DoSendPacket),
                    3,
                    tm,
                    tmp,
                    "tradeRaw" + i.ToString());
                ElementBounds tmpEB = ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX + 30 + curColumn * 200 + 165, (i % maxRaws) * 60 + 25).WithFixedHeight(GuiElement.scaled((200.0))).WithFixedWidth(35);
                tradeSlotsBounds.WithChild(tmpEB);
                //SingleComposer.AddDynamicText((this.Inventory).be.stocks[i].ToString(), CairoFont.WhiteDetailText(), tmpEB, "stock" + i);

            }
            SingleComposer.Compose();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        
    }
}
