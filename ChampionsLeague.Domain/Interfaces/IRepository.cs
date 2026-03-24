namespace ChampionsLeague.Domain.Interfaces;

/// <summary>
/// Generic repository contract — provides standard CRUD operations for any domain entity.
/// Using a generic interface means every entity gets consistent method signatures without
/// repeating code (DRY). Concrete implementations live in Infrastructure; callers
/// (controllers, services) depend only on this interface — enabling easy unit testing via mocks.
/// </summary>
/// <typeparam name="T">Any domain entity class.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Returns the entity with the given primary key, or null if not found.</summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>Returns all entities of type T from the database.</summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>Adds a new entity to the change tracker (not yet saved).</summary>
    Task AddAsync(T entity);

    /// <summary>Marks an entity as modified in the change tracker (not yet saved).</summary>
    void Update(T entity);

    /// <summary>Marks an entity for deletion in the change tracker (not yet saved).</summary>
    void Remove(T entity);

    /// <summary>Persists all pending change-tracker operations to the database.</summary>
    Task<int> SaveChangesAsync();
}
