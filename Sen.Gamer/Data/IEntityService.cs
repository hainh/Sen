
namespace Senla.Gamer.Data
{
    /// <summary>
    /// Interface to interact with database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityService<T> where T : IEntity
    {
        /// <summary>
        /// Save new entity to database.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Success (true) or failed (false)</returns>
        bool Create(T entity);

        /// <summary>
        /// Delete entity from database
        /// </summary>
        /// <param name="id">A value that is used as id</param>
        /// <returns>Success (true) or failed (false)</returns>
        bool Delete(object id);

        /// <summary>
        /// Get entity from databse.
        /// </summary>
        /// <param name="id">A value that is used as id</param>
        /// <returns>The entity if existed, null otherwise</returns>
        T GetById(object id);

        /// <summary>
        /// Write this entity to the databse.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Success (true) or failed (false)</returns>
        bool Update(T entity);
    }
}
