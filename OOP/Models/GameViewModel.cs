// OOP/Models/GameViewModel.cs
using DAL; // GameBoard'u kullanmak için

namespace OOP.Models
{
    public class GameViewModel
    {
        // Tahta verileri DAL'dan geliyor.
        public int[,] PlayerBoardData { get; set; }
        public int[,] AIBoardData { get; set; }
    }
}