using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace storagecontroller
{
    public class BlockEntitySignalTower : BlockEntity
    {
        //things to add 

        public BlockPos StorageControllerPos { get; set; } = new BlockPos(0) { };

        public List<BlockPos> ListContainer = new List<BlockPos>();

        public bool IsInRange(BlockPos checkpos)
        {
            int xdiff = Math.Abs(Pos.X - checkpos.X);
            if (xdiff >= 5) { return false; }
            int ydiff = Math.Abs(Pos.Y - checkpos.Y);
            if (ydiff >= 5) { return false; }
            int zdiff = Math.Abs(Pos.Z - checkpos.Z);
            if (zdiff >= 5) { return false; }
            return true;
        }
    }
}
