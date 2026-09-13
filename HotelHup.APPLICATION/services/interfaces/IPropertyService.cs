using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property;
using HotelHup.APPLICATION.DTO.property.propertycancellationdtos;
using HotelHup.APPLICATION.DTO.property.propertydepositdtos;
using HotelHup.APPLICATION.DTO.property.propertydto;
using HotelHup.APPLICATION.DTO.property.propertysettingdto;
using HotelHup.APPLICATION.DTO.property.propertytaxdto;
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
        Task<ResponseStatus<PropertyResponse>> UpdateAsync(User CurrentUserLogin,string ifmatch,int id, UpdatePropertyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<PropertyResponse>> ActivateAsync(string expected,User user,int id, CancellationToken ct = default);
        Task<ResponseStatus<PropertyResponse>> DeactivateAsync(string expected,User user,int id, DeactivatePropertyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<PropertySettingsResponse>> GetSettingsAsync(User actor,int id, CancellationToken ct = default);
        Task<ResponseStatus<PropertySettingsResponse>> UpdateSettingsAsync(User actor,int id, string ifmatch,UpdatePropertySettingsRequest request, CancellationToken ct = default);
        Task<ResponseStatus<IReadOnlyList<TaxResponse>>> GetTaxesAsync(User actor,int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> GetTaxAsync(User actor,int propertyId, int taxId, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> CreateTaxAsync(User actor,int propertyId, CreateTaxRequest request, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> UpdateTaxAsync(User actor,int propertyId, int taxId, UpdateTaxRequest request, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> SetTaxStatusAsync(User actor,int propertyId, int taxId, bool active, CancellationToken ct = default);
      
        Task<ResponseStatus<IReadOnlyList<DepositPolicyResponse>>> GetDepositPoliciesAsync(User actor,int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> GetDepositPolicyAsync(User actor,int propertyId, int id, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> CreateDepositPolicyAsync(User actor,int propertyId, CreateDepositPolicyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> UpdateDepositPolicyAsync(User actor,int propertyId, int id, UpdateDepositPolicyRequest request, string ifmathc,CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> SetDepositPolicyStatusAsync(User actor,int propertyId, int id, bool active, CancellationToken ct = default);


    }
}