using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.services;
using HotelHup.CORE.Entities;

namespace HotelHup.APPLICATION.services.interfaces;

public interface IServiceService
{
    Task<ResponseStatus<PagedResponse<ServiceResponse>>> GetListAsync(
        User actor,
        ServiceListRequest request,
        CancellationToken cancellationToken = default);

    Task<ResponseStatus<ServiceResponse>> GetAsync(
        User actor,
        int propertyId,
        int serviceId,
        CancellationToken cancellationToken = default);

    Task<ResponseStatus<ServiceResponse>> CreateAsync(
        User actor,
        int propertyId,
        CreateServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<ResponseStatus<ServiceResponse>> UpdateAsync(
        User actor,
        int propertyId,
        int serviceId,
        UpdateServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<ResponseStatus<ServiceResponse>> DeactivateAsync(
        User actor,
        int propertyId,
        int serviceId,
        DeactivateServiceRequest request,
        CancellationToken cancellationToken = default);
}
