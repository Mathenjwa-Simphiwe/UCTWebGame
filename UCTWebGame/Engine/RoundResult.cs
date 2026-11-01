namespace UCTWebGame.Engine
{
    public class RoundResult
    {
        public List<int> AttackerDice { get; set; } = new();
        public List<int> DefenderDice { get; set; } = new();
        public int AttackerLosses { get; set; }
        public int DefenderLosses { get; set; }
        public int AttackerSoldiersAfter { get; set; }
        public int DefenderSoldiersAfter { get; set; }

        public bool Conquered { get; set; }          // defender hit 0 soldiers
        public bool AttackEnded { get; set; }        // conquered, or attacker down to 1 soldier
        public int SoldiersGained { get; set; }      // only when Conquered

        public string? EliminatedPlayerId { get; set; }   // set when this conquest wiped a player out
        public bool GameOver { get; set; }
        public string? WinnerId { get; set; }             // null if the game ended because the human was eliminated
    }
}
