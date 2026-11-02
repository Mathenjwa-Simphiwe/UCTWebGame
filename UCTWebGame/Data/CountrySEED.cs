using System.Text.Json;
using UCTWebGame.Models;

namespace UCTWebGame.Data
{
    public class CountrySEED
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true

        };

        //this method reads countries.json 

        public static List<CountryInfo> LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var countries = JsonSerializer.Deserialize<List<CountryInfo>>(json, Options)
                ?? throw new InvalidOperationException("countries.json is empty or invalid.");

            // Recompute from population so the JSON can never disagree with the rules.
            foreach (var c in countries)
                c.StartingSoldiers = SoldierTable.SoldiersFor(c.Population);

            return countries;
        }
    }
}
