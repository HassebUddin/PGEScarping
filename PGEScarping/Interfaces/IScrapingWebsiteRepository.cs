using PGEScarping.Models;

namespace PGEScarping.Interfaces;

public interface IScrapingWebsiteRepository
{
    Task<ScrapingWebsite?> GetActiveBySourceTypeIdAsync(int sourceTypeId);
}
