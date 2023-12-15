using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using System.Collections.Generic;
using System.Linq;
using Vector2N = System.Numerics.Vector2;
using Vector3N = System.Numerics.Vector3;
using Vector4N = System.Numerics.Vector4;

namespace WhereTheGridAt
{
    public class WhereTheGridAt : BaseSettingsPlugin<WhereTheGridAtSettings>
    {
        public Vector2N PlayerPos { get; set; } = new Vector2N();
        public List<(Vector2N, Vector2N)> GridData { get; set; } = [];
        private RectangleF _rect;
        private ImDrawListPtr _backGroundWindowPtr;
        public static IngameData IngameData { get; set; }
        public static IngameState IngameState { get; set; }

        public override bool Initialise() => true;

        public WhereTheGridAt()
        {
            Name = "Where The Grids At";
        }

        public static Color ColorPicker(string labelName, Color inputColor)
        {
            var color = inputColor.ToVector4();
            var colorToVect4 = new Vector4N(color.X, color.Y, color.Z, color.W);
            if (ImGui.ColorEdit4(labelName, ref colorToVect4, ImGuiColorEditFlags.AlphaBar)) return new Color(colorToVect4.X, colorToVect4.Y, colorToVect4.Z, colorToVect4.W);
            return inputColor;
        }

        public override Job Tick()
        {
            IngameState = GameController?.IngameState ?? null;
            IngameData = IngameState?.Data ?? null;
            var Player = GameController?.Player ?? null;
            if (Player == null) return null;

            PlayerPos = Player.GridPosNum;
            GridData = GetGridLinesRadius(PlayerPos, Settings.GridSize, IngameState.Camera);
            return null;
        }

        public override void Render()
        {
            if (!Settings.Enable.Value || !GameController.InGame || IngameData == null || PlayerPos == Vector2N.Zero) return;

            var ingameUi = GameController.Game.IngameState.IngameUi;
            if (Settings.DisableDrawOnLeftOrRightPanelsOpen && (ingameUi.OpenLeftPanel.IsVisible || ingameUi.OpenRightPanel.IsVisible))
            {
                return;
            }

            if (!Settings.IgnoreFullscreenPanels && ingameUi.FullscreenPanels.Any(x => x.IsVisible))
            {
                return;
            }

            if (!Settings.IgnoreLargePanels && ingameUi.LargePanels.Any(x => x.IsVisible))
            {
                return;
            }

            _rect = GameController.Window.GetWindowRectangle() with { Location = Vector2.Zero };
            if (!Settings.DisableDrawRegionLimiting)
            {
                if (ingameUi.OpenRightPanel.IsVisible)
                {
                    _rect.Right = ingameUi.OpenRightPanel.GetClientRectCache.Left;
                }

                if (ingameUi.OpenLeftPanel.IsVisible)
                {
                    _rect.Left = ingameUi.OpenLeftPanel.GetClientRectCache.Right;
                }
            }

            ImGui.SetNextWindowSize(new Vector2N(_rect.Width, _rect.Height));
            ImGui.SetNextWindowPos(new Vector2N(_rect.Left, _rect.Top));

            ImGui.Begin("wherethecirclesat_drawregion",
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoBackground);

            _backGroundWindowPtr = ImGui.GetWindowDrawList();

            foreach (var line in GridData)
            {
                _backGroundWindowPtr.AddLine(line.Item1, line.Item2, Settings.GridColor.Value.ToImgui());
            }

            ImGui.End();
        }

        public List<(Vector2N, Vector2N)> GetGridLinesRadius(Vector2N PlayerPos, int Radius, Camera camera)
        {
            var gridLines = new List<(Vector2N, Vector2N)>();

            for (int x = -Radius; x < Radius; x++)
            {
                for (int y = -Radius; y < Radius; y++)
                {
                    bool isCurrentWithinRadius = IsWithinRadius(x, y, Radius);

                    if (isCurrentWithinRadius)
                    {
                        AddLineSegmentIfWithinBoundsAndPathfinding(PlayerPos, x, y, camera, gridLines);
                    }
                }
            }

            return gridLines;
        }

