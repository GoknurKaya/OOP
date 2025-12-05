// DAL/Models/Destroyer.cs
using System.Drawing;

namespace DAL.Models.Private
{
    // Ad alanı: DAL.Models
    public class Destroyer : Ship
    {
        public Destroyer()
        {
            Name = "Destroyer";
            Size = 2;
        }
        public override void Hit() => RegisterHit();
    }
}