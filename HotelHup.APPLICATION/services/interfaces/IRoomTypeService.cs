using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.RoomType;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface IRoomTypeService
    {
        Task<ResponseStatus<List<RoomTypeResponse>>> GetAllAsync(User actor, RoomTypeListRequest request, CancellationToken ct = default);
        Task<ResponseStatus<RoomTypeResponse>> GetAsync(User actor, int id, CancellationToken ct = default);
        Task<ResponseStatus<RoomTypeResponse>> CreateAsync(User actor, int propertyid,CreateRoomTypeRequest request, CancellationToken ct = default);
        Task<ResponseStatus<RoomTypeResponse>> UpdateAsync(User actor, string expectedRowVersion, int id, UpdateRoomTypeRequest request, CancellationToken ct = default);
        Task<ResponseStatus<RoomTypeResponse>> DeactivateAsync(User actor, string expectedRowVersion, int id, CancellationToken ct = default);
    }
}