        private void AddLineSegmentIfWithinBoundsAndPathfinding(Vector2N PlayerPos, int x, int y, Camera camera, List<(Vector2N, Vector2N)> gridLines)
        {
            var startGridPos = new Vector2N(PlayerPos.X + x, PlayerPos.Y + y);
            var endGridPosHorizontal = new Vector2N(PlayerPos.X + x + 1, PlayerPos.Y + y);
            var endGridPosVertical = new Vector2N(PlayerPos.X + x, PlayerPos.Y + y + 1);

            if (IsValidPathfindingPosition(startGridPos) && IsValidPathfindingPosition(endGridPosHorizontal) && IsValidPathfindingPosition(endGridPosVertical))
            {
                var startScreen = camera.WorldToScreen(ExpandWithTerrainHeight(startGridPos));
                var endScreenHorizontal = camera.WorldToScreen(ExpandWithTerrainHeight(endGridPosHorizontal));
                var endScreenVertical = camera.WorldToScreen(ExpandWithTerrainHeight(endGridPosVertical));

                RectangleF screensize = new RectangleF()
                {
                    X = 0,
                    Y = 0,
                    Width = GameController.Window.GetWindowRectangleTimeCache.Size.Width,
                    Height = GameController.Window.GetWindowRectangleTimeCache.Size.Height
                };

                if (Settings.LimitInsideWindow)
                {
                    if (IsPositionWithinScreen(startScreen, screensize, 0))
                    {
                        gridLines.Add((startScreen, endScreenHorizontal));
                        gridLines.Add((startScreen, endScreenVertical));
                    }
                }
                else
                {
                    gridLines.Add((startScreen, endScreenHorizontal));
                    gridLines.Add((startScreen, endScreenVertical));
                }
            }
        }

        private bool IsValidPathfindingPosition(Vector2N position)
        {
            // Check pathfinding value
            return IngameData.GetPathfindingValueAt(position) >= Settings.PathingTileMinimumValue;
        }

        private void CheckAndAddLineSegment(Vector2N PlayerPos, int x1, int y1, int x2, int y2, Camera camera, List<(Vector2N, Vector2N)> gridLines)
        {
            var startWorld = ExpandWithTerrainHeight(new Vector2N(PlayerPos.X + x1, PlayerPos.Y + y1));
            var endWorld = ExpandWithTerrainHeight(new Vector2N(PlayerPos.X + x2, PlayerPos.Y + y2));
            var startScreen = camera.WorldToScreen(startWorld);
            var endScreen = camera.WorldToScreen(endWorld);

            RectangleF screensize = new RectangleF()
            {
                X = 0,
                Y = 0,
                Width = GameController.Window.GetWindowRectangleTimeCache.Size.Width,
                Height = GameController.Window.GetWindowRectangleTimeCache.Size.Height
            };

            if (Settings.LimitInsideWindow)
            {
                if (IsPositionWithinScreen(endScreen, screensize, 0))
                {
                    gridLines.Add((startScreen, endScreen));
                }
            }
            else
            {
                gridLines.Add((startScreen, endScreen));
            }
        }

        private bool IsWithinRadius(int x, int y, int radius)
        {
            return x * x + y * y <= radius * radius;
        }

        private static bool IsPositionWithinScreen(Vector2N entityPos, RectangleF screensize, float allowancePX)
        {
            // Check if the entity position is within the screen bounds with allowance
            float leftBound = screensize.Left - allowancePX;
            float rightBound = screensize.Right + allowancePX;
            float topBound = screensize.Top - allowancePX;
            float bottomBound = screensize.Bottom + allowancePX;

            return entityPos.X >= leftBound && entityPos.X <= rightBound &&
                   entityPos.Y >= topBound && entityPos.Y <= bottomBound;
        }

        public Vector3N ExpandWithTerrainHeight(Vector2N gridPosition)
        {
            return new Vector3N(gridPosition.GridToWorld(), GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));
        }
    }
}