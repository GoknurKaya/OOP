// DAL/GameBoard.cs
using DAL.Models.Private;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL.Models.General
{
    // Ad alanı: DAL
    public class GameBoard
    {
        private const int GridSize = 10;

        // List<List<int>> serileştirme için
        public List<List<int>> GridData { get; private set; }

        public List<Ship> Fleet { get; private set; } = new List<Ship>();
        private readonly Random _random = new Random();

        public GameBoard()
        {
            // GridData'yı başlat (10x10, hepsi 0)
            GridData = Enumerable.Range(0, GridSize)
                                 .Select(_ => Enumerable.Repeat(0, GridSize).ToList())
                                 .ToList();

            // Gemileri Models klasöründen ekle
            Fleet.Add(new Battleship());
            Fleet.Add(new Cruiser());
            Fleet.Add(new Destroyer());

            PlaceShips(); // OTOMATİK YERLEŞTİRME TEKRAR ÇALIŞTIRILDI
        }

        public int GetSquareStatus(int x, int y) => GridData[x][y];
        public bool AllShipsSunk() => Fleet.All(s => s.IsSunk());

        // ... (FireAt metodu aynı kalır)

        public void PlaceShips()
        {
            Random random = new Random();
            foreach (var ship in Fleet)
            {
                bool placed = false;
                int attempts = 0; // Sonsuz döngüden kaçınmak için
                while (!placed && attempts < 100)
                {
                    attempts++;
                    bool isHorizontal = random.Next(2) == 0;
                    // Sınır kontrolü (yerleştirme alanını gemi boyutuna göre daraltır)
                    int startX = random.Next(GridSize - (isHorizontal ? ship.Size : 1));
                    int startY = random.Next(GridSize - (isHorizontal ? 1 : ship.Size));

                    List<(int X, int Y)> potentialCoords = new List<(int, int)>();
                    bool canPlace = true;

                    for (int i = 0; i < ship.Size; i++)
                    {
                        int currentX = isHorizontal ? startX + i : startX;
                        int currentY = isHorizontal ? startY : startY + i;

                        // Geçerlilik kontrolü
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
                        foreach (var coord in potentialCoords)
                        {
                            GridData[coord.X][coord.Y] = 1; // Gemi parçası
                        }
                        placed = true;
                    }
                }
                // Eğer yerleştirilemezse (100 deneme sonrası), bu bir hata olabilir, ancak şimdilik yoksayıyoruz.
            }
        }

        public bool FireAt(int x, int y)
        {
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;

            if (GridData[x][y] == 1) // İsabet!
            {
                GridData[x][y] = 2;
                var hitShip = Fleet.Find(s => s.Coordinates.Contains((x, y)));
                hitShip?.Hit();
                return true;
            }
            else if (GridData[x][y] == 0) // Iska
            {
                GridData[x][y] = 3;
                return false;
            }
            return false;
        }
    }
}