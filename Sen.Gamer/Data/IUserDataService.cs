
namespace Senla.Gamer.Data
{
    /// <summary>
    /// Service to retrieve user's data in database.
    /// </summary>
    public interface IUserDataService : IEntityService<IUserData>
    {
        /// <summary>
        /// Check if user with <c>username</c> existed in database.
        /// </summary>
        /// <param name="username">username to check</param>
        /// <returns>true if existed, false if otherwise</returns>
        bool IsUserExisted(string username);

        /// <summary>
        /// Try to login.
        /// </summary>
        /// <param name="username">username</param>
        /// <param name="password">password</param>
        /// <returns>UserData if logged in successfully, null if failed</returns>
        IUserData Login(string username, string password);

        /// <summary>
        /// Create new user data.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        IUserData CreateNewUser(string username, string password);
    }
}
