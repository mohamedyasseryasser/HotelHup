using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property.propertytaxdto;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface IPropertyTax
    {
        Task<ResponseStatus<IReadOnlyList<TaxResponse>>> GetTaxesAsync(User actor, int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> GetTaxAsync(User actor, int propertyId, int taxId, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>> CreateTaxAsync(User actor, int propertyId, CreateTaxRequest request, CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>>
              UpdateTaxAsync(
                  User actor,
                  int propertyId,
                  int taxId,
                  UpdateTaxRequest request,
                  string ifMatch,
                  CancellationToken ct = default);
        Task<ResponseStatus<TaxResponse>>
   ActivateTaxAsync(
       User actor,
       int propertyId,
       int taxId,
       string ifMatch,
       CancellationToken ct = default);

       Task<ResponseStatus<TaxResponse>>
     DeactivateTaxAsync(
         User actor,
         int propertyId,
         int taxId,
         string ifMatch,
         ChangeTaxStatusRequest request,
         CancellationToken ct = default);


    }
}
