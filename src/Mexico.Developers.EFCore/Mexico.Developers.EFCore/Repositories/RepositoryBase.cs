using Mexico.Developers.Core.Abstractions;
using Mexico.Developers.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace Mexico.Developers.EFCore.Repositories;

/// <summary>
/// This interface implement the most concurrent methods with the database
/// </summary>
public abstract class RepositoryBase<TKey, TUserKey> : IRepositoryBase<TKey, TUserKey>
{
    /// <summary>
    /// Represents a session with the database and can be used to query and save instances of your entities
    /// </summary>
    protected readonly DbContext Context;

    /// <summary>
    /// Create a new instace of Repository
    /// </summary>
    /// <param name="context">Represents a session with the database and can be used to query and save instances of your entities</param>
    /// <exception cref="ArgumentNullException">If context is null</exception>
    protected RepositoryBase(DbContext context)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Method that creates an entity in the database
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to create</typeparam>
    /// <param name="entity">Entity to create</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>Represents an asynchronous operation that can return a value.</returns>
    public Task<TKey> CreateItemAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return this.ProcessCreateItemAsync(entity, cancellationToken);
    }
    /// <summary>
    /// Method that creates an entity in the database
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to create</typeparam>
    /// <param name="entity">Entity to create</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>Represents an asynchronous operation that can return a value.</returns>
    private async Task<TKey> ProcessCreateItemAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        await this.Context.AddAsync(entity, cancellationToken);
        await this.Context.SaveChangesAsync(cancellationToken);
        return entity.Id;

    }
    /// <summary>
    /// Method that deletes an entity in the database
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to delete</typeparam>
    /// <param name="id">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>Represents an asynchronous operation that can return a value.</returns>
    public Task<bool> DeleteAsync<TEntity>(TKey id, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        if (id == null)
            throw new ArgumentException($"{nameof(id)} can't be null or zero");

        return this.ProcessDeleteAsync<TEntity>(id, cancellationToken);
    }
    /// <summary>
    /// Method that deletes an entity in the database
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to delete</typeparam>
    /// <param name="id">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>Represents an asynchronous operation that can return a value.</returns>
    private async Task<bool> ProcessDeleteAsync<TEntity>(TKey id, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey> 
    {
        var entity = await this.Context.Set<TEntity>().FirstOrDefaultAsync( x => (x.Id != null && x.Id.Equals(id)), cancellationToken);

        if (entity != null)
        {
            this.Context.Set<TEntity>().Remove(entity);

            return await this.Context.SaveChangesAsync(cancellationToken) > 0;
        }

        return false;
    }
    /// <summary>
    /// Get a DbSet that can be used to query and save instances of TEntity.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity for which a set should be returned.</typeparam>
    /// <returns>A set for the given entity type.</returns>
    public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(bool state, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        return await this.Context.Set<TEntity>().AsNoTracking().Where(x => x.State == state).ToListAsync(cancellationToken);
    }
    /// <summary>
    /// Get a a entity by Id.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TEntity> GetAsync<TEntity>(TKey id, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        var entity = this.Context.Set<TEntity>().AsNoTracking();
        try
        {
            return await entity.FirstAsync(x => Equals(x.Id, id), cancellationToken);
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case InvalidOperationException:
                    throw new EntityNotFoundException($"The entity with id {id} not found");
                default:
                    throw;
            }
        }
    }

    /// <summary>
    /// Method that updates an entity in the database
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to udpate</typeparam>
    /// <param name="entity">Entity to update</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>Represents an asynchronous operation that can return a value.</returns>
    public Task<bool> UpdateItemAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return this.ProcessUpdateItemAsync(entity, cancellationToken);
    }
    /// <summary>
    /// Method that updates an entity in the database
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity to udpate</typeparam>
    /// <param name="entity">Entity to update</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>Represents an asynchronous operation that can return a value.</returns>
    private async Task<bool> ProcessUpdateItemAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntityBase<TKey, TUserKey>
    {
        this.Context.Set<TEntity>().Update(entity);

        this.Context.Entry(entity).Property(nameof(IEntityBase<TKey, TUserKey>.UserCreatorId)).IsModified = false;
        this.Context.Entry(entity).Property(nameof(IEntityBase<TKey, TUserKey>.CreatedDate)).IsModified = false;

        var success = await this.Context.SaveChangesAsync(cancellationToken) > 0;

        return success;
    }
}
