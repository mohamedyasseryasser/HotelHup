# GetReservationSnapshots Endpoint - ??????? ???????

## ??????? ??????: ????? Interfaces

### 1. IReservationRepository - ????? ??? method

**?????:** `HotelHup.APPLICATION\interfacesrepo\IReservationRepo.cs`

??? ??? ??? method ?? ????? ??? interface (??? ??? closing brace):

```csharp
Task<Reservation?> GetReservationWithSnapshotsAsync(
    int propertyId,
    int reservationId,
    CancellationToken ct = default);
```

---

### 2. IReservationService - ????? ??? method

**?????:** `HotelHup.APPLICATION\services\interfaces\IReservationService.cs`

??? ??? ??? method ?? ????? ??? interface (??? ??? closing brace):

```csharp
Task<ResponseStatus<ReservationSnapshotsResponse>> GetReservationSnapshotsAsync(
    User actor,
    int propertyId,
    int reservationId,
    CancellationToken ct = default);
```

---

## ??????? ???????: ??????? ?? Repository

### ReservationRepository - ????? ??? method

**?????:** `HotelHup.INFRASTRUCTURE\repos\ReservationRepo.cs`

??? ??? ??? method ?? ????? ??? class (??? ??? closing brace):

```csharp
public async Task<Reservation?> GetReservationWithSnapshotsAsync(
    int propertyId,
    int reservationId,
    CancellationToken ct = default)
{
    return await _context.Reservations
        .AsNoTracking()
        .Where(x => x.PropertyId == propertyId && x.Id == reservationId)
        .Include(x => x.CancellationPolicySnapshot)
        .Include(x => x.DepositPolicySnapshot)
        .Include(x => x.ReservationTaxSnapshots)
        .FirstOrDefaultAsync(ct);
}
```

---

## ??????? ???????: ??????? ?? Service

### ReservationService - ????? ??? method

**?????:** `HotelHup.APPLICATION\services\implementation\ReservationService.cs`

??? ??? ??? method ??????? ?? ????? ??? class (??? ??? closing brace):

```csharp
public async Task<ResponseStatus<ReservationSnapshotsResponse>> GetReservationSnapshotsAsync(
    User actor,
    int propertyId,
    int reservationId,
    CancellationToken ct = default)
{
    // ?????? ?? ???????
    var auth = await AuthorizeAsync(actor, propertyId, ct);
    if (!auth.Success)
    {
        return Fail<ReservationSnapshotsResponse>(
            auth.Message,
            "FORBIDDEN",
            auth.StatusCode);
    }

    // ?????? ?? ??????
    var property = await _repository.GetPropertyAsync(propertyId, ct);
    if (property is null)
    {
        return Fail<ReservationSnapshotsResponse>(
            "Property was not found.",
            "PROPERTY_NOT_FOUND",
            404);
    }

    if (property.Status != PropertyStatus.Active)
    {
        return Fail<ReservationSnapshotsResponse>(
            "Property is inactive.",
            "PROPERTY_INACTIVE",
            409);
    }

    // ?????? ??? ????? ?? ??? snapshots
    var reservation = await _repository.GetReservationWithSnapshotsAsync(
        propertyId,
        reservationId,
        ct);

    if (reservation is null)
    {
        return Fail<ReservationSnapshotsResponse>(
            "Reservation was not found.",
            "RESERVATION_NOT_FOUND",
            404);
    }

    // ???? Response
    var response = new ReservationSnapshotsResponse
    {
        ReservationId = reservation.Id,
        PropertyId = reservation.PropertyId,
        CancellationPolicy = reservation.CancellationPolicySnapshot is not null
            ? MapCancellationPolicySnapshot(reservation.CancellationPolicySnapshot)
            : null,
        DepositPolicy = reservation.DepositPolicySnapshot is not null
            ? MapDepositPolicySnapshot(reservation.DepositPolicySnapshot)
            : null,
        Taxes = reservation.ReservationTaxSnapshots
            .Select(MapTaxSnapshot)
            .ToList()
            .AsReadOnly()
    };

    return Ok(response);
}

private static CancellationPolicySnapshotResponse MapCancellationPolicySnapshot(
    ReservationCancellationPolicySnapshot snapshot)
{
    return new CancellationPolicySnapshotResponse
    {
        Id = snapshot.Id,
        PolicyName = snapshot.PolicyName,
        Rules = snapshot.Rules,
        FreeCancellationHours = snapshot.FreeCancellationHours,
        CancellationFeePercentage = snapshot.CancellationFeePercentage,
        FixedCancellationFee = snapshot.FixedCancellationFee,
        IsNonRefundable = snapshot.IsNonRefundable,
        CutoffHours = snapshot.CutoffHours,
        CapturedAt = snapshot.CapturedAt
    };
}

private static DepositPolicySnapshotResponse MapDepositPolicySnapshot(
    ReservationDepositPolicySnapshot snapshot)
{
    return new DepositPolicySnapshotResponse
    {
        Id = snapshot.Id,
        PolicyName = snapshot.PolicyName,
        Type = snapshot.Type,
        Amount = snapshot.Amount,
        Percentage = snapshot.Percentage,
        RequiredDepositAmount = snapshot.RequiredDepositAmount,
        CapturedAt = snapshot.CapturedAt
    };
}

private static TaxSnapshotResponse MapTaxSnapshot(ReservationTaxSnapshot snapshot)
{
    return new TaxSnapshotResponse
    {
        Id = snapshot.Id,
        TaxCode = snapshot.TaxCode,
        TaxName = snapshot.TaxName,
        Rate = snapshot.Rate,
        Type = snapshot.Type,
        IsInclusive = snapshot.IsInclusive,
        TaxableAmount = snapshot.TaxableAmount,
        TaxAmount = snapshot.TaxAmount,
        TaxValidFrom = snapshot.TaxValidFrom,
        TaxValidTo = snapshot.TaxValidTo
    };
}
```

