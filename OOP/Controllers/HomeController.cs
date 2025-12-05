// OOP/Controllers/HomeController.cs
using DAL;
using DAL.Models; // Player modelini kullanmak için
using DAL.Models.General;
using DAL.Models.Private;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OOP.Models; // GameViewModel'i kullanmak için

namespace OOP.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // -----------------------------------------------------------
        // 👤 [GET] Login ve [POST] Login Metotları (Aynı kalır)
        // -----------------------------------------------------------
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username)
        {
            // ... (Kullanıcı bulma/oluşturma mantığı)
            if (string.IsNullOrWhiteSpace(username))
            {
                ViewBag.Error = "Kullanıcı adı boş olamaz.";
                return View();
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Username == username);

            if (player == null)
            {
                player = new Player { Username = username };
                _context.Players.Add(player);
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.Remove("PlayerBoard");
            HttpContext.Session.Remove("AIBoard");

            HttpContext.Session.SetInt32("CurrentPlayerId", player.Id);
            return RedirectToAction("GameScreen");
        }

        // -----------------------------------------------------------
        // 🎮 [GET] Ana Oyun Ekranı (GameBoard'lar tanımlanır)
        // -----------------------------------------------------------
        public IActionResult GameScreen()
        {
            if (!HttpContext.Session.GetInt32("CurrentPlayerId").HasValue) return RedirectToAction("Login");

            GameBoard playerBoard = HttpContext.Session.GetObject<GameBoard>("PlayerBoard");
            GameBoard aiBoard = HttpContext.Session.GetObject<GameBoard>("AIBoard");

            if (playerBoard == null || aiBoard == null)
            {
                playerBoard = new GameBoard();
                aiBoard = new GameBoard();

                HttpContext.Session.SetObject("PlayerBoard", playerBoard);
                HttpContext.Session.SetObject("AIBoard", aiBoard);
            }

            var gameViewModel = new GameViewModel
            {
                PlayerBoardData = playerBoard.GridData,
                AIBoardData = aiBoard.GridData
            };

            return View(gameViewModel);
        }

        // -----------------------------------------------------------
        // 💥 [POST] Atış İşlemi (Anonim Tip ve Değişken Tanımlama Hataları Düzeltildi)
        // -----------------------------------------------------------
        [HttpPost]
        public IActionResult Fire(int x, int y)
        {
            if (!HttpContext.Session.GetInt32("CurrentPlayerId").HasValue) return Unauthorized();

            // Tahtalar her zaman Fire metodu içinde tanımlanmalıdır
            GameBoard aiBoard = HttpContext.Session.GetObject<GameBoard>("AIBoard");
            GameBoard playerBoard = HttpContext.Session.GetObject<GameBoard>("PlayerBoard");

            if (aiBoard == null || playerBoard == null)
                return Json(new { success = false, message = "Oyun yeniden başlatılmalı." });

            // Hızlı tıklama/Aynı bloğa basma açığı kontrolü
            if (aiBoard.GetSquareStatus(x, y) == 2 || aiBoard.GetSquareStatus(x, y) == 3)
            {
                // alreadyShot property'si anonim tipe eklendi.
                return Json(new { success = false, message = "Bu kareye daha önce ateş ettiniz.", alreadyShot = true });
            }

            // 1. Oyuncunun Atışı
            bool playerHit = aiBoard.FireAt(x, y);
            int playerStatus = aiBoard.GetSquareStatus(x, y); // Durum burada tanımlandı (CS0103 çözümü)

            // Kazanma kontrolü
            if (aiBoard.AllShipsSunk())
            {
                HttpContext.Session.Remove("PlayerBoard");
                HttpContext.Session.Remove("AIBoard");
                return Json(new { success = true, gameover = true, winner = "Player", PlayerHit = playerHit, PlayerStatus = playerStatus });
            }

            // 2. AI'nın Atışı
            Random rand = new Random();
            int aiX, aiY;

            do
            {
                aiX = rand.Next(10);
                aiY = rand.Next(10);
            } while (playerBoard.GetSquareStatus(aiX, aiY) == 2 || playerBoard.GetSquareStatus(aiX, aiY) == 3);

            bool aiHit = playerBoard.FireAt(aiX, aiY);

            // Kaybetme kontrolü
            if (playerBoard.AllShipsSunk())
            {
                HttpContext.Session.Remove("PlayerBoard");
                HttpContext.Session.Remove("AIBoard");
                return Json(new { success = true, gameover = true, winner = "AI", PlayerHit = playerHit, PlayerStatus = playerStatus });
            }

            // Tahtaları Session'a kaydet (Güncelle)
            HttpContext.Session.SetObject("AIBoard", aiBoard);
            HttpContext.Session.SetObject("PlayerBoard", playerBoard);

            // Başarılı AJAX yanıtı
            return Json(new
            {
                success = true,
                PlayerHit = playerHit,
                PlayerStatus = playerStatus, // Anonim tip property'si
                aiShot = new { x = aiX, y = aiY, hit = aiHit, status = playerBoard.GetSquareStatus(aiX, aiY) }
            });
        }

        // -----------------------------------------------------------
        // 🏁 [POST] EndGame ve 📊 [GET] Statistics Metotları (Aynı kalır)
        // -----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> EndGame(bool isWin, int shotsTaken)
        {
            var playerId = HttpContext.Session.GetInt32("CurrentPlayerId");
            if (!playerId.HasValue) return Unauthorized();

            var player = await _context.Players.FindAsync(playerId.Value);

            if (player == null) return NotFound();

            player.TotalGamesPlayed++;

            if (isWin)
            {
                player.TotalWins++;
                player.TotalShotsFired += shotsTaken;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Statistics");
        }

        public async Task<IActionResult> Statistics()
        {
            var playerId = HttpContext.Session.GetInt32("CurrentPlayerId");
            if (!playerId.HasValue) return RedirectToAction("Login");

            var playerStats = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId.Value);

            if (playerStats == null) return NotFound();
            return View(playerStats);
        }
    }
}