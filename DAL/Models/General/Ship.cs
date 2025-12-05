// DAL/Ship.cs
using System.Collections.Generic;

namespace DAL
{
    // Ad alanı: DAL
    public abstract class Ship
    {
        public string Name { get; protected set; }
        public int Size { get; protected set; }
        public int Hits { get; protected set; } = 0;

        // Gemiye ait koordinatları tutar (Session serileştirmesi için public)
        public List<(int X, int Y)> Coordinates { get; set; } = new List<(int, int)>();

        // Polymorphism: Tüm gemilerin ortak davranışı
        public bool IsSunk() => Hits >= Size;

        // Polymorphism: Vuruş aldığında ne yapılacağını tanımlayan soyut metot
        public abstract void Hit();

        public void RegisterHit()
        {
            Hits++;
        }
    }
}