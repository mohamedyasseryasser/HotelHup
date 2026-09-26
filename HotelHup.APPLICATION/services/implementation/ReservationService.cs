using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.reservation;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static HotelHup.APPLICATION.Constant.Permissions;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class ReservationService : IReservationService    
{
    private readonly IUserRepository user;
    private readonly IReservationRepository _repository;
    public ReservationService(IUserRepository user, IReservationRepository repository)
    {
        this.user = user;
        _repository = repository;                                    
    }

    public async Task<ResponseStatus<IReadOnlyList<AvailabilityItemResponse>>> 
        SearchAvailabilityAsync(User actor,
        AvailabilityRequest request, 
        CancellationToken ct = default)
    {
        if (!HasScope(actor, request.PropertyId)) return Fail<IReadOnlyList<AvailabilityItemResponse>>("Property scope is invalid.", "PROPERTY_SCOPE_INVALID", 403);
        var dates = ValidateDates(request.CheckInDate, request.CheckOutDate);
        if (dates is not null) return Fail<IReadOnlyList<AvailabilityItemResponse>>(dates, "INVALID_DATES");
        var property = await _repository.GetPropertyAsync(request.PropertyId, ct);
        if (property is null) return Fail<IReadOnlyList<AvailabilityItemResponse>>("Property was not found.", "PROPERTY_NOT_FOUND", 404);
        if (property.Status != PropertyStatus.Active) return Fail<IReadOnlyList<AvailabilityItemResponse>>("Property is inactive.", "PROPERTY_INACTIVE");
        var rooms = await _repository.GetAvailableRoomsAsync(request.PropertyId, request.CheckInDate.Date, request.CheckOutDate.Date, request.Adults, request.Children, request.RoomTypeId, request.ExcludedReservationId, ct);
        var nights = (request.CheckOutDate.Date - request.CheckInDate.Date).Days;
        var result = rooms.Select(room =>
        {
            var rate = room.PricePerNight > 0 ? room.PricePerNight : room.RoomType?.BasePrice ?? 0m;
            return new AvailabilityItemResponse { RoomId = room.Id, RoomNumber = room.RoomNumber, RoomTypeId = room.RoomTypeId, RoomTypeName = room.RoomType?.Name ?? string.Empty, NightlyRate = rate, Nights = nights, TotalAmount = Money(rate * nights) };
        }).ToList();
        return Ok<IReadOnlyList<AvailabilityItemResponse>>(result);
    }

    public async Task<ResponseStatus<PagedReservationResponse>>
        ListAsync(User actor,
        ReservationSearchRequest request
        , CancellationToken ct = default)
    {
        var propertyId = request.PropertyId ?? actor.PropertyId;
        if (!propertyId.HasValue || !HasScope(actor, propertyId.Value)) return Fail<PagedReservationResponse>("A valid property scope is required.", "PROPERTY_SCOPE_REQUIRED", 403);
        var (items, total) = await _repository.SearchAsync(propertyId.Value, request, ct);
        return Ok(new PagedReservationResponse { Items = items.Select(MapSummary).ToList(), Page = request.Page, PageSize = request.PageSize, TotalCount = total });
    }

    public async Task<ResponseStatus<ReservationResponse>>
        GetAsync(User actor, 
        int propertyId,
        int id,
        CancellationToken ct = default)
    {
        if (!HasScope(actor, propertyId)) return Fail<ReservationResponse>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404);
        var reservation = await _repository.GetByIdAsync(propertyId, id, false, ct);
        return reservation is null ? Fail<ReservationResponse>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404) : Ok(Map(reservation));
    }

    public async Task<ResponseStatus<ReservationResponse>>
        CreateAsync(User actor,
        CreateReservationRequest request,
        CancellationToken ct = default) =>
    await     _repository.ExecuteSerializableAsync(
            () => CreateCoreAsync(actor, request, ct), ct);

    private async Task<ResponseStatus<ReservationResponse>>
        CreateCoreAsync(User actor,
        CreateReservationRequest request,
        CancellationToken ct = default)
    {
        //actor must be manager or admin or receptionist
        //must be manager or receptionist in scope property
        var auth = await AuthorizeAsync(actor, request.PropertyId, ct);
        {
            if (!auth.Success) return Fail<ReservationResponse>(auth.Message, auth.StatusCode);
        }
        //must be checkin < checkout
        var dates = ValidateDates(request.CheckInDate, request.CheckOutDate);
        if (dates is not null)
        {
            return Fail<ReservationResponse>(dates, "INVALID_DATES", 422);
        }
        //property must be not null and is active
        var property = await _repository.GetPropertyAsync(request.PropertyId, ct);
        if (property is null)
        {
            return Fail<ReservationResponse>("Property was not found.", "PROPERTY_NOT_FOUND", 404);
        }
        if (property.Status != PropertyStatus.Active)
        {
            return Fail<ReservationResponse>("Property is inactive.", "PROPERTY_INACTIVE", 409);
        }
        //guest must be not null and is active
        var guest = await _repository.GetGuestAsync(request.PropertyId, request.GuestId, ct);
        if (guest is null)
        {
            return Fail<ReservationResponse>("Guest was not found for this property.", "GUEST_NOT_FOUND", 404);
        }
        if (!guest.IsActive)
        {
            return Fail<ReservationResponse>("Guest is inactive.", "GUEST_INACTIVE", 409);
        }
        if (request.createReservationRoomRequests.Count == 0)
        {
            return Fail<ReservationResponse>("At least one room is required.", "ROOMS_REQUIRED", 422);
        }
        var requestedRooms = request.createReservationRoomRequests;
        //must each reservationromm has roomid or roomtypeid one at least
        if (requestedRooms.Any(x => !x.RoomId.HasValue && !x.RoomTypeId.HasValue))
        {
            return Fail<ReservationResponse>(
                "Each room request must contain RoomId or RoomTypeId.",
                "ROOM_REQUIRED",
                422);
        }
        var now = DateTimeOffset.UtcNow;
        var taxes = await _repository.GetActiveTaxesAsync(request.PropertyId, now, ct);
        var reservation = new Reservation
        {
            PropertyId = request.PropertyId,
            GuestId = request.GuestId,
            Status = ReservationStatus.Pending,
            CheckInDate = request.CheckInDate.Date,
            CheckOutDate = request.CheckOutDate.Date,
            Source = request.Source,
            CreatedAt = now,
            CreatedBy = actor.Id,
            CancellationPolicyId = request.CancellationPolicyId,
            CancellationPolicyVersionId = request.CancellationPolicyVersionId,
            DepositPolicyId = request.DepositPolicyId,
            DepositPolicyVersionId = request.DepositPolicyVersionId,

        };
        var selectedRoomIds = new HashSet<int>();
        foreach (var item in requestedRooms)
        {
            //if roomid hasvalue
            //{
            //must be active
            //get roomtypeid must be active and avaliable
            //if request roomtype has value must be equal request room.roomtypeid and active 
            //}
            //if roomid not has value
            //check request roomtypeid must be active 
            //check roomtype has avaliable rooms and reservate first room

            var room = item.RoomId.HasValue ?
                await _repository.GetRoomAsync(request.PropertyId, item.RoomId.Value, false, ct) : null;

            if (item.RoomId.HasValue && room is null)
            {
                return Fail<ReservationResponse>("Room was not found.", "ROOM_NOT_FOUND", 404);
            }
            if (room is not null && item.RoomTypeId.HasValue && room.RoomTypeId != item.RoomTypeId.Value)
            {
                return Fail<ReservationResponse>("Room type does not match.", "ROOM_TYPE_MISMATCH", 422);
            }
            //check to not double reservations 
            var typeId = item.RoomTypeId ?? room?.RoomTypeId;
            var available = await _repository.GetAvailableRoomsAsync(request.PropertyId,
                request.CheckInDate.Date,
                request.CheckOutDate.Date,
                item.Adults,
                item.Children,
                typeId,
                null,
                ct);
            room = room is null ? available.FirstOrDefault(x => selectedRoomIds.Add(x.Id)) :
                available.FirstOrDefault(x => x.Id == room.Id && selectedRoomIds.Add(x.Id));
            if (room is null)
            {
                return Fail<ReservationResponse>(
                    "One or more requested rooms are no longer available.",
                    "ROOM_NO_LONGER_AVAILABLE",
                    409);
            }

            var line = await BuildRoomLineAsync(
                item.DiscountAmount,
                item.FeeAmount,
                request.PropertyId,
                room,
                item.RatePlanId,
                item.Adults,
                item.Children,
                request.CheckInDate.Date,
                request.CheckOutDate.Date,
                taxes,
                actor.Id,
                now,
                ct);

            if (line is null) return Fail<ReservationResponse>(
                "No valid rate plan exists for one of the selected rooms.",
                "RATE_PLAN_NOT_FOUND",
                422);
            reservation.ReservationRooms.Add(line);
        }
        RecalculateTotals(reservation, taxes);
        foreach (var tax in taxes)
            reservation.ReservationTaxSnapshots.Add(new ReservationTaxSnapshot
            {
                TaxId = tax.Id,
                TaxCode = tax.Code,
                TaxName = tax.Name,
                Rate = tax.Rate,
                Type = tax.Type,
                IsInclusive = tax.IsInclusive,
                TaxableAmount = Money(reservation.BaseAmount - reservation.TotalDiscountAmount),
                TaxAmount = Money(tax.Type == TaxType.Percentage
                    ? (reservation.BaseAmount - reservation.TotalDiscountAmount) * tax.Rate / 100m
                    : tax.Rate),
                TaxValidFrom = tax.ValidFrom,
                TaxValidTo = tax.ValidTo,
                CreatedAt = now,
                CreatedBy = actor.Id
            });
        reservation.Adults = reservation.ReservationRooms.Sum(x => x.Adults);
        reservation.Children = reservation.ReservationRooms.Sum(x => x.Children);
        await AddPolicySnapshotsAsync(reservation, request, request.CheckInDate, now, ct);
        if (request.CancellationPolicyId.HasValue && reservation.CancellationPolicySnapshot is null)
        {
            return Fail<ReservationResponse>(
                "Cancellation policy was not found or is not valid for the stay date.", 
                "CANCELLATION_POLICY_NOT_FOUND",
                422);
        }
        if (request.DepositPolicyId.HasValue && reservation.DepositPolicySnapshot is null)
        {
            return Fail<ReservationResponse>(
                "Deposit policy was not found or is not valid for the stay date.",
                "DEPOSIT_POLICY_NOT_FOUND", 
                422);
        }
        AddHistory(reservation, null, ReservationStatus.Pending, "Reservation created", actor.Id, now);
        var folio = new Folio
        {
            Status = FolioStatus.Open,
            Currency = property.Settings?.DefaultCurrency ?? "USD",
            Total = reservation.TotalAmount,
            Balance = reservation.TotalAmount,
            CreatedAt = now,
            CreatedBy = actor.Id
        };
        try 
        {
            await _repository.AddAsync(reservation, folio, ct); 
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<ReservationResponse>(
                "The reservation could not be created because availability changed.",
                "CONCURRENCY_CONFLICT",
                409);
        }
        var created = await _repository.GetByIdAsync(
            request.PropertyId,
            reservation.Id,
            false,
            ct);

        return Ok(
            Map(created ?? reservation),
            "Reservation created.",
            201);
    }

    public async Task<ResponseStatus<ReservationResponse>>
        UpdateAsync(User actor,
        int propertyId,
        int id,
        UpdateReservationRequest request,
        CancellationToken ct = default) =>
    await     _repository.ExecuteSerializableAsync(
            () => UpdateCoreAsync(actor, propertyId, id, request, ct), ct);

    private async Task<ResponseStatus<ReservationResponse>>
        UpdateCoreAsync(User actor,
        int propertyId,
        int id,
        UpdateReservationRequest request,
        CancellationToken ct = default)
    {
        //actor must be manager or admin or receptionist
        //must be manager or receptionist in scope property
        var auth = await AuthorizeAsync(actor, propertyId, ct);
        {
            if (!auth.Success) return Fail<ReservationResponse>(auth.Message, auth.StatusCode);
        }
        var reservation = await _repository.GetByIdAsync(propertyId, id, true, ct);
        if (reservation is null)
        {
            return Fail<ReservationResponse>(
                "Reservation was not found.",
                "RESERVATION_NOT_FOUND",
                404);
        }
        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
        {
            return Fail<ReservationResponse>("Only pending or confirmed reservations can be modified.", "INVALID_STATE_TRANSITION", 409);
        }
        var checkIn = request.CheckInDate?.Date ?? reservation.CheckInDate.Date;
        var checkOut = request.CheckOutDate?.Date ?? reservation.CheckOutDate.Date;
        var dates = ValidateDates(checkIn, checkOut);
        if (dates is not null)
        {
            return Fail<ReservationResponse>(dates, "INVALID_DATES", 422);
        }
        if (request.ExpectedRowVersion is not null &&
            !reservation.RowVersion.SequenceEqual(request.ExpectedRowVersion))
        {
            return Fail<ReservationResponse>("The reservation was modified by another request.", "CONCURRENCY_CONFLICT", 409);
        }
        if (request.GuestId.HasValue)
        {
            var guest = await _repository.GetGuestAsync(propertyId, request.GuestId.Value, ct);
            if (guest is null || !guest.IsActive)
            {
                return Fail<ReservationResponse>("Guest was not found or is inactive.", "GUEST_NOT_FOUND", 404);
            }
            reservation.GuestId = request.GuestId.Value;
        }
        var replacements = request.Rooms;
        if (replacements.Count == 0 && (request.RoomId.HasValue || request.RoomTypeId.HasValue || request.RatePlanId.HasValue))
        {
            replacements = new List<UpdateReservationRoomRequest> 
            { new() 
            { RoomId = request.RoomId, RoomTypeId = request.RoomTypeId,
                RatePlanId = request.RatePlanId, Adults = request.Adults ?? reservation.Adults, 
                Children = request.Children ?? reservation.Children } 
            };
        }
        if (replacements.Count > 0 || 
            checkIn != reservation.CheckInDate.Date ||
            checkOut != reservation.CheckOutDate.Date ||
            request.Adults.HasValue ||
            request.Children.HasValue)
        {
            var source = replacements.Count > 0 ? replacements : reservation.ReservationRooms.Where(x => x.IsCurrent).
                Select(x => 
                new UpdateReservationRoomRequest 
                { ReservationRoomId = x.Id, RoomId = x.RoomId, RatePlanId = x.RatePlanId, Adults = x.Adults, 
                    Children = x.Children, DiscountAmount = x.DiscountAmount, FeeAmount = x.FeeAmount }).ToList();
            if (source.Count == 0)
            {
                return Fail<ReservationResponse>("At least one room is required.", "ROOMS_REQUIRED", 422);
            }
            var taxes = await _repository.GetActiveTaxesAsync(propertyId, DateTimeOffset.UtcNow, ct);
            var newLines = new List<ReservationRoom>();
            var selected = new HashSet<int>();
            foreach (var item in source)
            {
                var room = item.RoomId.HasValue ? await _repository.GetRoomAsync(propertyId, item.RoomId.Value, false, ct) : null;
                if (item.RoomId.HasValue && room is null)
                {
                    return Fail<ReservationResponse>("Room was not found.", "ROOM_NOT_FOUND", 404);
                }
                var requestedTypeId = item.RoomTypeId ?? room?.RoomTypeId;
                var available = await _repository.GetAvailableRoomsAsync(propertyId,
                    checkIn,
                    checkOut,
                    item.Adults,
                    item.Children,
                    requestedTypeId, 
                    id, 
                    ct);
                room ??= available.FirstOrDefault(x => !selected.Contains(x.Id));
                if (room is null)
                {
                    return Fail<ReservationResponse>(
                        "No room is available for one of the requested room types.",
                        "ROOM_NO_LONGER_AVAILABLE",
                        409);
                }
                if (item.RoomTypeId.HasValue && room.RoomTypeId != item.RoomTypeId.Value)
                {
                    return Fail<ReservationResponse>("Room type does not match.", "ROOM_TYPE_MISMATCH", 422);
                }
                if (!available.Any(x => x.Id == room.Id) || !selected.Add(room.Id))
                {
                    return Fail<ReservationResponse>("One or more rooms are unavailable.", "ROOM_NO_LONGER_AVAILABLE", 409);
                }
                var line = await BuildRoomLineAsync(item.DiscountAmount,
                    item.FeeAmount, 
                    propertyId,
                    room,
                    item.RatePlanId ?? room.RoomType?.RatePlans.FirstOrDefault()?.Id, 
                    item.Adults,
                    item.Children,
                    checkIn,
                    checkOut,
                    taxes,
                    actor.Id,
                    DateTimeOffset.UtcNow, ct);
                if (line is null)
                {
                    return Fail<ReservationResponse>("No valid rate plan exists.", "RATE_PLAN_NOT_FOUND", 422);
                }
                newLines.Add(line);
            }
            foreach (var old in reservation.ReservationRooms.Where(x => x.IsCurrent)) 
            {
                old.IsCurrent = false; 
                old.ReleasedAt = DateTimeOffset.UtcNow; old.AssignmentReason = "Replaced during reservation update";

            }
            foreach (var line in newLines)
            {
                reservation.ReservationRooms.Add(line);
            }
            reservation.CheckInDate = checkIn; 
            reservation.CheckOutDate = checkOut;
            RecalculateTotals(reservation, taxes);
            reservation.Adults = reservation.ReservationRooms.Where(x => x.IsCurrent).Sum(x => x.Adults);
            reservation.Children = reservation.ReservationRooms.Where(x => x.IsCurrent).Sum(x => x.Children);
            if (reservation.Folio is not null)
            {
                reservation.Folio.Total = reservation.TotalAmount;
                 ReconcileFolio(reservation.Folio);
            }
        }
        else 
        { 
            reservation.Adults = request.Adults ?? reservation.Adults;
            reservation.Children = request.Children ??   reservation.Children;
        }
        reservation.UpdatedAt = DateTimeOffset.UtcNow; reservation.UpdatedBy = actor.Id;
        try
        { 
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException) 
        {
            return Fail<ReservationResponse>("The reservation was modified by another request.", "CONCURRENCY_CONFLICT", 409); 
        }
        var updated = await _repository.GetByIdAsync(
            propertyId,
            reservation.Id,
            false,
            ct);

        return Ok(Map(updated ?? reservation));
    }

    public Task<ResponseStatus<ReservationActionResponse>>
        ConfirmAsync(
            User actor,
            int propertyId,
            int id,
            ConfirmReservationRequest request,
            CancellationToken ct = default)
    {
        return _repository.ExecuteSerializableAsync(
            () => TransitionAsync(
                actor,
                propertyId,
                id,
                ReservationStatus.Confirmed,
                request.Reason ?? "Reservation confirmed",
                request.ExpectedRowVersion,
                false,
                false,
                ct),
            ct);
    }

    public Task<ResponseStatus<ReservationActionResponse>>
       CancelAsync(
           User actor,
           int propertyId,
           int id,
           CancelReservationRequest request,
           CancellationToken ct = default)
       =>
       _repository.ExecuteSerializableAsync(
           () => TransitionAsync(
               actor,
               propertyId,
               id,
               ReservationStatus.Cancelled,
               request.Reason,
               request.ExpectedRowVersion,
               request.OverridePolicy,
               false,
               ct),
           ct);


    public Task<ResponseStatus<ReservationActionResponse>>
     NoShowAsync(
         User actor,
         int propertyId,
         int id,
         NoShowRequest request,
         CancellationToken ct = default)
     =>
     _repository.ExecuteSerializableAsync(
         () => TransitionAsync(
             actor,
             propertyId,
             id,
             ReservationStatus.NoShow,
             request.Reason ?? "No-show recorded",
             request.ExpectedRowVersion,
             request.OverridePolicy,
             true,
             ct),
         ct);
    public Task<ResponseStatus<ReservationActionResponse>>
    AssignRoomAsync(
        User actor,
        int propertyId,
        int id,
        AssignRoomRequest request,
        CancellationToken ct = default)
    =>
    _repository.ExecuteSerializableAsync(
        () => AssignRoomCoreAsync(
            actor,
            propertyId,
            id,
            request,
            ct),
        ct);


    public async Task<ResponseStatus<ReservationActionResponse>>
        AssignRoomCoreAsync(User actor,
        int propertyId,
        int id,
        AssignRoomRequest request,
        CancellationToken ct = default)
    {
        //actor must be manager or admin or receptionist
        //must be manager or receptionist in scope property
        var auth = await AuthorizeAsync(actor, propertyId, ct);
        {
            if (!auth.Success) return Fail<ReservationActionResponse>(auth.Message, auth.StatusCode);
        }
        var reservation = await _repository.GetByIdAsync(propertyId, id, true, ct);
        if (reservation is null)
        {
            return Fail<ReservationActionResponse>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404);
        }
        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.NoShow or ReservationStatus.CheckedOut)
        {
            return Fail<ReservationActionResponse>("Room cannot be assigned in the current state.", "INVALID_STATE_TRANSITION", 409);
        }
        if (request.Rooms.Count<=0)
        {
            return Fail<ReservationActionResponse>("must be fount one room at least.", "must be fount one room at least", 409);

        }
        var items = request.Rooms;
 
        var oldReservationTotal = reservation.TotalAmount;

        var now = DateTimeOffset.UtcNow;
        foreach (var item in items)
        {
            if (reservation.ReservationRooms.Any(x => x.IsCurrent && x.RoomId == item.RoomId))
            {
                return Fail<ReservationActionResponse>("Room is already assigned to this reservation.", "ROOM_ALREADY_ASSIGNED", 409);
            }
            var room = await _repository.GetRoomAsync(propertyId, item.RoomId, false, ct);
            if (room is null)
            {
                return Fail<ReservationActionResponse>("Room was not found.", "ROOM_NOT_FOUND", 404);
            }
            var available = await _repository.GetAvailableRoomsAsync(
                propertyId,
                reservation.CheckInDate,
                reservation.CheckOutDate,
                item.Adults ?? reservation.Adults,
                item.Children ?? reservation.Children,
                room.RoomTypeId,
                id,
                ct);
            if (!available.Any(x => x.Id == room.Id))
            {
                return Fail<ReservationActionResponse>("Room is no longer available.", "ROOM_NO_LONGER_AVAILABLE", 409);
            }
            var plan =await  _repository.GetRatePlanAsync(propertyId,room.RoomTypeId,item.RatePlanId,reservation.CheckInDate,ct);
            if (plan is null)
            {
                return Fail<ReservationActionResponse>("rateplane was not found.", "RATEPLANE_NOT_FOUND", 404);
            }
            
            var version = plan?.Versions.
                Where(x => x.IsActive && x.ValidFrom.Date <= reservation.CheckInDate.Date &&
                (x.ValidTo == null || x.ValidTo.Value.Date > reservation.CheckInDate.Date)).
                OrderByDescending(x => x.VersionNumber).
                FirstOrDefault();

            if (plan is null || version is null)
            {
                return Fail<ReservationActionResponse>("No valid rate plan exists.", "RATE_PLAN_NOT_FOUND", 422);
            }
            var nights = (reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days;
            var rate = version.Price > 0 ? version.Price : room.PricePerNight;
            var amount = Money(rate * nights);
            reservation.ReservationRooms.Add(
                new ReservationRoom 
            {
                    RoomId = room.Id, 
                    Adults = item.Adults ?? reservation.Adults,
                    Children = item.Children ?? reservation.Children,
                    CheckInDate = reservation.CheckInDate,
                    CheckOutDate = reservation.CheckOutDate, 
                    Nights = nights,
                    RatePlanId = plan.Id,
                    RatePlanVersionId = version.Id,
                    NightlyRate = rate,
                    BaseAmount = amount,
                    TotalAmount = amount,
                    IsCurrent = true,
                    AssignedAt = now,
                    AssignmentReason = request.Reason,
                    CreatedAt = now,
                    CreatedBy = actor.Id,
                    RatePlanSnapshot = new ReservationRoomRatePlanSnapshot 
                    {
                        RatePlanId = plan.Id, 
                        RatePlanVersionId = version.Id,
                        RatePlanName = plan.Name,
                        RatePlanType = plan.Type, IsRefundable = plan.IsRefundable, 
                        RulesSnapshot = version.Rules ?? plan.Rules,
                        NightlyRate = rate, 
                        BaseAmount = amount,
                        TotalAmount = amount,
                        CapturedAt = now, 
                        CreatedAt = now, 
                        CreatedBy = actor.Id 
                    }
                });
        }
        var taxes = await _repository.GetActiveTaxesAsync(propertyId, now, ct);
        
        RecalculateTotals(reservation, taxes); 
        
        reservation.Adults = reservation.ReservationRooms
            .Where(x => x.IsCurrent).
            Sum(x => x.Adults);

        reservation.Children = reservation.ReservationRooms.
            Where(x => x.IsCurrent).
            Sum(x => x.Children);
        
        reservation.UpdatedAt = now;
        reservation.UpdatedBy = actor.Id;
 
        var totalDelta = Money(
            reservation.TotalAmount - oldReservationTotal);

        if (reservation.Folio is not null)
        {
            reservation.Folio.Total = Money(
                reservation.Folio.Total + totalDelta);

            reservation.Folio.Balance = Money(
                reservation.Folio.Balance + totalDelta);
        }
        try { await _repository.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Fail<ReservationActionResponse>("The reservation was modified by another request.", "CONCURRENCY_CONFLICT", 409); }
        return Ok(Action(reservation));
    }

    public async Task<ResponseStatus<ReservationActionResponse>> 
        CheckInAsync(User actor, 
        int propertyId,
        int id,
        CheckInRequest request,
        CancellationToken ct = default)
    { 
        //actor must be manager or admin or receptionist
        //must be manager or receptionist in scope property
        var auth = await AuthorizeAsync(actor, propertyId, ct);
        {
            if (!auth.Success) return Fail<ReservationActionResponse>(auth.Message, auth.StatusCode);
        }
        if (!request.IdentityVerified)
        {
            return Fail<ReservationActionResponse>("Identity verification is required.", "IDENTITY_VERIFICATION_REQUIRED", 422);
        }
        var reservation = await _repository.GetByIdAsync(propertyId, id, true, ct);
        if (reservation is null)
        {
            return Fail<ReservationActionResponse>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404);
        }
        if (reservation.Status != ReservationStatus.Confirmed)
        {
            return Fail<ReservationActionResponse>("Only confirmed reservations can check in.", "INVALID_STATE_TRANSITION", 409);
        }
        // Check-in is a reservation-level operation: a reservation can no longer
        // be checked in partially. Ignore the legacy RoomId/RoomIds fields and
        // always process every current room assignment in the reservation.
        var assignments = reservation.ReservationRooms
            .Where(x => x.IsCurrent)
            .ToList();
        if (assignments.Count == 0)
        {
            return Fail<ReservationActionResponse>(
                "The reservation must have at least one current room assignment.",
                "ROOM_ASSIGNMENT_REQUIRED",
                422);
        }
        var rooms = new List<Room>();
        foreach (var assignment in assignments)
        {
            var room = await _repository.GetRoomAsync(propertyId, assignment.RoomId, true, ct);
            if (room is null)
            {
                return Fail<ReservationActionResponse>("Room was not found.", "ROOM_NOT_FOUND", 404);
            }
            if (room.Status is not (RoomStatus.Available or RoomStatus.Inspected))
            {
                return Fail<ReservationActionResponse>("One or more rooms are not ready for check-in.", "ROOM_NOT_READY", 409);
            }
            room.Status = RoomStatus.Occupied; rooms.Add(room);
        }
        reservation.Status = ReservationStatus.CheckedIn;
        reservation.UpdatedAt = DateTimeOffset.UtcNow;
        reservation.UpdatedBy = actor.Id;
        AddHistory(
            reservation,
            ReservationStatus.Confirmed,
            ReservationStatus.CheckedIn, 
            request.RegistrationNotes, 
            actor.Id,
            DateTimeOffset.UtcNow);
        await _repository.PersistLifecycleAsync(reservation, rooms, null, ct);
        return Ok(Action(reservation));
    }

    public Task<ResponseStatus<ReservationActionResponse>> 
        ReleaseRoomAsync(User actor,
        int propertyId, 
        int id,
        int reservationRoomId, 
        CancellationToken ct = default) =>
        _repository.ExecuteSerializableAsync(
            () => ReleaseRoomCoreAsync(actor, propertyId, id, reservationRoomId, ct), ct);

    private async Task<ResponseStatus<ReservationActionResponse>>
        ReleaseRoomCoreAsync(User actor,
        int propertyId,
        int id,
        int reservationRoomId,
        CancellationToken ct = default)
    {
        var auth = await AuthorizeAsync(actor, propertyId, ct);
        if (!auth.Success)
        {
            return Fail<ReservationActionResponse>(auth.Message, "FORBIDDEN", auth.StatusCode);
        }
        var reservation = await _repository.GetByIdAsync(propertyId, id, true, ct);
        if (reservation is null)
        {
            return Fail<ReservationActionResponse>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404);
        }
        if (reservation.Status is ReservationStatus.CheckedIn
            or ReservationStatus.CheckedOut
            or ReservationStatus.Cancelled
            or ReservationStatus.NoShow)
        {
            return Fail<ReservationActionResponse>("Room assignment cannot be released in the current state.", "INVALID_STATE_TRANSITION", 409);
        }
        var assignment = reservation.ReservationRooms.FirstOrDefault(x => x.Id == reservationRoomId && x.IsCurrent);
        if (assignment is null)
        {
            return Fail<ReservationActionResponse>("Current room assignment was not found.", "ROOM_ASSIGNMENT_NOT_FOUND", 404);
        }
        if (reservation.ReservationRooms.Count(x => x.IsCurrent) <= 1)
        {
            return Fail<ReservationActionResponse>("At least one current room assignment is required.", "ROOM_REQUIRED");
        }
        assignment.IsCurrent = false;
        assignment.ReleasedAt = DateTimeOffset.UtcNow;
        assignment.AssignmentReason = "Released";
        var taxes = await _repository.GetActiveTaxesAsync(propertyId, DateTimeOffset.UtcNow, ct);
        RecalculateTotals(reservation, taxes);
        reservation.Adults = reservation.ReservationRooms.Where(x => x.IsCurrent).Sum(x => x.Adults);
        reservation.Children = reservation.ReservationRooms.Where(x => x.IsCurrent).Sum(x => x.Children);
        if (reservation.Folio is not null)
        {
            reservation.Folio.Total = reservation.TotalAmount;
            ReconcileFolio(reservation.Folio);
        }
        reservation.UpdatedAt = DateTimeOffset.UtcNow;
        reservation.UpdatedBy = actor.Id;
        await _repository.SaveChangesAsync(ct);
        return Ok(Action(reservation));
    }

    public async Task<ResponseStatus<ReservationActionResponse>> 
        CheckOutAsync(User actor,
        int propertyId,
        int id,
        CheckOutRequest request,
        CancellationToken ct = default)
    {
        var auth = await AuthorizeAsync(actor, propertyId, ct);
        if (!auth.Success)
        {
            return Fail<ReservationActionResponse>(auth.Message, "FORBIDDEN", auth.StatusCode);
        }
        var reservation = await _repository.GetByIdAsync(propertyId, id, true, ct);
        if (reservation is null)
        {
            return Fail<ReservationActionResponse>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404);
        }
       //must be reservation is checkin
        if (reservation.Status != ReservationStatus.CheckedIn)
        {
            return Fail<ReservationActionResponse>("Only checked-in reservations can check out.", "INVALID_STATE_TRANSITION", 409);
        }
        if (reservation.Folio is not null)
        {
            ReconcileFolio(reservation.Folio);
            var roles = await user.GetRolesAsync(actor.Id, ct);
            var canOverride = roles.Any(x => x.IsActive &&
                (string.Equals(x.Name, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(x.Name, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)));
            if (request.ManagerOverride && !canOverride)
            {
                return Fail<ReservationActionResponse>(
                    "Only a manager or admin can override the outstanding balance.",
                    "POLICY_OVERRIDE_NOT_ALLOWED",
                    403);
            }
            if (reservation.Folio.Payments.Any(x => x.Status == PaymentStatus.Pending))
            {
                return Fail<ReservationActionResponse>("A payment is still pending.", "PENDING_PAYMENT", 409);
            }
            if (reservation.Folio.Balance > 0m && !request.ManagerOverride)
            {
                return Fail<ReservationActionResponse>("The folio balance must be settled before check-out.", "BALANCE_NOT_SETTLED", 409);
            }
        }
        var rooms = new List<Room>();
        foreach (var assignment in reservation.ReservationRooms.Where(x => x.IsCurrent))
        { 
            var room = await _repository.GetRoomAsync(propertyId, assignment.RoomId, true, ct);
            if (room is not null) 
            { 
                room.Status = RoomStatus.Dirty;
                rooms.Add(room); 
                assignment.IsCurrent = false;
                assignment.ReleasedAt = DateTimeOffset.UtcNow; 
            }
        }
        reservation.Status = ReservationStatus.CheckedOut; 
        reservation.UpdatedAt = DateTimeOffset.UtcNow;
        reservation.UpdatedBy = actor.Id;
        if (reservation.Folio is not null)
        {
            reservation.Folio.Status = FolioStatus.Closed;
            reservation.Folio.ClosedAt = DateTimeOffset.UtcNow;
            reservation.Folio.ClosedBy = actor.Id;
        }
        AddHistory(
            reservation,
            ReservationStatus.CheckedIn,
            ReservationStatus.CheckedOut, 
            request.Notes,
            actor.Id,
            DateTimeOffset.UtcNow
            );
        var housekeepingTasks = rooms.Select(room => new HousekeepingTask 
        { 
            RoomId = room.Id,
            Status = HousekeepingTaskStatus.Pending,
            Notes = request.Notes,
            CreatedAt = DateTimeOffset.UtcNow
            , CreatedBy = actor.Id
        }).ToList();

        await _repository.PersistLifecycleAsync(reservation, rooms, housekeepingTasks, ct); 
        return Ok(Action(reservation));
    }

    public async Task<ResponseStatus<IReadOnlyList<ReservationStatusHistoryResponse>>> GetStatusHistoryAsync(User actor, int propertyId, int id, CancellationToken ct = default)
    {
        var reservation = await _repository.GetByIdAsync(propertyId, id, false, ct); if (reservation is null) return Fail<IReadOnlyList<ReservationStatusHistoryResponse>>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404);
        return Ok<IReadOnlyList<ReservationStatusHistoryResponse>>(reservation.StatusHistory.OrderBy(x => x.ChangedAt).Select(x => new ReservationStatusHistoryResponse { Id = x.Id, FromStatus = x.FromStatus, ToStatus = x.ToStatus, Reason = x.Reason, ActorId = x.ActorId, ChangedAt = x.ChangedAt }).ToList());
    }
    //-----------------------------------------------------------------------------------
    //private methods
    //------------------------------------------------------------------------------------

    private async Task<ResponseStatus<ReservationActionResponse>> TransitionAsync(
        User actor,
        int propertyId,
        int id,
        ReservationStatus target,
        string reason,
        byte[]? expectedRowVersion,
        bool overridePolicy,
        bool isNoShow,
        CancellationToken ct)
    {
        var authorization = await AuthorizeAsync(actor, propertyId, ct);
        if (!authorization.Success) return Fail<ReservationActionResponse>(authorization.Message, "FORBIDDEN", authorization.StatusCode);

        var reservation = await _repository.GetByIdAsync(propertyId, id, true, ct);
        if (reservation is null) return Fail<ReservationActionResponse>("Reservation was not found.", "RESERVATION_NOT_FOUND", 404);
        var property = await _repository.GetPropertyAsync(propertyId, ct);
        if (property is null) return Fail<ReservationActionResponse>("Property was not found.", "PROPERTY_NOT_FOUND", 404);
        if (property.Status != PropertyStatus.Active) return Fail<ReservationActionResponse>("Property is inactive.", "PROPERTY_INACTIVE", 409);
        if (reservation.Guest is null || !reservation.Guest.IsActive) return Fail<ReservationActionResponse>("Guest was not found or is inactive.", "GUEST_NOT_FOUND", 404);

        if (expectedRowVersion is not null && !expectedRowVersion.SequenceEqual(reservation.RowVersion))
            return Fail<ReservationActionResponse>("The reservation was modified by another request.", "CONCURRENCY_CONFLICT", 409);

        var valid = target switch
        {
            ReservationStatus.Confirmed => reservation.Status == ReservationStatus.Pending,
            ReservationStatus.Cancelled => reservation.Status is ReservationStatus.Pending or ReservationStatus.Confirmed,
            ReservationStatus.NoShow => reservation.Status is ReservationStatus.Pending or ReservationStatus.Confirmed,
            _ => false
        };
        if (!valid) return Fail<ReservationActionResponse>("The requested status transition is not allowed.", "INVALID_STATE_TRANSITION", 409);

        var now = DateTimeOffset.UtcNow;
        var localNow = ToPropertyTime(property, now);
        var roles = await user.GetRolesAsync(actor.Id, ct);
        var isAdmin = roles.Any(x => x.IsActive && string.Equals(x.Name, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase));
        var isManager = roles.Any(x => x.IsActive && string.Equals(x.Name, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase));
        var isReceptionist = roles.Any(x => x.IsActive && string.Equals(x.Name, nameof(UserRole.Receptionist), StringComparison.OrdinalIgnoreCase));
        if (overridePolicy && !isManager && !isAdmin)
            return Fail<ReservationActionResponse>("Only a manager or admin can override the policy.", "POLICY_OVERRIDE_NOT_ALLOWED", 403);

        decimal cancellationFee = 0m,
            refundableAmount = 0m, 
            heldAmount = 0m, 
            paidAmount = 0m,
            outstandingCancellationFee = 0m,
            noShowCharge = 0m;
        if (target == ReservationStatus.Confirmed)
        {
            if (reservation.CancellationPolicyId.HasValue && reservation.CancellationPolicySnapshot is null)
                return Fail<ReservationActionResponse>("Cancellation policy snapshot is required.", "POLICY_SNAPSHOT_REQUIRED", 409);
            var requiredDeposit = reservation.DepositPolicySnapshot?.RequiredDepositAmount ?? 0m;
            var paidDeposit = reservation.Folio?.Payments.Where(x => x.Status is PaymentStatus.Paid or PaymentStatus.PartiallyPaid).Sum(x => x.Amount) ?? 0m;
            if (property.Settings?.RequireDepositForReservation == true && requiredDeposit > paidDeposit)
                return Fail<ReservationActionResponse>("The required deposit has not been paid.", "DEPOSIT_REQUIRED", 409);

            foreach (var assignment in reservation.ReservationRooms.Where(x => x.IsCurrent))
            {
                var available = await _repository.GetAvailableRoomsAsync(propertyId, reservation.CheckInDate, reservation.CheckOutDate, assignment.Adults, assignment.Children, assignment.Room?.RoomTypeId, id, ct);
                if (!available.Any(x => x.Id == assignment.RoomId))
                    return Fail<ReservationActionResponse>($"Room {assignment.RoomId} is no longer available.", "ROOM_NO_LONGER_AVAILABLE", 409);
            }
        }
        else if (target == ReservationStatus.Cancelled)
        {
            if (isReceptionist && !isManager && !isAdmin && !overridePolicy && reservation.CancellationPolicySnapshot is not null)
            {
                var cutoff = reservation.CheckInDate.Date.AddHours(-reservation.CancellationPolicySnapshot.CutoffHours);
                if (localNow.DateTime > cutoff) 
                    return Fail<ReservationActionResponse>("Cancellation is past the allowed cutoff.", "CANCELLATION_CUTOFF_EXCEEDED", 403);
            }
            var payments = reservation.Folio?.Payments ?? Array.Empty<Payment>();
            var grossPaidAmount = payments
                .Where(x => x.Status is PaymentStatus.Paid or PaymentStatus.PartiallyPaid)
                .Sum(x => x.Amount);
            var refundedAmount = payments
                .SelectMany(x => x.Refunds)
                .Where(x => x.Status == RefundStatus.Completed)
                .Sum(x => x.Amount);
            paidAmount = Money(Math.Max(0m, grossPaidAmount - refundedAmount));

            if (reservation.CancellationPolicySnapshot is not null)
            {
                var snapshot = reservation.CancellationPolicySnapshot;
                var hoursUntilCheckIn = (reservation.CheckInDate.Date - localNow.DateTime).TotalHours;
                var withinFreeWindow = hoursUntilCheckIn >= snapshot.FreeCancellationHours;
                cancellationFee = snapshot.IsNonRefundable || !withinFreeWindow
                    ? Money(reservation.TotalAmount * snapshot.CancellationFeePercentage / 100m + snapshot.FixedCancellationFee)
                    : 0m;
                cancellationFee = Math.Min(cancellationFee, reservation.TotalAmount);
            }
            refundableAmount = Money(Math.Max(0m, paidAmount - cancellationFee));
            outstandingCancellationFee = Money(Math.Max(0m, cancellationFee - paidAmount));
            heldAmount = Money(Math.Min(paidAmount, cancellationFee));
            if (cancellationFee > 0m && reservation.Folio is not null)
            {
                reservation.Folio.Items.Add(new FolioItem
                {
                    Type = FolioItemType.Adjustment,
                    Description = "Cancellation fee",
                    Amount = cancellationFee,
                    Source = FolioItemSource.System,
                    Status = FolioItemStatus.Posted,
                    PostedAt = now,
                    CreatedAt = now,
                    CreatedBy = actor.Id
                });
                reservation.Folio.Total += cancellationFee;
             }
            if (reservation.Folio is not null) ReconcileFolio(reservation.Folio);

        }
        else if (isNoShow)
        {
            var cutoff = property.Settings?.HotelDayClosingTime ?? property.Settings?.CheckOutTime ?? new TimeSpan(12, 0, 0);
            var isPastNoShowTime = localNow.DateTime >= reservation.CheckInDate.Date.Add(cutoff);
            if (isReceptionist && !isManager && !isAdmin && !overridePolicy && !isPastNoShowTime)
            {
                return Fail<ReservationActionResponse>(
                    "No-show can only be recorded after the hotel cutoff time.", 
                    "NO_SHOW_CUTOFF_NOT_REACHED",
                    403);
            }
            noShowCharge = reservation.ReservationRooms.Where(x => x.IsCurrent).Select(x => x.NightlyRate).FirstOrDefault();
            if (noShowCharge > 0m && reservation.Folio is not null)
            {
                reservation.Folio.Items.Add(new FolioItem { Type = FolioItemType.Adjustment,
                    Description = "No-show charge",
                    Amount = noShowCharge, 
                    Source = FolioItemSource.System,
                    Status = FolioItemStatus.Posted,
                    PostedAt = now, CreatedAt = now,
                    CreatedBy = actor.Id
                });
                reservation.Folio.Total += noShowCharge;
             }
            if (reservation.Folio is not null) ReconcileFolio(reservation.Folio);

        }

        var from = reservation.Status;
        reservation.Status = target;
        reservation.UpdatedAt = now;
        reservation.UpdatedBy = actor.Id;
        AddHistory(reservation, from, target, reason, actor.Id, now);
        var audit = new AuditLog
        {
            UserId = actor.Id,
            PropertyId = propertyId,
            EntityName = nameof(Reservation),
            EntityId = id.ToString(),
            Action = target.ToString(),
            Reason = reason,
            OldValues = JsonSerializer.Serialize(new { Status = from.ToString() }),
            NewValues = JsonSerializer.Serialize(new { Status = target.ToString(), cancellationFee, paidAmount, refundableAmount, outstandingCancellationFee, noShowCharge }),
            CreatedAt = now,
            CreatedBy = actor.Id
        };
        try 
        {
            await _repository.PersistTransitionAsync(reservation, audit, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            {
                return Fail<ReservationActionResponse>(
                    "The reservation was modified by another request.",
                    "CONCURRENCY_CONFLICT", 409);
            }
        }
        return Ok(Action(
            reservation,
            cancellationFee,
            refundableAmount,
            heldAmount,
            noShowCharge,
            reason,
            paidAmount,
            outstandingCancellationFee));
    }

    private static DateTimeOffset 
        ToPropertyTime(Property property,
        DateTimeOffset utcNow)
    {
        var timeZoneId = property.Settings?.TimeZone;
        if (string.IsNullOrWhiteSpace(timeZoneId)) return utcNow;
        try { return TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)); }
        catch (TimeZoneNotFoundException) { return utcNow; }
        catch (InvalidTimeZoneException) { return utcNow; }
    }

    private async Task<ReservationRoom?> BuildRoomLineAsync(
     decimal discount,
     decimal feeAmount,
     int propertyId,
     Room room,
     int? ratePlanId,
     int adults,
     int children,
     DateTime checkIn,
     DateTime checkOut,
     IReadOnlyList<Tax> taxes,
     string actorId,
     DateTimeOffset now,
     CancellationToken ct)
    {
        if (room.RoomType is null)
        {
            return null;
        }

        var plan = await _repository.GetRatePlanAsync(
            propertyId: propertyId,

          
            roomTypeId: room.RoomTypeId,

            ratePlanId: ratePlanId,
            stayDate: checkIn,
            ct: ct);

        if (plan is null)
        {
            return null;
        }

        var version = plan.Versions
            .Where(x =>
                x.IsActive &&
                x.ValidFrom.Date <= checkIn.Date &&
                (
                    x.ValidTo == null ||
                    x.ValidTo.Value.Date > checkIn.Date
                ))
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefault();

        if (version is null)
        {
            return null;
        }

        var nights =
            (checkOut.Date - checkIn.Date).Days;

        if (nights <= 0)
        {
            return null;
        }

        var rate =
            version.Price > 0m
                ? version.Price
                : room.PricePerNight > 0m
                    ? room.PricePerNight
                    : room.RoomType.BasePrice;

        if (rate < 0m)
        {
            return null;
        }

        var baseAmount = Money(rate * nights);

        var discountAmount = Money(
            Math.Max(0m, discount));

        var calculatedFeeAmount = Money(
            Math.Max(0m, feeAmount));

        var lineTotal = Money(
            baseAmount -
            discountAmount +
            calculatedFeeAmount);

        return new ReservationRoom
        {
            RoomId = room.Id,
            Adults = adults,
            Children = children,
            CheckInDate = checkIn.Date,
            CheckOutDate = checkOut.Date,
            Nights = nights,

            RatePlanId = plan.Id,
            RatePlanVersionId = version.Id,

            NightlyRate = rate,
            BaseAmount = baseAmount,
            DiscountAmount = discountAmount,
            FeeAmount = calculatedFeeAmount,

             TotalAmount = lineTotal,

            IsCurrent = true,
            AssignedAt = now,
            AssignmentReason = "Reservation room assignment",
            CreatedAt = now,
            CreatedBy = actorId,

            RatePlanSnapshot = new ReservationRoomRatePlanSnapshot
            {
                RatePlanId = plan.Id,
                RatePlanVersionId = version.Id,
                RatePlanName = plan.Name,
                RatePlanType = plan.Type,
                IsRefundable = version.IsRefundable,

                RulesSnapshot =
                    string.IsNullOrWhiteSpace(version.Rules)
                        ? plan.Rules
                        : version.Rules,

                NightlyRate = rate,
                BaseAmount = baseAmount,
                DiscountAmount = discountAmount,
                FeeAmount = calculatedFeeAmount,
                TotalAmount = lineTotal,

                CapturedAt = now,
                CreatedAt = now,
                CreatedBy = actorId
            }
        };
    }


    private static void RecalculateTotals(
        Reservation reservation,
        IReadOnlyList<Tax> taxes)
    {
        var currentLines = reservation.ReservationRooms
            .Where(x => x.IsCurrent)
            .ToList();

        reservation.BaseAmount = Money(
            currentLines.Sum(x => x.BaseAmount));

        reservation.TotalDiscountAmount = Money(
            currentLines.Sum(x => x.DiscountAmount));

        reservation.TotalFeeAmount = Money(
            currentLines.Sum(x => x.FeeAmount));

        var taxableAmount = Money(
            Math.Max(
                0m,
                reservation.BaseAmount -
                reservation.TotalDiscountAmount));

         reservation.TotalTaxAmount = Money(
            taxes.Sum(t =>
                t.Type == TaxType.Percentage
                    ? taxableAmount * t.Rate / 100m
                    : t.Rate));

         var exclusiveTaxAmount = Money(
            taxes
                .Where(t => !t.IsInclusive)
                .Sum(t =>
                    t.Type == TaxType.Percentage
                        ? taxableAmount * t.Rate / 100m
                        : t.Rate));

         reservation.TotalAmount = Money(
            taxableAmount +
            reservation.TotalFeeAmount +
            exclusiveTaxAmount);
    }


    private async Task AddPolicySnapshotsAsync(
     Reservation reservation,
     CreateReservationRequest request,
     DateTime checkIn,
     DateTimeOffset now,
     CancellationToken ct)
    {
        if (request.CancellationPolicyId.HasValue)
        {
            var cancellationVersion =
                await _repository.GetCancellationPolicyVersionAsync(
                    propertyId: request.PropertyId,
                    policyId: request.CancellationPolicyId.Value,
                    versionId: request.CancellationPolicyVersionId,
                    stayDate: new DateTimeOffset(
                        checkIn.Date,
                        TimeSpan.Zero),
                    ct: ct);

            if (cancellationVersion is not null)
            {
                reservation.CancellationPolicySnapshot =
                    new ReservationCancellationPolicySnapshot
                    {
                        CancellationPolicyId =
                            cancellationVersion.CancellationPolicyId,

                        CancellationPolicyVersionId =
                            cancellationVersion.Id,

                        PolicyName =
                            cancellationVersion.CancellationPolicy?.Name
                            ?? "Cancellation policy",

                        Rules = string.IsNullOrWhiteSpace(
                            cancellationVersion.Rules)
                                ? "{}"
                                : cancellationVersion.Rules,

                        FreeCancellationHours =
                            cancellationVersion.FreeCancellationHours,

                        CancellationFeePercentage =
                            cancellationVersion.CancellationFeePercentage,

                        FixedCancellationFee =
                            cancellationVersion.FixedCancellationFee,

                        IsNonRefundable =
                            cancellationVersion.IsNonRefundable,

                        CutoffHours =
                            cancellationVersion.CutoffHours,

                        CapturedAt = now,
                        CreatedAt = now,
                        CreatedBy = reservation.CreatedBy
                    };
            }
        }

        if (request.DepositPolicyId.HasValue)
        {
            var depositVersion =
                await _repository.GetDepositPolicyVersionAsync(
                    propertyId: request.PropertyId,
                    policyId: request.DepositPolicyId.Value,
                    versionId: request.DepositPolicyVersionId,
                    stayDate: new DateTimeOffset(
                        checkIn.Date,
                        TimeSpan.Zero),
                    ct: ct);

            if (depositVersion is not null)
            {
                var requiredDepositAmount =
                    depositVersion.Type == DepositType.Percentage
                        ? Money(
                            reservation.TotalAmount *
                            depositVersion.Percentage /
                            100m)
                        : Money(depositVersion.Amount);

                reservation.DepositPolicySnapshot =
                    new ReservationDepositPolicySnapshot
                    {
                        DepositPolicyId =
                            depositVersion.DepositId,

                        DepositPolicyVersionId =
                            depositVersion.Id,

                        PolicyName =
                            depositVersion.Name,

                        Type =
                            depositVersion.Type,

                        Amount =
                            Money(depositVersion.Amount),

                        Percentage =
                            Money(depositVersion.Percentage),

                        RequiredDepositAmount =
                            requiredDepositAmount,

                        CapturedAt = now,
                        CreatedAt = now,
                        CreatedBy = reservation.CreatedBy
                    };
            }
        }
    }


    private static void
        AddHistory(Reservation reservation,
        ReservationStatus? from,
        ReservationStatus to,
        string? reason,
        string actorId,
        DateTimeOffset now)
        =>
        reservation.StatusHistory.Add(
            new ReservationStatusHistory
            {
                FromStatus = from,
                ToStatus = to,
                Reason = reason,
                ActorId = actorId,
                ChangedAt = now,
                CreatedAt = now,
                CreatedBy = actorId
            });

    private async Task<ResponseStatus<bool>>
        AuthorizeAsync
        (User actor,
        int propertyId,
        CancellationToken ct)
    {
        if (actor is null)
        {
            return Fail<bool>(message: "User is not authenticated.", status: 401);
        }
        //must be admin and role is active
        var roles = await user.GetRolesAsync(actor.Id, ct);
        if (roles.Any(x => x.IsActive &&
        (
                  string.Equals(x.Name, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)))
        )
        {
            return Ok(true);
        }
        //must be receptioist or manager belong be property
        if
        (
            !roles.Any(x => x.IsActive &&
            (
               string.Equals(x.Name, nameof(UserRole.Receptionist), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(x.Name, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase))
            )
        )
        {
            return Fail<bool>("Admin or Manager or receptionsit access required.", 403);
        }
        if (!actor.PropertyId.HasValue || actor.PropertyId.Value != propertyId)
        {
            return Fail<bool>("You are not authorized to access this property.", 403);
        }
        return Ok(true);
    }
    private static bool
        HasScope(User actor, int propertyId) =>
        actor is not null && actor.PropertyId.HasValue && actor.PropertyId.Value == propertyId;

    private static string?
        ValidateDates(
        DateTime checkIn,
        DateTime checkOut) =>
        checkOut.Date <= checkIn.Date ? "Check-out must be after check-in." : null;
    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static ReservationActionResponse Action(
      Reservation x,
      decimal cancellationFee = 0m,
      decimal refundableAmount = 0m,
      decimal heldAmount = 0m,
      decimal noShowCharge = 0m,
      string? reason = null,
      decimal paidAmount = 0m,
      decimal outstandingCancellationFee = 0m)
    {
        return new ReservationActionResponse
        {
            Id = x.Id,
            Status = x.Status,

            RowVersion = Convert.ToBase64String(
                x.RowVersion ?? Array.Empty<byte>()),

            RoomIds = x.ReservationRooms
                .Where(r => r.IsCurrent)
                .Select(r => r.RoomId)
                .ToList(),

            Balance = x.Folio?.Balance ?? 0m,
            CreditBalance = x.Folio?.CreditBalance ?? 0m,

            CancellationFee = cancellationFee,
            RefundableAmount = refundableAmount,
            HeldAmount = heldAmount,
            PaidAmount = paidAmount,
            OutstandingCancellationFee = outstandingCancellationFee,
            NoShowCharge = noShowCharge,
            Reason = reason
        };
    }

    private static void ReconcileFolio(Folio folio)
    {
        var grossPaid = folio.Payments
            .Where(x => x.Status is PaymentStatus.Paid or PaymentStatus.PartiallyPaid)
            .Sum(x => x.Amount);
        var refunded = folio.Payments
            .SelectMany(x => x.Refunds)
            .Where(x => x.Status == RefundStatus.Completed)
            .Sum(x => x.Amount);
        var netPaid = Money(Math.Max(0m, grossPaid - refunded));
        var difference = Money(folio.Total - netPaid);

        folio.Balance = Math.Max(0m, difference);
        folio.CreditBalance = Math.Max(0m, -difference);
    }
    private static ReservationSummaryResponse MapSummary(Reservation x) => new() { Id = x.Id, PropertyId = x.PropertyId, GuestId = x.GuestId, GuestName = $"{x.Guest?.FirstName} {x.Guest?.LastName}".Trim(), Status = x.Status, CheckInDate = x.CheckInDate, CheckOutDate = x.CheckOutDate, TotalAmount = x.TotalAmount, RoomId = x.ReservationRooms.FirstOrDefault(r => r.IsCurrent)?.RoomId, RoomIds = x.ReservationRooms.Where(r => r.IsCurrent).Select(r => r.RoomId).ToList() };
    private static ReservationResponse Map(Reservation x)
    {
        return new ReservationResponse
        {
            Id = x.Id,
            PropertyId = x.PropertyId,
            GuestId = x.GuestId,

            GuestName =
                $"{x.Guest?.FirstName} {x.Guest?.LastName}".Trim(),

            Status = x.Status,
            CheckInDate = x.CheckInDate,
            CheckOutDate = x.CheckOutDate,
            Adults = x.Adults,
            Children = x.Children,
            Source = x.Source,

            BaseAmount = x.BaseAmount,
            DiscountAmount = x.TotalDiscountAmount,
            TaxAmount = x.TotalTaxAmount,
            FeeAmount = x.TotalFeeAmount,
            TotalAmount = x.TotalAmount,

            RowVersion = Convert.ToBase64String(
                x.RowVersion ?? Array.Empty<byte>()),

            Rooms = x.ReservationRooms.Select(r =>
                new ReservationRoomResponse
                {
                    Id = r.Id,
                    RoomId = r.RoomId,
                    RoomNumber = r.Room?.RoomNumber ?? string.Empty,
                    RatePlanId = r.RatePlanId,
                    RatePlanVersionId = r.RatePlanVersionId,
                    Nights = r.Nights,
                    NightlyRate = r.NightlyRate,
                    BaseAmount = r.BaseAmount,
                    DiscountAmount = r.DiscountAmount,

                    TaxAmount = r.RatePlanSnapshot?.TaxAmount ?? 0m,

                    FeeAmount = r.FeeAmount,
                    TotalAmount = r.TotalAmount,
                    IsCurrent = r.IsCurrent
                }).ToList()
        };
    }


    private static ResponseStatus<T> Ok<T>(T data, string message = "", int status = 200) => new(data, message, status);
    private static ResponseStatus<T> Fail<T>(string message, int status = 400) => new(message: message, statusCode: status);
    private static ResponseStatus<T> Fail<T>(string message, string code, int status = 400) => new(message: message, statusCode: status, code: code);
}
