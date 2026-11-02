namespace UCTWebGame.Engine
{

    public interface IDice
    {
        int Roll();  //imitates the roll of dice
    }
    public class RandomDice : IDice
    {
        private readonly Random _rng;
        public RandomDice(int? seed = null) =>
            _rng = seed is null ? new Random() : new Random(seed.Value);
        public int Roll() => _rng.Next(1, 7);  //returns a number between 1 and 6
    }
}
