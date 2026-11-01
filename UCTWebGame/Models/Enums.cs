namespace UCTWebGame.Models;

public enum GameStatus { Setup, InProgress, Finished }

// Reinforce = player still has new soldiers to place
// Action    = player may attack, transfer or skip
// Attacking = an attack is in progress (transfers not allowed)
public enum TurnPhase { Reinforce, Action, Attacking }

public enum PlayerType { Human, Npc }

public enum EventSeverity { Info, Warning, Critical }

public enum GameEventType
{
    GameStarted,
    TurnStart,
    Reinforce,
    Transfer,
    NpcDecision,
    AttackStarted,
    DiceRound,
    Conquest,
    AttackStopped,
    TurnSkipped,
    Eliminated,
    GameOver
}