using DAL.Models.Private;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DAL.Models.General
{
    public class GameBoard
    {
        private const int GridSize = 10;

        public List<List<int>> GridData { get; set; }
        public List<ShipData> Fleet { get; set; } = new List<ShipData>();

        [JsonIgnore]
        private Random _random = new Random();

        public GameBoard()
        {
            GridData = Enumerable.Range(0, GridSize)
                                 .Select(_ => Enumerable.Repeat(0, GridSize).ToList())
                                 .ToList();
        }

        public void InitializeFleet()
        {
            Fleet = new List<ShipData>
            {
                new ShipData { Name = "Battleship", Size = 4 },
                new ShipData { Name = "Cruiser", Size = 3 },
                new ShipData { Name = "Destroyer", Size = 2 }
            };
        }

        public void PlaceShipsRandomly()
        {
            InitializeFleet();
            Random random = new Random();

            foreach (var ship in Fleet)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 200)
                {
                    attempts++;
                    bool isHorizontal = random.Next(2) == 0;
                    int startX = random.Next(GridSize - (isHorizontal ? ship.Size - 1 : 0));
                    int startY = random.Next(GridSize - (isHorizontal ? 0 : ship.Size - 1));

                    List<(int X, int Y)> potentialCoords = new List<(int, int)>();
                    bool canPlace = true;

                    for (int i = 0; i < ship.Size; i++)
                    {
                        int currentX = isHorizontal ? startX + i : startX;
                        int currentY = isHorizontal ? startY : startY + i;

                        if (currentX >= GridSize || currentY >= GridSize || GridData[currentX][currentY] != 0)
                        {
                            canPlace = false;
                            break;
                        }
                        potentialCoords.Add((currentX, currentY));
                    }

                    if (canPlace)
                    {
                        ship.Coordinates = potentialCoords;
                        ship.Hits = 0;
                        foreach (var coord in potentialCoords)
                        {
                            GridData[coord.X][coord.Y] = 1;
                        }
                        placed = true;
                    }
                }
            }
        }

        public bool PlaceShipManually(int shipIndex, int startX, int startY, bool isHorizontal)
        {
            if (shipIndex < 0 || shipIndex >= Fleet.Count) return false;

            var ship = Fleet[shipIndex];

            // Önceki yerleşimi temizle
            if (ship.Coordinates != null && ship.Coordinates.Count > 0)
            {
                foreach (var coord in ship.Coordinates)
                {
                    if (GridData[coord.X][coord.Y] == 1)
                        GridData[coord.X][coord.Y] = 0;
                }
                ship.Coordinates.Clear();
            }

            List<(int X, int Y)> potentialCoords = new List<(int, int)>();

            for (int i = 0; i < ship.Size; i++)
            {
                int currentX = isHorizontal ? startX + i : startX;
                int currentY = isHorizontal ? startY : startY + i;

                if (currentX >= GridSize || currentY >= GridSize || currentX < 0 || currentY < 0)
                    return false;

                if (GridData[currentX][currentY] != 0)
                    return false;

                potentialCoords.Add((currentX, currentY));
            }

            ship.Coordinates = potentialCoords;
            ship.Hits = 0;
            foreach (var coord in potentialCoords)
            {
                GridData[coord.X][coord.Y] = 1;
            }

            return true;
        }

        public int GetSquareStatus(int x, int y)
        {
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return -1;
            return GridData[x][y];
        }

        public bool AllShipsSunk()
        {
            return Fleet.All(s => s.IsSunk());
        }

        public bool FireAt(int x, int y)
        {
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;

            if (GridData[x][y] == 1)
            {
                GridData[x][y] = 2;
                var hitShip = Fleet.FirstOrDefault(s => s.Coordinates != null && s.Coordinates.Contains((x, y)));
                if (hitShip != null)
                {
                    hitShip.Hits++;
                }
                return true;
            }
            else if (GridData[x][y] == 0)
            {
                GridData[x][y] = 3;
                return false;
            }
            return false;
        }
    }

    public class ShipData
    {
        public string Name { get; set; } = string.Empty;
        public int Size { get; set; }
        public int Hits { get; set; } = 0;
        public List<(int X, int Y)> Coordinates { get; set; } = new List<(int, int)>();

        public bool IsSunk() => Hits >= Size;
    }
}