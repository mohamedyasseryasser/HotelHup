using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{
    public class PropertyTaxRepo:IPropertyTaxRepo
    {
        public hotelhupContext _context { get; }

        public PropertyTaxRepo(hotelhupContext context)
        {
            _context = context;
        }
 public async Task<IReadOnlyList<Tax>>
    GetCurrentTaxesAsync(
        int propertyId,
        DateTimeOffset now,
        CancellationToken ct = default)
        {
            return await _context.Taxes
                .AsNoTracking()
                .Where(x =>
                    x.PropertyId == propertyId &&
                    x.IsActive &&
                    x.ValidFrom <= now &&
                    (
                        x.ValidTo == null ||
                        now < x.ValidTo
                    ))
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);
        }


        public async Task<Tax?>
        GetTaxAsync(
            int propertyId,
            int taxId,
            CancellationToken ct = default)
        {
            return await _context.Taxes
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == taxId,
                    ct);
        }


        public async Task<Tax?>
     GetTrackedTaxAsync(
         int propertyId,
         int taxId,
         CancellationToken ct = default)
        {
            return await _context.Taxes
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == taxId,
                    ct);
        }

        public async Task<bool>
     TaxCodeExistsAsync(
         int propertyId,
         string code,
         int? excludedTaxId = null,
         CancellationToken ct = default)
        {
            var normalizedCode =
                code.Trim().ToUpperInvariant();

            return await _context.Taxes
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Code == normalizedCode &&
                        (
                            !excludedTaxId.HasValue ||
                            x.Id != excludedTaxId.Value
                        ),
                    ct);
        }

        public async Task<bool>
     HasTaxOverlapAsync(
         int propertyId,
         string code,
         DateTimeOffset validFrom,
         DateTimeOffset? validTo,
         int? excludedTaxId = null,
         CancellationToken ct = default)
        {
            var normalizedCode =
                code.Trim().ToUpperInvariant();

            var requestedTo =
                validTo ?? DateTimeOffset.MaxValue;

            return await _context.Taxes
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Code == normalizedCode &&
                        x.IsActive &&
                        (
                            !excludedTaxId.HasValue ||
                            x.Id != excludedTaxId.Value
                        ) &&
                        x.ValidFrom < requestedTo &&
                        (
                            x.ValidTo == null ||
                            x.ValidTo > validFrom
                        ),
                    ct);
        }

        public async Task<int>
          CountActiveTaxesAsync(
              int propertyId,
              int? excludedTaxId = null,
              CancellationToken ct = default)
        {
            return await _context.Taxes
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.IsActive &&
                        (
                            !excludedTaxId.HasValue ||
                            x.Id != excludedTaxId.Value
                        ),
                    ct);
        }

        public async Task<int>
         CountActiveReservationsUsingTaxAsync(
             int propertyId,
             int taxId,
             CancellationToken ct = default)
        {
            return await _context.reservationTaxSnapshots
                .AsNoTracking()
                .Where(x =>
                    x.TaxId == taxId &&
                    x.Reservation.PropertyId == propertyId &&
                    (
                        x.Reservation.Status ==
                            ReservationStatus.Confirmed ||
                        x.Reservation.Status ==
                            ReservationStatus.CheckedIn
                    ))
                .Select(x => x.ReservationId)
                .Distinct()
                .CountAsync(ct);
        }

  
 
    }
}