---

## ??????? ???????: ??? Endpoint ?? Controller

### ReservationController - ????? ??? Endpoint

**?????:** `HotelHup\Controllers\ReservationController.cs`

??? ??? ??? endpoint ??? ??? endpoints ?????? (??? ??? closing brace ??? class):

```csharp
[HttpGet("reservations/{id:int}/snapshots")]
[Authorize(Policy = Permissions.Reservations.Read)]
public async Task<IActionResult> GetSnapshots(
    int id,
    [FromQuery] int propertyId,
    CancellationToken ct)
{
    var actor = await GetActor();
    if (!actor.Success || actor.Data is null)
    {
        return Result(actor);
    }

    return Result(
        await _service.GetReservationSnapshotsAsync(
            actor.Data,
            propertyId,
            id,
            ct));
}
```

---

## ??????? ???????: DTOs

### ReservationSnapshotsResponse - ?? ???? DTOs

**?????:** `HotelHup.APPLICATION\DTO\reservation\ReservationSnapshotsResponse.cs`

?? ????? ??? ????? ?????? ?? ???? ??? DTOs ????????:

- `ReservationSnapshotsResponse`
- `CancellationPolicySnapshotResponse`
- `DepositPolicySnapshotResponse`
- `TaxSnapshotResponse`

---

## ???? ??? Business Logic:

? **?????? ?? ???????:** ??? ?????? ?? ?? ???????? ???? ?????? `Permissions.Reservations.Read`

? **?????? ?? ??????:** 
- ?????? ????? ???? null
- ?????? ?? ???? Active

? **?????? ?? ?????:**
- ????? ????? ???? null
- ????? ????? ?????? ?????? (PropertyId ??????)

? **??? ??? Snapshots:**
- CancellationPolicySnapshot (?? ???)
- DepositPolicySnapshot (?? ???)
- ReservationTaxSnapshots (?????)

? **??? ????? ????????:**
- ?? ??????? `AsNoTracking()` ????? ??? ????? ?? ??????

---

## ????? ??????? ??? API:

```
GET /api/v1/reservations/{id}/snapshots?propertyId={propertyId}
```

**????:**
```
GET /api/v1/reservations/1/snapshots?propertyId=5
```

**Response:**
```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "reservationId": 1,
    "propertyId": 5,
    "cancellationPolicy": {
      "id": 1,
      "policyName": "Standard Cancellation",
      "rules": "{}",
      "freeCancellationHours": 48,
      "cancellationFeePercentage": 10.00,
      "fixedCancellationFee": 0.00,
      "isNonRefundable": false,
      "cutoffHours": 24,
      "capturedAt": "2024-01-01T10:00:00Z"
    },
    "depositPolicy": {
      "id": 1,
      "policyName": "10% Deposit",
      "type": 0,
      "amount": 0.00,
      "percentage": 10.00,
      "requiredDepositAmount": 100.00,
      "capturedAt": "2024-01-01T10:00:00Z"
    },
    "taxes": [
      {
        "id": 1,
        "taxCode": "VAT",
        "taxName": "Value Added Tax",
        "rate": 15.00,
        "type": 0,
        "isInclusive": true,
        "taxableAmount": 1000.00,
        "taxAmount": 150.00,
        "taxValidFrom": "2024-01-01T00:00:00Z",
        "taxValidTo": null
      }
    ],
    "retrievedAt": "2024-01-15T12:30:00Z"
  }
}
```

---

## ??????? ????:

?? **????????? ??????? ???:**
- `IReservationRepository.cs` - ????? method ???? ???
- `IReservationService.cs` - ????? method ???? ???
- `ReservationService.cs` - ????? method ???? + 3 mapping methods
- `ReservationRepository.cs` - ????? method ????
- `ReservationController.cs` - ????? endpoint ????
- `ReservationSnapshotsResponse.cs` - ??? ???? (DTOs)

?? **?? ??? ?????:**
- ?? business logic ???
- ?? entities
- ?? methods ??????

---
