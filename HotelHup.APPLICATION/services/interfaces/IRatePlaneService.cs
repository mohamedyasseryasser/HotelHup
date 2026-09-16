using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.RatePlane;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IRatePlanService
    {
        Task<ResponseStatus<List<RatePlanResponse>>> GetAllAsync(User actor,int propertyid, CancellationToken ct = default);
        Task<ResponseStatus<RatePlanResponse>> GetAsync(User actor, int id, CancellationToken ct = default);
        Task<ResponseStatus<RatePlanResponse>> CreateAsync(User actor,int propertyid,int roomtypeid, CreateRatePlanRequest request, CancellationToken ct = default);
        Task<ResponseStatus<RatePlanResponse>> UpdateAsync(User actor, string expectedRowVersion, int id, UpdateRatePlanRequest request, CancellationToken ct = default);
        Task<ResponseStatus<List<RatePlanVersionResponse>>> GetVersionsAsync(User actor, int id, CancellationToken ct = default);
        Task<ResponseStatus<RatePlanVersionResponse>> GetVersionAsync(User actor, int id, int versionNumber, CancellationToken ct = default);
        Task<ResponseStatus<RatePlanResponse>> DeactivateAsync(User actor, string expectedRowVersion, int id, CancellationToken ct = default);
    }

}
