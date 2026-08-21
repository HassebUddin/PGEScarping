using Microsoft.EntityFrameworkCore;
using PGEScarping.Models;

namespace PGEScarping.Data;

public sealed class TechnoDevContext : DbContext
{
    public TechnoDevContext(DbContextOptions<TechnoDevContext> options) : base(options) { }

    public DbSet<ScrapingWebsite> ScrapingWebsites => Set<ScrapingWebsite>();
}
