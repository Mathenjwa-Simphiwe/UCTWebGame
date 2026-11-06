namespace UCTWebGame.Services;

public interface ICommentaryService
{
    // facts    = plain description of what just happened (built by CommentaryFacts)
    // fallback = text to use if the LLM is unavailable, slow, or out of credit
    Task<string> GenerateAsync(string facts, string fallback, CancellationToken cancellationToken = default);
}
