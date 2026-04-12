using Shared.Models;

namespace SleeperFunctions.Application;

public interface IDraftService
{
    Task<List<DraftPicksModel>> GetDraftPicksAsync(string draft_id);
    Task<List<DraftsModel>> GetDraftsAsync(string league_id);
}