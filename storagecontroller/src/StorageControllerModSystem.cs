using Vintagestory.API.Common;
using storagecontroller;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using HarmonyLib;
using System.Reflection;

namespace StorageController
{
    public class StorageControllerModSystem : ModSystem
    {
        private ICoreClientAPI capi;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockEntityClass("StorageControllerMaster", typeof(BlockEntityStorageController));
            api.RegisterItemClass("ItemStorageLinker", typeof(ItemStorageLinker));
            api.RegisterBlockClass("BlockStorageController", typeof(BlockStorageController));


            api.RegisterBlockClass("BlockSignaltower", typeof(BlockSignalTower));
            api.RegisterBlockEntityClass("Signaltower", typeof(BlockEntitySignalTower));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api as ICoreClientAPI;

            RegisterCustomIcon("trash-can", 0, 0, 0, 255);
            RegisterCustomIcon("refresh", 239, 222, 205, 255);
            RegisterCustomIcon("input", 0, 0, 0, 255);
            RegisterCustomIcon("arrow-up", 239, 222, 205, 150);
            RegisterCustomIcon("arrow-down", 239, 222, 205, 150);
        }

        private void RegisterCustomIcon(string key, int r, int g, int b, int a)
        {
            capi.Gui.Icons.CustomIcons[key] = delegate (Context ctx, int x, int y, float w, float h, double[] rgba)
            {
                AssetLocation location = new AssetLocation("game:textures/icons/" + key + ".svg");
                IAsset svgAsset = capi.Assets.TryGet(location, true);
                int value = ColorUtil.ColorFromRgba(r, g, b, a);
                capi.Gui.DrawSvg(svgAsset, ctx.GetTarget() as ImageSurface, x, y, (int)w, (int)h, new int?(value));
            };
        }
    }
}
