using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Collections.Generic;

namespace WhereTheGridAt
{
    public class WhereTheGridAtSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        public ToggleNode DisableDrawRegionLimiting { get; set; } = new ToggleNode(false);
        public ToggleNode DisableDrawOnLeftOrRightPanelsOpen { get; set; } = new ToggleNode(false);
        public ToggleNode IgnoreFullscreenPanels { get; set; } = new ToggleNode(false);
        public ToggleNode IgnoreLargePanels { get; set; } = new ToggleNode(false);
        public ToggleNode LimitInsideWindow { get; set; } = new ToggleNode(true);
        public ColorNode GridColor { get; set; } = new ColorNode(Color.White);
        public RangeNode<int> GridSize { get; set; } = new RangeNode<int>(60, 1, 600);
        public RangeNode<int> PathingTileMinimumValue { get; set; } = new RangeNode<int>(0, 0, 20);
    }
}