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
    private readonly IGameEventService _gameService;
    private readonly GameEngine _gameEngine;
    private readonly INpcService _npcService;
    private readonly ICommentaryService _commentary;
    private readonly IWebHostEnvironment _environment;

    public GameController(
        IGameEventService gameService,
        GameEngine gameEngine,
        INpcService npcService,
        ICommentaryService commentary,
        IWebHostEnvironment environment)
    {
        _gameService = gameService;
        _gameEngine = gameEngine;
        _npcService = npcService;
        _commentary = commentary;
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
            var startLog = _npcService.PlayUntilHumanTurn(game);
            await AddCommentaryAsync(game, startLog, cancellationToken);
            GameLog.Append(game, startLog);

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

    [HttpPost("{gameId}/reinforcements")]
    [ProducesResponseType<GameActionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PlaceReinforcements(
        string gameId,
        ApplyReinforcementsRequest request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForActionAsync(gameId, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        return await CommitActionAsync(
            loaded.Game!,
            () =>
            {
                _gameEngine.ApplyReinforcements(
                    loaded.Game!,
                    HumanPlayer(loaded.Game!).Id,
                    request.Allocation);
                return null;
            },
            cancellationToken);
    }

    [HttpPost("{gameId}/attack")]
    [ProducesResponseType<GameActionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartAttack(
        string gameId,
        StartAttackRequest request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForActionAsync(gameId, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        var game = loaded.Game!;
        return await CommitActionAsync(
            game,
            () =>
            {
                _gameEngine.StartAttack(
                    game,
                    HumanPlayer(game).Id,
                    request.AttackerCode.Trim().ToUpperInvariant(),
                    request.DefenderCode.Trim().ToUpperInvariant());
                return null;
            },
            cancellationToken);
    }

    [HttpPost("{gameId}/attack/roll")]
    [ProducesResponseType<GameActionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveAttackRound(
        string gameId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForActionAsync(gameId, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        var game = loaded.Game!;

        // Read the names BEFORE the roll: a conquest clears game.PendingAttack.
        var attackerName = game.Countries.FirstOrDefault(c => c.Code == game.PendingAttack?.AttackerCode)?.Name ?? "The attacker";
        var defenderName = game.Countries.FirstOrDefault(c => c.Code == game.PendingAttack?.DefenderCode)?.Name ?? "the defender";

        return await CommitActionAsync(
            game,
            () => _gameEngine.ResolveDiceRound(game),
            cancellationToken,
            async result =>
            {
                if (result is null) return null;
                var input = CommentaryFacts.ForDiceRound(attackerName, defenderName, result);
                return await _commentary.GenerateAsync(input.Facts, input.Fallback, cancellationToken);
            });
    }

    [HttpPost("{gameId}/attack/stop")]
    [ProducesResponseType<GameActionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> StopAttack(
        string gameId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForActionAsync(gameId, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        var game = loaded.Game!;
        return await CommitActionAsync(
            game,
            () =>
            {
                _gameEngine.StopAttack(game, HumanPlayer(game).Id);
                return null;
            },
            cancellationToken);
    }

    [HttpPost("{gameId}/transfer")]
    [ProducesResponseType<GameActionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Transfer(
        string gameId,
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForActionAsync(gameId, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        var game = loaded.Game!;
        return await CommitActionAsync(
            game,
            () =>
            {
                _gameEngine.Transfer(
                    game,
                    HumanPlayer(game).Id,
                    request.FromCode.Trim().ToUpperInvariant(),
                    request.ToCode.Trim().ToUpperInvariant(),
                    request.Amount);
                return null;
            },
            cancellationToken);
    }

    [HttpPost("{gameId}/skip")]
    [ProducesResponseType<GameActionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SkipTurn(
        string gameId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForActionAsync(gameId, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        var game = loaded.Game!;
        return await CommitActionAsync(
            game,
            () =>
            {
                _gameEngine.Skip(game, HumanPlayer(game).Id);
                return null;
            },
            cancellationToken);
    }

    [HttpPost("{gameId}/advance-npcs")]
    [ProducesResponseType<GameActionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AdvanceNpcTurns(
        string gameId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadForActionAsync(gameId, cancellationToken);
        if (loaded.Error is not null)
            return loaded.Error;

        var game = loaded.Game!;
        if (game.Status != GameStatus.InProgress ||
            _gameEngine.GetCurrentPlayer(game).Type == PlayerType.Human)
        {
            return Ok(new GameActionResponse(game));
        }

        return await CommitActionAsync(game, () => null, cancellationToken);
    }

    private async Task<(GameEvent? Game, IActionResult? Error)> LoadForActionAsync(
        string gameId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
            return (null, Unauthorized());

        var game = await _gameService.GetByIdForUserAsync(gameId, userId, cancellationToken);
        return game is null ? (null, NotFound()) : (game, null);
    }

    private async Task<IActionResult> CommitActionAsync(
        GameEvent game,
        Func<RoundResult?> mutation,
        CancellationToken cancellationToken,
        Func<RoundResult?, Task<string?>>? buildCommentary = null)
    {
        var expectedVersion = game.Version;
        RoundResult? roundResult;
        List<CountryChange> baseline;
        List<GameLogEntry> newLog;
        try
        {
            roundResult = mutation();
            baseline = GameLog.Snapshot(game);                      // the board before any NPC moves
            newLog = _npcService.PlayUntilHumanTurn(game);          // NPC turns, each with a justification
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        await AddCommentaryAsync(game, newLog, cancellationToken);
        GameLog.Append(game, newLog);

        var saved = await _gameService.ReplaceForUserAsync(
            game,
            expectedVersion,
            cancellationToken);
        if (!saved)
            return Conflict(new { message = "This campaign changed in another request. Reload it and try again." });

        var commentary = buildCommentary is null ? null : await buildCommentary(roundResult);
        return Ok(new GameActionResponse(game, roundResult, commentary, newLog, baseline));
    }

    // Commentary on every dice round (the LLM is used when the free-tier gap allows, templates otherwise).
    private async Task AddCommentaryAsync(
        GameEvent game,
        IEnumerable<GameLogEntry> entries,
        CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            if (entry.Type != LogTypes.DiceRound || entry.Result is null) continue;

            var attacker = game.Countries.FirstOrDefault(c => c.Code == entry.AttackerCode)?.Name ?? "The attacker";
            var defender = game.Countries.FirstOrDefault(c => c.Code == entry.DefenderCode)?.Name ?? "the defender";
            var input = CommentaryFacts.ForDiceRound(attacker, defender, entry.Result);
            entry.Commentary = await _commentary.GenerateAsync(input.Facts, input.Fallback, cancellationToken);
        }
    }

    private static Player HumanPlayer(GameEvent game) =>
        game.Players.First(player => player.Type == PlayerType.Human);

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub");
}