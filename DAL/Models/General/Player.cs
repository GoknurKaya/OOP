// DAL/Models/Player.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models.General
{
    // Veritabanı tablosunu temsil eden model
    public class Player
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        public int TotalGamesPlayed { get; set; } = 0;
        public int TotalWins { get; set; } = 0;
        public int TotalShotsFired { get; set; } = 0;

        [NotMapped]
        public double WinRate => TotalGamesPlayed > 0 ? (double)TotalWins / TotalGamesPlayed * 100 : 0;

        [NotMapped]
        public double AverageShotsPerWin => TotalWins > 0 ? (double)TotalWins != 0 ? (double)TotalShotsFired / TotalWins : TotalShotsFired : 0;
    }
}