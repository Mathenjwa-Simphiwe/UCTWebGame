namespace UCTWebGame.Models;

// A country as it exists inside one specific game.
public class CountryState
{
	public string Code { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public int Soldiers { get; set; }
	public string OwnerId { get; set; } = string.Empty;        // Player.Id of the current owner

	// Kept so the conquest bonus can use the country's original value:
	// Max(2, StartingSoldiers / 2)
	public int StartingSoldiers { get; set; }
}







