// DAL/Models/Battleship.cs
namespace DAL.Models.Private
{
    // Ad alanı: DAL.Models
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