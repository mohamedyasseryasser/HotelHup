using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.reservation;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IReservationService
    {
        Task<ResponseStatus<IReadOnlyList<AvailabilityItemResponse>>> SearchAvailabilityAsync(User actor, AvailabilityRequest request, CancellationToken ct = default);
        Task<ResponseStatus<PagedReservationResponse>> ListAsync(User actor, ReservationSearchRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationResponse>> GetAsync(User actor, int propertyId, int id, CancellationToken ct = default);
        Task<ResponseStatus<ReservationResponse>> CreateAsync(User actor, CreateReservationRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationResponse>> UpdateAsync(User actor, int propertyId, int id, UpdateReservationRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationActionResponse>> ConfirmAsync(User actor, int propertyId, int id, ConfirmReservationRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationActionResponse>> CancelAsync(User actor, int propertyId, int id, CancelReservationRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationActionResponse>> AssignRoomAsync(User actor, int propertyId, int id, AssignRoomRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationActionResponse>> ReleaseRoomAsync(User actor, int propertyId, int id, int reservationRoomId, CancellationToken ct = default);
        Task<ResponseStatus<ReservationActionResponse>> CheckInAsync(User actor, int propertyId, int id, CheckInRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationActionResponse>> CheckOutAsync(User actor, int propertyId, int id, CheckOutRequest request, CancellationToken ct = default);
        Task<ResponseStatus<ReservationActionResponse>> NoShowAsync(User actor, int propertyId, int id, NoShowRequest request, CancellationToken ct = default);
        Task<ResponseStatus<IReadOnlyList<ReservationStatusHistoryResponse>>> GetStatusHistoryAsync(User actor, int propertyId, int id, CancellationToken ct = default);
    }

}
