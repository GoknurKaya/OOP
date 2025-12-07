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

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username)
        {
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

            if (aiBoard.AllShipsSunk())
            {
                HttpContext.Session.SetString("AIBoard", JsonSerializer.Serialize(aiBoard));
                return Json(new
                {
                    success = true,
                    gameover = true,
                    winner = "Player",
                    PlayerHit = playerHit,
                    PlayerStatus = playerStatus,
                    shotCount = shotCount
                });
            }

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

            if (playerBoard.AllShipsSunk())
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
                    shotCount = shotCount
                });
            }

            HttpContext.Session.SetString("AIBoard", JsonSerializer.Serialize(aiBoard));
            HttpContext.Session.SetString("PlayerBoard", JsonSerializer.Serialize(playerBoard));

            return Json(new
            {
                success = true,
                PlayerHit = playerHit,
                PlayerStatus = playerStatus,
                aiShot = new { x = aiX, y = aiY, hit = aiHit, status = aiStatus }
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