using System.ComponentModel.DataAnnotations;

namespace DAL.Models.General
{
    public class GameSession
    {
        [Key]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public string PlayerBoardJson { get; set; } = string.Empty;
        public string AIBoardJson { get; set; } = string.Empty;
        public int ShotsFired { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsCompleted { get; set; } = false;
        public bool PlayerWon { get; set; } = false;
    }
}