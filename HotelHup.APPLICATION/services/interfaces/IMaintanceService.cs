using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.maintenance;
using HotelHup.CORE.Entities;

namespace HotelHup.APPLICATION.services.interfaces;

public interface IMaintenanceService
{
    Task<ResponseStatus<PagedResponse<MaintenanceResponseDto>>> GetListAsync(
        User actor,
        MaintenanceListRequest request,
        CancellationToken ct = default);

    Task<ResponseStatus<MaintenanceResponseDto>> GetAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        CancellationToken ct = default);

    Task<ResponseStatus<MaintenanceResponseDto>> CreateAsync(
        User actor,
        CreateMaintenanceRequest request,
        CancellationToken ct = default);

    Task<ResponseStatus<MaintenanceResponseDto>> UpdateAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        UpdateMaintenanceRequest request,
        CancellationToken ct = default);

    Task<ResponseStatus<MaintenanceResponseDto>> AssignAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        AssignMaintenanceRequest request,
        CancellationToken ct = default);

    Task<ResponseStatus<MaintenanceResponseDto>> StartAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        MaintenanceActionRequest request,
        CancellationToken ct = default);

    Task<ResponseStatus<MaintenanceResponseDto>> CompleteAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        MaintenanceActionRequest request,
        CancellationToken ct = default);
}
