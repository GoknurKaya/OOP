using DAL;
using DAL.Models.General;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OOP.Models;
using System.Text.Json;

namespace OOP.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private const int HITS_TO_WIN = 9; // Kazanmak için gereken isabet sayısı

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // Varsayımlar:
        // 1. Controller'ınızda bir DbContext (_context) bağımlılığı var.
        // 2. DAL.Models.General.Player modelinin bir 'Password' özelliği var.

        [HttpGet]
        public IActionResult Login()
        {
            // Giriş sayfasını göster
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password) // Şifre parametresi eklendi
        {
            // 1. Basit Boşluk Kontrolleri
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Kullanıcı adı ve şifre boş bırakılamaz.";
                return View();
            }

            // 2. Kullanıcıyı Bulma
            // Şifreler veritabanında HASH'lenmiş olmalıdır! Güvenlik için bu çok önemlidir.
            // Örnekte basitlik için düz metin karşılaştırması kullanılmıştır.
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Username == username && p.Password == password);
            // Gerçek uygulamada: p.PasswordHash == Hash(password)

            // 3. Kullanıcı Kontrolü
            if (player == null)
            {
                // Kullanıcı adı veya şifre hatalı.
                ViewBag.Error = "Kullanıcı adı veya şifre yanlış.";
                return View();
            }

            // 4. Başarılı Giriş İşlemi
            // Mevcut oturumu temizleyip yeni oyuncu kimliğini oturuma kaydet
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("CurrentPlayerId", player.Id);

            // Yönlendirme
            return RedirectToAction("ShipPlacement");
        }

        [HttpGet]
        public IActionResult ShipPlacement()
        {
            if (!HttpContext.Session.GetInt32("CurrentPlayerId").HasValue)
                return RedirectToAction("Login");

            var playerBoard = new GameBoard();
            playerBoard.InitializeFleet();

            HttpContext.Session.SetString("PlayerBoard", JsonSerializer.Serialize(playerBoard));

            return View(playerBoard);
        }

        [HttpPost]
        public IActionResult PlaceShip(int shipIndex, int startX, int startY, bool isHorizontal)
        {
            var playerBoardJson = HttpContext.Session.GetString("PlayerBoard");
            if (string.IsNullOrEmpty(playerBoardJson))
                return Json(new { success = false, message = "Tahta bulunamadı" });

            var playerBoard = JsonSerializer.Deserialize<GameBoard>(playerBoardJson);

            bool placed = playerBoard.PlaceShipManually(shipIndex, startX, startY, isHorizontal);

            if (placed)
            {
                HttpContext.Session.SetString("PlayerBoard", JsonSerializer.Serialize(playerBoard));
                return Json(new { success = true, board = playerBoard.GridData });
            }

            return Json(new { success = false, message = "Gemi yerleştirilemedi" });
        }

        [HttpPost]
        public IActionResult PlaceShipsRandomly()
        {
            var playerBoard = new GameBoard();
            playerBoard.PlaceShipsRandomly();

            HttpContext.Session.SetString("PlayerBoard", JsonSerializer.Serialize(playerBoard));

            return Json(new { success = true, board = playerBoard.GridData });
        }

        [HttpPost]
        public IActionResult StartGame()
        {
            var playerBoardJson = HttpContext.Session.GetString("PlayerBoard");
            if (string.IsNullOrEmpty(playerBoardJson))
                return Json(new { success = false, message = "Gemiler yerleştirilmedi" });

            var playerBoard = JsonSerializer.Deserialize<GameBoard>(playerBoardJson);

            bool allShipsPlaced = playerBoard.Fleet.All(s => s.Coordinates != null && s.Coordinates.Count > 0);

            if (!allShipsPlaced)
                return Json(new { success = false, message = "Tüm gemileri yerleştirin" });

            var aiBoard = new GameBoard();
            aiBoard.PlaceShipsRandomly();

            HttpContext.Session.SetString("AIBoard", JsonSerializer.Serialize(aiBoard));
            HttpContext.Session.SetInt32("ShotCount", 0);
            HttpContext.Session.SetInt32("PlayerHits", 0); // Oyuncu isabet sayısı
            HttpContext.Session.SetInt32("AIHits", 0); // AI isabet sayısı

            return Json(new { success = true });
        }

        public IActionResult GameScreen()
        {
            if (!HttpContext.Session.GetInt32("CurrentPlayerId").HasValue)
                return RedirectToAction("Login");

            var playerBoardJson = HttpContext.Session.GetString("PlayerBoard");
            var aiBoardJson = HttpContext.Session.GetString("AIBoard");

            if (string.IsNullOrEmpty(playerBoardJson) || string.IsNullOrEmpty(aiBoardJson))
                return RedirectToAction("ShipPlacement");

            var playerBoard = JsonSerializer.Deserialize<GameBoard>(playerBoardJson);
            var aiBoard = JsonSerializer.Deserialize<GameBoard>(aiBoardJson);

            var gameViewModel = new GameViewModel
            {
                PlayerBoardData = playerBoard.GridData,
                AIBoardData = aiBoard.GridData
            };

            return View(gameViewModel);
        }

        [HttpPost]
        public IActionResult Fire(int x, int y)
        {
            if (!HttpContext.Session.GetInt32("CurrentPlayerId").HasValue)
                return Unauthorized();

            var aiBoardJson = HttpContext.Session.GetString("AIBoard");
            var playerBoardJson = HttpContext.Session.GetString("PlayerBoard");

            if (string.IsNullOrEmpty(aiBoardJson) || string.IsNullOrEmpty(playerBoardJson))
                return Json(new { success = false, message = "Oyun yeniden başlatılmalı." });

            var aiBoard = JsonSerializer.Deserialize<GameBoard>(aiBoardJson);
            var playerBoard = JsonSerializer.Deserialize<GameBoard>(playerBoardJson);

            // Daha önce ateş edilmiş mi kontrol et
            if (aiBoard.GetSquareStatus(x, y) == 2 || aiBoard.GetSquareStatus(x, y) == 3)
            {
                return Json(new { success = false, message = "Bu kareye daha önce ateş ettiniz.", alreadyShot = true });
            }

            // Atış sayısını artır
            int shotCount = HttpContext.Session.GetInt32("ShotCount") ?? 0;
            shotCount++;
            HttpContext.Session.SetInt32("ShotCount", shotCount);

            // Oyuncu ateş etti
            bool playerHit = aiBoard.FireAt(x, y);
            int playerStatus = aiBoard.GetSquareStatus(x, y);

            // İsabet sayısını güncelle
            int playerHits = HttpContext.Session.GetInt32("PlayerHits") ?? 0;
            if (playerHit)
            {
                playerHits++;
                HttpContext.Session.SetInt32("PlayerHits", playerHits);
            }

            // Oyuncu kazandı mı? (9 isabet)
            if (playerHits >= HITS_TO_WIN)
            {
                HttpContext.Session.SetString("AIBoard", JsonSerializer.Serialize(aiBoard));
                return Json(new
                {
                    success = true,
                    gameover = true,
                    winner = "Player",
                    playerHit = playerHit,
                    PlayerStatus = playerStatus,
                    shotCount = shotCount,
                    playerHits = playerHits,
                    aiHits = HttpContext.Session.GetInt32("AIHits") ?? 0
                });
            }

            // AI'nın sırası
            Random rand = new Random();
            int aiX, aiY;
            int attempts = 0;

            do
            {
                aiX = rand.Next(10);
                aiY = rand.Next(10);
                attempts++;
            } while ((playerBoard.GetSquareStatus(aiX, aiY) == 2 || playerBoard.GetSquareStatus(aiX, aiY) == 3) && attempts < 100);

            bool aiHit = playerBoard.FireAt(aiX, aiY);
            int aiStatus = playerBoard.GetSquareStatus(aiX, aiY);

            // AI isabet sayısını güncelle
            int aiHits = HttpContext.Session.GetInt32("AIHits") ?? 0;
            if (aiHit)
            {
                aiHits++;
                HttpContext.Session.SetInt32("AIHits", aiHits);
            }

            // AI kazandı mı? (9 isabet)
            if (aiHits >= HITS_TO_WIN)
            {
                HttpContext.Session.SetString("PlayerBoard", JsonSerializer.Serialize(playerBoard));
                HttpContext.Session.SetString("AIBoard", JsonSerializer.Serialize(aiBoard));
                return Json(new
                {
                    success = true,
                    gameover = true,
                    winner = "AI",
                    PlayerHit = playerHit,
                    PlayerStatus = playerStatus,
                    aiShot = new { x = aiX, y = aiY, hit = aiHit, status = aiStatus },
                    shotCount = shotCount,
                    playerHits = playerHits,
                    aiHits = aiHits
                });
            }

            // Oyun devam ediyor
            HttpContext.Session.SetString("AIBoard", JsonSerializer.Serialize(aiBoard));
            HttpContext.Session.SetString("PlayerBoard", JsonSerializer.Serialize(playerBoard));

            return Json(new
            {
                success = true,
                PlayerHit = playerHit,
                PlayerStatus = playerStatus,
                aiShot = new { x = aiX, y = aiY, hit = aiHit, status = aiStatus },
                playerHits = playerHits,
                aiHits = aiHits
            });
        }

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

            HttpContext.Session.Remove("PlayerBoard");
            HttpContext.Session.Remove("AIBoard");
            HttpContext.Session.Remove("ShotCount");
            HttpContext.Session.Remove("PlayerHits");
            HttpContext.Session.Remove("AIHits");

            return Ok();
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