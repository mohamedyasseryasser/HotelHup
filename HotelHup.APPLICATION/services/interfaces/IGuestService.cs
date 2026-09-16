using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.guest;
using HotelHup.APPLICATION.services.implementation;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IGuestService
    {
        Task<ResponseStatus<GuestResponse>> CreateAsync(User actor, int propertyid,CreateGuestRequest request, CancellationToken ct = default);
        Task<ResponseStatus<GuestResponse>> GetByIdAsync(User actor,int propertyid, int id, CancellationToken ct = default);
        Task<ResponseStatus<PagedResponse<GuestListItemResponse>>> GetListAsync(User actor, int propertyid,GuestListRequest request, CancellationToken ct = default);
        Task<ResponseStatus<GuestResponse>> UpdateAsync(User actor,int propertyid, int id, UpdateGuestRequest request, CancellationToken ct = default);
        Task<ResponseStatus<GuestResponse>> DeactivateAsync(User actor, int propertyid,int id, DeactivateGuestRequest request, CancellationToken ct = default);
        Task<ResponseStatus<GuestResponse>> AnonymizeAsync(User actor,int propertyid, int id, AnonymizeGuestRequest request, CancellationToken ct = default);
        Task<ResponseStatus<IReadOnlyList<GuestReservationSummaryResponse>>> GetReservationHistoryAsync(User actor, int propertyid,int id, CancellationToken ct = default);
    }
}
