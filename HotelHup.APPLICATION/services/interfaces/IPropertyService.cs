using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IPropertyService
    {
        Task<ResponseStatus<PropertyListResponse>> GetListAsync(User user,PropertyListRequest request, CancellationToken ct = default);
        Task<ResponseStatus<PropertyResponse>> GetAsync(User user,int id, CancellationToken ct = default);
        Task<ResponseStatus<PropertyResponse>> CreateAsync(User CurrentUserLogin, CreatePropertyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<PropertyResponse>> UpdateAsync(User CurrentUserLogin,int id, UpdatePropertyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<PropertyResponse>> ActivateAsync(int id, CancellationToken ct = default);
        Task<ResponseStatus<PropertyResponse>> DeactivateAsync(int id, DeactivatePropertyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<PropertySettingsResponse>> GetSettingsAsync(int id, CancellationToken ct = default);
        Task<ResponseStatus<PropertySettingsResponse>> UpdateSettingsAsync(int id, UpdatePropertySettingsRequest request, CancellationToken ct = default);
        Task<ResponseStatus<IReadOnlyList<TaxResponse>>> GetTaxesAsync(int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> GetTaxAsync(int propertyId, int taxId, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> CreateTaxAsync(int propertyId, CreateTaxRequest request, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> UpdateTaxAsync(int propertyId, int taxId, UpdateTaxRequest request, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> SetTaxStatusAsync(int propertyId, int taxId, bool active, CancellationToken ct = default);
        Task<ResponseStatus<IReadOnlyList<CancellationPolicyResponse>>> GetCancellationPoliciesAsync(int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<CancellationPolicyResponse>> GetCancellationPolicyAsync(int propertyId, int id, CancellationToken ct = default);
        Task<ResponseStatus<CancellationPolicyResponse>> CreateCancellationPolicyAsync(int propertyId, CreateCancellationPolicyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<CancellationPolicyResponse>> UpdateCancellationPolicyAsync(int propertyId, int id, UpdateCancellationPolicyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<CancellationPolicyResponse>> SetCancellationPolicyStatusAsync(int propertyId, int id, bool active, CancellationToken ct = default);
        Task<ResponseStatus<IReadOnlyList<DepositPolicyResponse>>> GetDepositPoliciesAsync(int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> GetDepositPolicyAsync(int propertyId, int id, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> CreateDepositPolicyAsync(int propertyId, CreateDepositPolicyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> UpdateDepositPolicyAsync(int propertyId, int id, UpdateDepositPolicyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> SetDepositPolicyStatusAsync(int propertyId, int id, bool active, CancellationToken ct = default);
    }
}