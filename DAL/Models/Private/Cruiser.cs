// DAL/Models/Cruiser.cs
using System.Drawing;

namespace DAL.Models.Private
{
    // Ad alanı: DAL.Models
    public class Cruiser : Ship
    {
        public Cruiser()
        {
            Name = "Cruiser";
            Size = 3;
        }
        public override void Hit() => RegisterHit();
    }
}