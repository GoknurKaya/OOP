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
        private const int HITS_TO_WIN = 9;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string confirmPassword)
        {
            // Validasyon kontrolleri
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Kullanıcı adı ve şifre boş bırakılamaz.";
                return View();
            }

            if (username.Length < 3)
            {
                ViewBag.Error = "Kullanıcı adı en az 3 karakter olmalıdır.";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Şifre en az 6 karakter olmalıdır.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Şifreler eşleşmiyor.";
                return View();
            }

            // Kullanıcı adı zaten var mı kontrol et
            var existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.Username == username);

            if (existingPlayer != null)
            {
                ViewBag.Error = "Bu kullanıcı adı zaten kullanılıyor.";
                return View();
            }

            // Yeni oyuncu oluştur
            var newPlayer = new Player
            {
                Username = username,
                Password = password, // Not: Gerçek uygulamada hash'lenmiş olmalı!
                TotalGamesPlayed = 0,
                TotalWins = 0,
                TotalShotsFired = 0
            };

            _context.Players.Add(newPlayer);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";

            // 2 saniye sonra login sayfasına yönlendir
            Response.Headers.Add("Refresh", "2; url=" + Url.Action("Login"));

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Kullanıcı adı ve şifre boş bırakılamaz.";
                return View();
            }

            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Username == username && p.Password == password);

            if (player == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre yanlış.";
                return View();
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("CurrentPlayerId", player.Id);

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
            HttpContext.Session.SetInt32("PlayerHits", 0);
            HttpContext.Session.SetInt32("AIHits", 0);
            HttpContext.Session.Remove("AITargets"); // Temiz başlangıç

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

            if (aiBoard.GetSquareStatus(x, y) == 2 || aiBoard.GetSquareStatus(x, y) == 3)
            {
                return Json(new { success = false, message = "Bu kareye daha önce ateş ettiniz.", alreadyShot = true });
            }

            int shotCount = HttpContext.Session.GetInt32("ShotCount") ?? 0;
            shotCount++;
            HttpContext.Session.SetInt32("ShotCount", shotCount);

            bool playerHit = aiBoard.FireAt(x, y);
            int playerStatus = aiBoard.GetSquareStatus(x, y);

            int playerHits = HttpContext.Session.GetInt32("PlayerHits") ?? 0;
            if (playerHit)
            {
                playerHits++;
                HttpContext.Session.SetInt32("PlayerHits", playerHits);
            }

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

            // AI'nın zeki atışı
            (int aiX, int aiY) = GetSmartAIShot(playerBoard);

            bool aiHit = playerBoard.FireAt(aiX, aiY);
            int aiStatus = playerBoard.GetSquareStatus(aiX, aiY);

            int aiHits = HttpContext.Session.GetInt32("AIHits") ?? 0;
            if (aiHit)
            {
                aiHits++;
                HttpContext.Session.SetInt32("AIHits", aiHits);

                // İsabet koordinatını kaydet
                var targetList = GetAITargets();
                targetList.Add(new AITarget { X = aiX, Y = aiY });
                SaveAITargets(targetList);
            }
            else
            {
                // Iskaladıysa ve aktif hedef varsa, gemi battı mı kontrol et
                var targetList = GetAITargets();
                if (targetList.Count > 0 && IsShipSunk(playerBoard, targetList))
                {
                    // Gemi battı, hedefleri temizle
                    SaveAITargets(new List<AITarget>());
                }
            }

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

        // AI hedef listesi helper metodları
        private List<AITarget> GetAITargets()
        {
            var targetJson = HttpContext.Session.GetString("AITargets");
            if (string.IsNullOrEmpty(targetJson))
                return new List<AITarget>();

            return JsonSerializer.Deserialize<List<AITarget>>(targetJson) ?? new List<AITarget>();
        }

        private void SaveAITargets(List<AITarget> targets)
        {
            HttpContext.Session.SetString("AITargets", JsonSerializer.Serialize(targets));
        }

        // Zeki AI atış stratejisi
        private (int, int) GetSmartAIShot(GameBoard playerBoard)
        {
            Random rand = new Random();
            var targets = GetAITargets();

            if (targets.Count > 0)
            {
                var lastHit = targets[targets.Count - 1];

                // Yön vektörleri: yukarı, aşağı, sol, sağ
                var directions = new List<(int dx, int dy)>
                {
                    (0, -1), (0, 1), (-1, 0), (1, 0)
                };

                // Eğer birden fazla isabet varsa, yönü belirle
                if (targets.Count > 1)
                {
                    var prevHit = targets[targets.Count - 2];
                    int dx = lastHit.X - prevHit.X;
                    int dy = lastHit.Y - prevHit.Y;

                    // Aynı yönde devam et
                    int nextX = lastHit.X + dx;
                    int nextY = lastHit.Y + dy;

                    if (IsValidShot(playerBoard, nextX, nextY))
                        return (nextX, nextY);

                    // Ters yöne dene
                    nextX = prevHit.X - dx;
                    nextY = prevHit.Y - dy;

                    if (IsValidShot(playerBoard, nextX, nextY))
                        return (nextX, nextY);
                }

                // Son isabetten başlayarak tüm yönleri dene
                var shuffledDirs = directions.OrderBy(x => rand.Next()).ToList();

                foreach (var dir in shuffledDirs)
                {
                    int newX = lastHit.X + dir.dx;
                    int newY = lastHit.Y + dir.dy;

                    if (IsValidShot(playerBoard, newX, newY))
                        return (newX, newY);
                }

                // Son isabet etrafında yer yoksa, ilk isabete geri dön
                if (targets.Count > 1)
                {
                    var firstHit = targets[0];
                    foreach (var dir in shuffledDirs)
                    {
                        int newX = firstHit.X + dir.dx;
                        int newY = firstHit.Y + dir.dy;

                        if (IsValidShot(playerBoard, newX, newY))
                            return (newX, newY);
                    }
                }
            }

            // Hedef yoksa rastgele atış yap
            int aiX, aiY;
            int attempts = 0;

            do
            {
                aiX = rand.Next(10);
                aiY = rand.Next(10);
                attempts++;
            } while (!IsValidShot(playerBoard, aiX, aiY) && attempts < 100);

            return (aiX, aiY);
        }

        private bool IsValidShot(GameBoard board, int x, int y)
        {
            if (x < 0 || x >= 10 || y < 0 || y >= 10)
                return false;

            int status = board.GetSquareStatus(x, y);
            return status != 2 && status != 3;
        }

        private bool IsShipSunk(GameBoard board, List<AITarget> hitCoordinates)
        {
            foreach (var coord in hitCoordinates)
            {
                var directions = new List<(int dx, int dy)>
                {
                    (0, -1), (0, 1), (-1, 0), (1, 0)
                };

                foreach (var dir in directions)
                {
                    int checkX = coord.X + dir.dx;
                    int checkY = coord.Y + dir.dy;

                    if (checkX >= 0 && checkX < 10 && checkY >= 0 && checkY < 10)
                    {
                        int status = board.GetSquareStatus(checkX, checkY);
                        if (status == 1)
                            return false;
                    }
                }
            }

            return true;
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
            else
            {
                 player.TotalShotsFired += shotsTaken;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("PlayerBoard");
            HttpContext.Session.Remove("AIBoard");
            HttpContext.Session.Remove("ShotCount");
            HttpContext.Session.Remove("PlayerHits");
            HttpContext.Session.Remove("AIHits");
            HttpContext.Session.Remove("AITargets");

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

    // AI hedef koordinatları için sınıf (JSON serialization için)
    public class AITarget
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}