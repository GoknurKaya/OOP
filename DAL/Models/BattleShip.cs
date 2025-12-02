// DAL/Models/Battleship.cs
using DAL; // Ship.cs'yi kullanmak için

namespace DAL.Models
{
    public class Battleship : Ship
    {
        public Battleship()
        {
            Name = "Battleship";
            Size = 4;
        }
        public override void Hit() => RegisterHit();
    }
}