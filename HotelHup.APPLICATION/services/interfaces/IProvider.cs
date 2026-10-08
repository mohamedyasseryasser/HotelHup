using HotelHup.CORE.Entities;

namespace HotelHup.APPLICATION.services.interfaces;

public interface ISystemActorProvider
{
    Task<User?> GetAsync(CancellationToken ct = default);
}
