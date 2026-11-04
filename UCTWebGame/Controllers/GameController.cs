using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCTWebGame.Data;
using UCTWebGame.DTOs;
using UCTWebGame.Engine;
using UCTWebGame.Models;
using UCTWebGame.Services;

namespace UCTWebGame.Controllers;

[Authorize]
[ApiController]
[Route("api/games")]
public sealed class GameController : ControllerBase
{
    private readonly GameEventService _gameService;
    private readonly GameEngine _gameEngine;
    private readonly IWebHostEnvironment _environment;

    public GameController(
        GameEventService gameService,
        GameEngine gameEngine,
        IWebHostEnvironment environment)
    {
        _gameService = gameService;
        _gameEngine = gameEngine;
        _environment = environment;
    }

    [HttpPost]
    [ProducesResponseType<GameEvent>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameEvent>> StartGame(
        StartGameRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var countryPath = Path.Combine(_environment.ContentRootPath, "Data", "countries.json");
            var countries = CountrySEED.LoadFromFile(countryPath);
            var game = _gameEngine.StartGame(
                userId,
                request.ChosenCountryCode.Trim().ToUpperInvariant(),
                countries);

            var savedGame = await _gameService.CreateAsync(game, cancellationToken);
            return CreatedAtAction(nameof(GetGame), new { gameId = savedGame.Id }, savedGame);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GameEvent>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GameEvent>>> GetMyGames(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var games = await _gameService.GetForUserAsync(userId, cancellationToken);
        return Ok(games);
    }

    [HttpGet("{gameId}")]
    [ProducesResponseType<GameEvent>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameEvent>> GetGame(
        string gameId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var game = await _gameService.GetByIdForUserAsync(gameId, userId, cancellationToken);
        return game is null ? NotFound() : Ok(game);
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub");
}
