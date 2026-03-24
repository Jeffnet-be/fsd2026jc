using ChampionsLeague.Domain.Interfaces;
using ChampionsLeague.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChampionsLeague.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation wrapping EF Core DbContext.
/// All concrete repositories inherit this class to avoid repeating CRUD boilerplate.
/// The Repository pattern is discussed alongside DbContext in course section 10.3.
/// DbContext is injected with Scoped lifetime (one instance per HTTP request).
/// </summary>
/// <typeparam name="T">Domain entity type.</typeparam>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    /// <summary>
    /// Shared DbContext — Scoped lifetime means one instance per HTTP request.
    /// Protected so derived repositories can build LINQ queries against it.
    /// </summary>
    protected readonly AppDbContext _context;
    protected readonly DbSet<T>     _set;

    protected BaseRepository(AppDbContext context)
    {
        _context = context;
        _set     = context.Set<T>();
    }

    /// <inheritdoc/>
    public virtual async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();

    /// <inheritdoc/>
    public virtual async Task AddAsync(T entity) => await _set.AddAsync(entity);

    /// <inheritdoc/>
    public virtual void Update(T entity) => _set.Update(entity);

    /// <inheritdoc/>
    public virtual void Remove(T entity) => _set.Remove(entity);

    /// <inheritdoc/>
    public virtual async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
}
