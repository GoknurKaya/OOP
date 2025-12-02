// OOP/Models/Ship.cs
using System.Collections.Generic;

namespace DAL.Models
{
    // Inheritance için temel soyut sınıf
    public abstract class Ship
    {
        public string Name { get; protected set; }
        public int Size { get; protected set; }
        public int Hits { get; protected set; } = 0;

        // Gemiye ait koordinatları tutar
        public List<(int X, int Y)> Coordinates { get; set; } = new List<(int, int)>();
        public bool IsSunk() => Hits >= Size;

        public abstract void Hit();

        public void RegisterHit()
        {
            Hits++;
        }
    }
}