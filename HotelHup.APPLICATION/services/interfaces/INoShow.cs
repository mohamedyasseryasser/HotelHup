namespace HotelHup.APPLICATION.services.interfaces;

public interface INoShowProcessor
{
    Task ProcessAsync(CancellationToken ct = default);
}

