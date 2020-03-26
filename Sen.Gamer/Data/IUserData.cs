using System;

namespace Senla.Gamer.Data
{
    public interface IUserData : IEntity
    {
        string UserName { get; set; }

        string Password { get; set; }

        DateTime CreateTime { get; set; }

        DateTime LastTimeLogin { get; set; }

        string Email { get; set; }

        string PhoneNo { get; set; }

        string AvatarUrl { get; set; }

        byte Gender { get; set; }

        byte Age { get; set; }

        long Gold { get; set; }

        int Points { get; set; }

        byte Level { get; set; }

        string DisplayName { get; set; }
    }
}
