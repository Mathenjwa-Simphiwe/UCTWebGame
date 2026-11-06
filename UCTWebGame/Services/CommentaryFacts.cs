using UCTWebGame.Engine;

namespace UCTWebGame.Services;

public record CommentaryInput(string Facts, string Fallback);

// Builds (1) the facts sent to the LLM and (2) a template fallback that uses the same real numbers.
public static class CommentaryFacts
{
    public static CommentaryInput ForAttackStart(string attacker, string defender, int attackerSoldiers, int defenderSoldiers)
    {
        var facts = $"{attacker} ({attackerSoldiers} soldiers) has just declared an attack on {defender} ({defenderSoldiers} soldiers).";
        var fallback = Pick(
            $"{attacker} launches an attack on {defender}!",
            $"{attacker} marches on {defender} with {attackerSoldiers} soldiers.",
            $"War! {attacker} takes aim at {defender}.");
        return new CommentaryInput(facts, fallback);
    }

    public static CommentaryInput ForDiceRound(string attacker, string defender, RoundResult r)
    {
        var attackerBefore = r.AttackerSoldiersAfter + r.AttackerLosses;
        var defenderBefore = r.DefenderSoldiersAfter + r.DefenderLosses;

        var facts =
            $"Attacker: {attacker} ({attackerBefore} soldiers). Defender: {defender} ({defenderBefore} soldiers). " +
            $"Attacker dice: {string.Join(", ", r.AttackerDice)}. Defender dice: {string.Join(", ", r.DefenderDice)}. " +
            $"{attacker} lost {r.AttackerLosses} soldier(s) and {defender} lost {r.DefenderLosses}. " +
            $"Now {attacker} has {r.AttackerSoldiersAfter} and {defender} has {r.DefenderSoldiersAfter}.";

        string fallback;
        if (r.Conquered)
        {
            facts += $" {defender} has been conquered and {attacker} gains {r.SoldiersGained} soldiers.";
            fallback = Pick(
                $"{defender} falls! {attacker} takes the country and gains {r.SoldiersGained} soldiers.",
                $"It's over for {defender}. {attacker} claims victory and {r.SoldiersGained} fresh troops.");
        }
        else if (r.AttackEnded)
        {
            facts += $" {attacker} is down to 1 soldier and the attack has failed.";
            fallback = Pick(
                $"{attacker} is spent and the assault on {defender} collapses.",
                $"{defender} holds the line as {attacker} runs out of soldiers.");
        }
        else if (r.AttackerLosses == 0 && r.DefenderLosses == 0)
        {
            fallback = Pick(
                "Dead even! Neither side loses a soldier.",
                $"A stalemate this round between {attacker} and {defender}.");
        }
        else if (r.DefenderLosses > r.AttackerLosses)
        {
            fallback = Pick(
                $"{attacker} wins the exchange! {defender} loses {r.DefenderLosses}.",
                $"{attacker} presses forward, costing {defender} {r.DefenderLosses} soldier(s).");
        }
        else if (r.AttackerLosses > r.DefenderLosses)
        {
            fallback = Pick(
                $"{defender} fights back and {attacker} loses {r.AttackerLosses}.",
                $"A costly round for {attacker}, down {r.AttackerLosses}.");
        }
        else
        {
            fallback = Pick(
                $"Blood on both sides! Each loses {r.AttackerLosses}.",
                $"{attacker} and {defender} trade losses of {r.AttackerLosses} each.");
        }

        return new CommentaryInput(facts, fallback);
    }

    private static string Pick(params string[] options) => options[Random.Shared.Next(options.Length)];
}
