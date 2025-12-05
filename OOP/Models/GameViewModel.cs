// OOP/Models/GameViewModel.cs
using DAL; // GameBoard'u kullanmak için

namespace OOP.Models
{
    public class GameViewModel
    {
        // DEĞİŞİKLİK BURADA: Tip List<List<int>> oldu.
        public List<List<int>> PlayerBoardData { get; set; }
        public List<List<int>> AIBoardData { get; set; }
    }
}