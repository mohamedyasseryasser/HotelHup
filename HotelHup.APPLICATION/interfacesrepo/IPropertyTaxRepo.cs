using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IPropertyTaxRepo
    {
        Task<IReadOnlyList<Tax>>
     GetCurrentTaxesAsync(
         int propertyId,
         DateTimeOffset now,
         CancellationToken ct = default);

        Task<Tax?>
            GetTaxAsync(
                int propertyId,
                int taxId,
                CancellationToken ct = default);

        Task<Tax?>
            GetTrackedTaxAsync(
                int propertyId,
                int taxId,
                CancellationToken ct = default);

        Task<bool>
            TaxCodeExistsAsync(
                int propertyId,
                string code,
                int? excludedTaxId = null,
                CancellationToken ct = default);

        Task<bool>
            HasTaxOverlapAsync(
                int propertyId,
                string code,
                DateTimeOffset validFrom,
                DateTimeOffset? validTo,
                int? excludedTaxId = null,
                CancellationToken ct = default);

        Task<int>
            CountActiveTaxesAsync(
                int propertyId,
                int? excludedTaxId = null,
                CancellationToken ct = default);

        Task<int>
            CountActiveReservationsUsingTaxAsync(
                int propertyId,
                int taxId,
                CancellationToken ct = default);




    }
}
