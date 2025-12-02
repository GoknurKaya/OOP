// DAL/GameBoard.cs
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography; // Random için daha iyi bir seçenek kullanıldı

namespace DAL
{
    public class GameBoard
    {
        private const int GridSize = 10;

        // Encapsulation: Tahta verisi (Session serileştirmesi için public property/getter)
        public int[,] GridData { get; private set; } = new int[GridSize, GridSize];

        public List<Ship> Fleet { get; private set; } = new List<Ship>();

        public GameBoard()
        {
            // Yeni tahta oluşturulurken gemiler filoya eklenir
            Fleet.Add(new Models.Battleship()); // Models klasöründeki Battleship'i kullanır
                                                // Diğer gemileri de ekleyin:
                                                // Fleet.Add(new Models.Cruiser()); 
                                                // Fleet.Add(new Models.Destroyer()); 

            PlaceShips();
        }

        public bool FireAt(int x, int y)
        {
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;

            if (GridData[x, y] == 1) // İsabet!
            {
                GridData[x, y] = 2;
                var hitShip = Fleet.Find(s => s.Coordinates.Contains((x, y)));
                hitShip?.Hit();
                return true;
            }
            else if (GridData[x, y] == 0) // Iska
            {
                GridData[x, y] = 3;
                return false;
            }
            return false;
        }

        public int GetSquareStatus(int x, int y) => GridData[x, y];
        public bool AllShipsSunk() => Fleet.All(s => s.IsSunk());

        public void PlaceShips()
        {
            Random random = new Random();
            foreach (var ship in Fleet)
            {
                bool placed = false;
                while (!placed)
                {
                    bool isHorizontal = random.Next(2) == 0;
                    int startX = random.Next(GridSize - (isHorizontal ? ship.Size : 1));
                    int startY = random.Next(GridSize - (isHorizontal ? 1 : ship.Size));

                    List<(int X, int Y)> potentialCoords = new List<(int X, int Y)>();
                    bool canPlace = true;

                    for (int i = 0; i < ship.Size; i++)
                    {
                        int currentX = isHorizontal ? startX + i : startX;
                        int currentY = isHorizontal ? startY : startY + i;

                        if (GridData[currentX, currentY] != 0)
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
                            GridData[coord.X, coord.Y] = 1; // Gemi parçası
                        }
                        placed = true;
                    }
                }
            }
        }
    }
}