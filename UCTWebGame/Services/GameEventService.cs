namespace UCTWebGame.Data
{
    ///ROUGH DRAFT: This is a utility class to compute the odds of a single round of dice rolls in a battle.
    public readonly struct BattleOdds
    {
        private const int DefaultDieSides = 6;

        /// <summary>The number of attacker/defender die comparisons this round.</summary>
        public int DicePairs { get; }

        /// <summary>The number of sides on each fair die.</summary>
        public int DieSides { get; }

        /// <summary>Chance that the attacker wins one comparison.</summary>
        public double AttackerWinProbabilityPerPair { get; }

        /// <summary>Chance that the defender wins one comparison.</summary>
        public double DefenderWinProbabilityPerPair { get; }

        /// <summary>Chance that one comparison is a tie.</summary>
        public double TieProbabilityPerPair { get; }

        /// <summary>Expected attacker casualties in this dice round.</summary>
        public double ExpectedAttackerLosses => DicePairs * DefenderWinProbabilityPerPair;

        /// <summary>Expected defender casualties in this dice round.</summary>
        public double ExpectedDefenderLosses => DicePairs * AttackerWinProbabilityPerPair;

        /// <summary>Chance the attacker loses at least one soldier this round.</summary>
        public double ProbabilityAttackerLosesSoldier =>
            1d - Math.Pow(1d - DefenderWinProbabilityPerPair, DicePairs);

        /// <summary>Chance the defender loses at least one soldier this round.</summary>
        public double ProbabilityDefenderLosesSoldier =>
            1d - Math.Pow(1d - AttackerWinProbabilityPerPair, DicePairs);

        /// <summary>
        /// Creates odds for a round containing the specified number of paired rolls.
        /// For this game, each die is a fair six-sided die unless another side count is supplied.
        /// </summary>
        public BattleOdds(int dicePairs, int dieSides = DefaultDieSides)
        {
            if (dicePairs < 0)
                throw new ArgumentOutOfRangeException(nameof(dicePairs), "Dice pairs cannot be negative.");
            if (dieSides < 2)
                throw new ArgumentOutOfRangeException(nameof(dieSides), "A die must have at least two sides.");

            DicePairs = dicePairs;
            DieSides = dieSides;

            // For two identical fair dice, either side wins with equal probability;
            // ties account for one outcome per side of the die.
            TieProbabilityPerPair = 1d / dieSides;
            AttackerWinProbabilityPerPair = (1d - TieProbabilityPerPair) / 2d;
            DefenderWinProbabilityPerPair = AttackerWinProbabilityPerPair;
        }

        /// <summary>
        /// Builds one-round odds using the game's rule that the attacker keeps
        /// one soldier behind and both sides roll the same number of dice.
        /// </summary>
        public static BattleOdds ForArmies(int attackerSoldiers, int defenderSoldiers)
        {
            if (attackerSoldiers < 2)
                throw new ArgumentOutOfRangeException(nameof(attackerSoldiers), "The attacker needs at least two soldiers.");
            if (defenderSoldiers < 1)
                throw new ArgumentOutOfRangeException(nameof(defenderSoldiers), "The defender needs at least one soldier.");

            int dicePairs = Math.Min(attackerSoldiers - 1, defenderSoldiers);
            return new BattleOdds(dicePairs);
        }
    }
}
