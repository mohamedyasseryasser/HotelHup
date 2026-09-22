using HotelHup.APPLICATION.DTO.reservation;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{


    public interface IReservationRepository
    {
        Task<Property?> GetPropertyAsync(int propertyId, CancellationToken ct = default);
        Task<Guest?> GetGuestAsync(int propertyId, int guestId, CancellationToken ct = default);
        Task<Room?> GetRoomAsync(int propertyId, int roomId, bool tracking, CancellationToken ct = default);
        Task<RatePlan?> GetRatePlanAsync(int propertyId, int roomTypeId, int? ratePlanId, DateTime stayDate, CancellationToken ct = default);
        Task<CancellationPolicyVersion?> GetCancellationPolicyVersionAsync(int propertyId, int policyId, int? versionId, DateTimeOffset stayDate, CancellationToken ct = default);
        Task<DepositPolicyVersion?> GetDepositPolicyVersionAsync(int propertyId, int policyId, int? versionId, DateTimeOffset stayDate, CancellationToken ct = default);
        Task<IReadOnlyList<Tax>> GetActiveTaxesAsync(int propertyId, DateTimeOffset stayDate, CancellationToken ct = default);
        Task<IReadOnlyList<Room>> GetAvailableRoomsAsync(int propertyId, DateTime checkIn, DateTime checkOut, int adults, int children, int? roomTypeId, int? excludedReservationId, CancellationToken ct = default);
        Task<Reservation?> GetByIdAsync(int propertyId, int id, bool tracking, CancellationToken ct = default);
        Task<(IReadOnlyList<Reservation> Items, int TotalCount)> SearchAsync(int propertyId, ReservationSearchRequest request, CancellationToken ct = default);
        Task AddAsync(Reservation reservation, Folio folio, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task PersistLifecycleAsync(Reservation reservation, IReadOnlyCollection<Room> rooms, IReadOnlyCollection<HousekeepingTask>? housekeepingTasks = null, CancellationToken ct = default);
    }

}
