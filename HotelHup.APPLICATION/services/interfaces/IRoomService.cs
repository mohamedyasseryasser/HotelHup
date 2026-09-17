using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.room;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IRoomService
    {
        Task<ResponseStatus<PagedResponse<RoomListItemResponse>>> GetListAsync(User actor, RoomListRequest request, CancellationToken ct = default);
        Task<ResponseStatus<RoomResponse>> GetAsync(User actor, int propertyId, int id, CancellationToken ct = default);
        Task<ResponseStatus<RoomResponse>> CreateAsync(User actor, int propertyId, CreateRoomRequest request, CancellationToken ct = default);
        Task<ResponseStatus<RoomResponse>> UpdateAsync(User actor, int propertyId, int id, UpdateRoomRequest request, CancellationToken ct = default);
        Task<ResponseStatus<RoomResponse>> DeactivateAsync(User actor, int propertyId, int id, DeactivateRoomRequest request, CancellationToken ct = default);
    }

}
