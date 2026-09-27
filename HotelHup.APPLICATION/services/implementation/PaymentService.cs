using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Payments;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.implementation
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repository;
        private readonly IUserRepository _users;

        public PaymentService(
            IPaymentRepository repository,
            IUserRepository users)
        {
            _repository = repository;
            _users = users;
        }

        public async Task<ResponseStatus<PaymentResponseDto>>
            CreatePaymentAsync(
            User actor,
            int folioId,
            CreatePaymentRequest request,
            string? headerIdempotencyKey,
            CancellationToken ct = default)
        {
            // Business Rule: Payment amount must be greater than zero
            // and the payment method must be valid.
            if (request is null ||
                request.Amount <= 0 ||
                !Enum.IsDefined(request.Method))
            {
                return Fail<PaymentResponseDto>(
                    "Payment amount and method are invalid.",
                    "INVALID_PAYMENT",
                    422);
            }

            // Business Rule: Every payment request must have an idempotency key
            // to prevent duplicate payment creation.
            var key = string.IsNullOrWhiteSpace(headerIdempotencyKey)
                ? request.IdempotencyKey?.Trim()
                : headerIdempotencyKey.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                return Fail<PaymentResponseDto>(
                    "Idempotency-Key header or IdempotencyKey body value is required.",
                    "IDEMPOTENCY_KEY_REQUIRED",
                    422);
            }
            // Business Rule: The idempotency key cannot exceed 200 characters.
            if (key.Length > 200)
            {
                return Fail<PaymentResponseDto>(
                    "Idempotency key is too long.",
                    "INVALID_IDEMPOTENCY_KEY",
                    422);
            }
            try
            {
                return await _repository.ExecuteTransactionAsync(async () =>
                {
                    var existing =
                        await _repository.GetByIdempotencyKeyAsync(
                            key,
                            false,
                            ct);

                    // Business Rule: Reusing an idempotency key with
                    // different payment data is not allowed.
                    if (existing is not null)
                    {
                        if (existing.FolioId != folioId ||
                            existing.Amount != Money(request.Amount) ||
                            existing.Method != request.Method)
                        {
                            return Fail<PaymentResponseDto>(
                                "The idempotency key was already used for a different payment.",
                                "IDEMPOTENCY_CONFLICT",
                                409);
                        }

                        // Business Rule: Repeating the same payment request
                        // with the same idempotency key must return the existing payment
                        // instead of creating a duplicate payment.
                        return Ok(
                            Map(existing),
                            "The existing payment was returned.");
                    }

                    if (!string.IsNullOrWhiteSpace(request.ExternalId))
                    {
                        var duplicate =
                            await _repository.GetByExternalIdAsync(
                                request.ExternalId.Trim(),
                                false,
                                ct);

                        // Business Rule: An external transaction ID must be unique
                        // to prevent duplicate financial transactions.
                        if (duplicate is not null)
                        {
                            return Fail<PaymentResponseDto>(
                                "The external transaction already exists.",
                                "DUPLICATE_EXTERNAL_TRANSACTION",
                                409);
                        }
                    }

                    var folio =
                        await _repository.GetFolioForUpdateAsync(
                            folioId,
                            ct);

                    // Business Rule: A payment can only be created
                    // for an existing folio.
                    if (folio is null)
                    {
                        return Fail<PaymentResponseDto>(
                            "Folio was not found.",
                            "FOLIO_NOT_FOUND",
                            404);
                    }
                    //must reservation state is not cencel or noshow
                    if (folio.Reservation.Status is ReservationStatus.Cancelled || folio.Reservation.Status is  ReservationStatus.NoShow)
                    {
                        return Fail<PaymentResponseDto>(
                           "reservation is cancel or noshow.",
                           "RESERVATION IS CANCELED OR NOSHOW",
                           404);
                    }
                    var access =
                        await AuthorizeAsync(
                            actor,
                            folio,
                            Permissions.Payments.Create,
                            ct);

                    if (!access.Success)
                    {
                        return Fail<PaymentResponseDto>(
                            access.Message,
                            access.Code,
                            access.StatusCode);
                    }

                    // Business Rule: A closed folio cannot accept new payments.
                    if (folio.Status == FolioStatus.Closed)
                    {
                        return Fail<PaymentResponseDto>(
                            "Closed folio cannot accept payments.",
                            "FOLIO_CLOSED",
                            409);
                    }
                    
                    Recalculate(folio);

                    var amount = Money(request.Amount);

                    // Business Rule: Overpayment is allowed only when
                    // the property settings explicitly enable it.
                    var allowOverpayment =
                        folio.Reservation.Property?.Settings?.AllowOverpayment == true;

                    // Business Rule: When overpayment is disabled,
                    // the payment cannot exceed the outstanding folio balance.
                    if (!allowOverpayment && amount > folio.Balance)
                    {
                        return Fail<PaymentResponseDto>(
                            "Payment exceeds the outstanding folio balance.",
                            "PAYMENT_EXCEEDS_BALANCE",
                            422);
                    }

                    var now = DateTimeOffset.UtcNow;

                    var payment = new Payment
                    {
                        FolioId = folio.id,
                        Amount = amount,
                        Method = request.Method,
                        Status = PaymentStatus.Pending,
                        ExternalId = string.IsNullOrWhiteSpace(request.ExternalId)
                            ? null
                            : request.ExternalId.Trim(),
                        IdempotencyKey = key,
                        PaidAt = DateTime.UtcNow,
                        CreatedAt = now,
                        CreatedBy = actor.Id
                    };

                    folio.Payments.Add(payment);

                    payment.Status = PaymentStatus.Paid;

                    Recalculate(folio);

                    // Business Rule: If the folio still has an outstanding balance
                    // after the payment, the payment is marked as partially paid.
                    if (folio.Balance > 0)
                    {
                        payment.Status = PaymentStatus.PartiallyPaid;
                    }

                    await _repository.SaveChangesAsync(ct);

                    await _repository.AddAuditAsync(
                        Audit(
                            actor,
                            folio,
                            payment,
                            "Payment.Created",
                            null,
                            payment),
                        ct);

                    await _repository.SaveChangesAsync(ct);

                    return Ok(
                        Map(payment),
                        "Payment created successfully.",
                        201);

                }, ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<PaymentResponseDto>(
                    "The folio was modified by another request.",
                    "PAYMENT_CONCURRENCY_CONFLICT",
                    409);
            }
            catch (DbUpdateException)
            {
                return Fail<PaymentResponseDto>(
                    "The payment could not be persisted; retry with the same idempotency key.",
                    "PAYMENT_PERSISTENCE_CONFLICT",
                    409);
            }
        }

        public async Task<ResponseStatus<PaymentResponseDto>>
            GetPaymentByIdAsync(
            User actor,
            int paymentId,
            CancellationToken ct = default)
        {
            var payment =
                await _repository.GetByIdAsync(
                    paymentId,
                    false,
                    ct);

            if (payment is null)
            {
                return Fail<PaymentResponseDto>(
                    "Payment was not found.",
                    "PAYMENT_NOT_FOUND",
                    404);
            }

            var access =
                await AuthorizeAsync(
                    actor,
                    payment.Folio,
                    Permissions.Payments.Read,
                    ct);

            return access.Success
                ? Ok(Map(payment))
                : Fail<PaymentResponseDto>(
                    access.Message,
                    access.Code,
                    access.StatusCode);
        }

        public async Task<ResponseStatus<IReadOnlyList<PaymentResponseDto>>>
            GetPaymentsByFolioAsync(
            User actor,
            int folioId,
            CancellationToken ct = default)
        {
            var folio =
                await _repository.GetFolioForUpdateAsync(
                    folioId,
                    ct);

            if (folio is null)
            {
                return Fail<IReadOnlyList<PaymentResponseDto>>(
                    "Folio was not found.",
                    "FOLIO_NOT_FOUND",
                    404);
            }

            var access =
                await AuthorizeAsync(
                    actor,
                    folio,
                    Permissions.Payments.Read,
                    ct);

            if (!access.Success)
            {
                return Fail<IReadOnlyList<PaymentResponseDto>>(
                    access.Message,
                    access.Code,
                    access.StatusCode);
            }

            var payments =
                await _repository.GetByFolioIdAsync(
                    folioId,
                    ct);

            return Ok<IReadOnlyList<PaymentResponseDto>>(
                payments.Select(Map).ToList());
        }

        public async Task<ResponseStatus<PaymentSummaryDto>>
            GetSummaryAsync(
            User actor,
            int folioId,
            CancellationToken ct = default)
        {
            var folio =
                await _repository.GetFolioForUpdateAsync(
                    folioId,
                    ct);

            if (folio is null)
            {
                return Fail<PaymentSummaryDto>(
                    "Folio was not found.",
                    "FOLIO_NOT_FOUND",
                    404);
            }

            var access =
                await AuthorizeAsync(
                    actor,
                    folio,
                    Permissions.Payments.Read,
                    ct);

            if (!access.Success)
            {
                return Fail<PaymentSummaryDto>(
                    access.Message,
                    access.Code,
                    access.StatusCode);
            }

            Recalculate(folio);

            return Ok(new PaymentSummaryDto
            {
                FolioId = folio.id,
                Total = folio.Total,
                Balance = folio.Balance,
                SuccessfulPayments =
                    Money(
                        folio.Payments
                            .Where(IsSuccessful)
                            .Sum(x => x.Amount)),
                Refunds =
                    Money(
                        folio.Payments
                            .SelectMany(x => x.Refunds)
                            .Where(x => x.Status == RefundStatus.Completed)
                            .Sum(x => x.Amount)),
                Payments =
                    folio.Payments
                        .Select(Map)
                        .ToList()
            });
        }

        public async Task<ResponseStatus<PaymentResponseDto>>
            RefundPaymentAsync(
            User actor,
            int paymentId,
            RefundPaymentRequest request,
            CancellationToken ct = default)
        {
            // Business Rule: A refund must have a positive amount
            // and a non-empty reason.
            if (request is null ||
                request.Amount <= 0 ||
                string.IsNullOrWhiteSpace(request.Reason))
            {
                return Fail<PaymentResponseDto>(
                    "Refund amount and reason are required.",
                    "INVALID_REFUND",
                    422);
            }

            try
            {
                return await _repository.ExecuteTransactionAsync(async () =>
                {
                    var payment =
                        await _repository.GetByIdAsync(
                            paymentId,
                            true,
                            ct);

                    if (payment is null)
                    {
                        return Fail<PaymentResponseDto>(
                            "Payment was not found.",
                            "PAYMENT_NOT_FOUND",
                            404);
                    }

                    var access =
                        await AuthorizeAsync(
                            actor,
                            payment.Folio,
                            Permissions.Refunds.Create,
                            ct);

                    if (!access.Success)
                    {
                        var approval =
                            await AuthorizeAsync(
                                actor,
                                payment.Folio,
                                Permissions.Refunds.Approve,
                                ct);

                        if (!approval.Success)
                        {
                            return Fail<PaymentResponseDto>(
                                access.Message,
                                access.Code,
                                access.StatusCode);
                        }
                    }

                    // Business Rule: Only successful payments can be refunded.
                    if (payment.Status is PaymentStatus.Failed
                        or PaymentStatus.Voided ||
                        !IsSuccessful(payment))
                    {
                        return Fail<PaymentResponseDto>(
                            "Only a successful payment can be refunded.",
                            "PAYMENT_NOT_REFUNDABLE",
                            409);
                    }

                    var alreadyRefunded =
                        payment.Refunds
                            .Where(x =>
                                x.Status == RefundStatus.Completed)
                            .Sum(x => x.Amount);

                    var amount = Money(request.Amount);

                    // Business Rule: The total refunded amount
                    // cannot exceed the original payment amount.
                    if (alreadyRefunded + amount > payment.Amount)
                    {
                        return Fail<PaymentResponseDto>(
                            "Refund exceeds the refundable payment amount.",
                            "REFUND_EXCEEDS_PAYMENT",
                            422);
                    }

                    // Business Rule: An external refund transaction ID
                    // must be unique.
                    if (!string.IsNullOrWhiteSpace(request.ExternalId) &&
                        await _repository.GetByExternalIdAsync(
                            request.ExternalId.Trim(),
                            false,
                            ct) is not null)
                    {
                        return Fail<PaymentResponseDto>(
                            "The external refund transaction already exists.",
                            "DUPLICATE_EXTERNAL_TRANSACTION",
                            409);
                    }

                    var refund = new Refund
                    {
                        PaymentId = payment.Id,
                        FolioId = payment.FolioId,
                        Method = payment.Method,
                        Amount = amount,
                        Status = RefundStatus.Completed,
                        Reason = request.Reason.Trim(),
                        ExternalId =
                            string.IsNullOrWhiteSpace(request.ExternalId)
                                ? null
                                : request.ExternalId.Trim(),
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = actor.Id
                    };

                    payment.Refunds.Add(refund);

                    // Business Rule: A payment is fully refunded when
                    // the total completed refunds reach the original payment amount.
                    payment.Status =
                        Money(alreadyRefunded + amount) >= payment.Amount
                            ? PaymentStatus.Refunded
                            : PaymentStatus.PartiallyRefunded;

                    Recalculate(payment.Folio);

                    await _repository.SaveChangesAsync(ct);

                    await _repository.AddAuditAsync(
                        Audit(
                            actor,
                            payment.Folio,
                            payment,
                            "Payment.Refunded",
                            null,
                            refund),
                        ct);

                    await _repository.SaveChangesAsync(ct);

                    return Ok(
                        Map(payment),
                        "Payment refunded successfully.");

                }, ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<PaymentResponseDto>(
                    "The payment was modified by another request.",
                    "REFUND_CONCURRENCY_CONFLICT",
                    409);
            }
        }

        //--------------------------------------------------------------------------------------
        // Private Methods
        //--------------------------------------------------------------------------------------

        private async Task<ResponseStatus<bool>>
            AuthorizeAsync(
            User actor,
            Folio folio,
            string permission,
            CancellationToken ct)
        {
            // Business Rule: Only active users can perform payment operations.
            if (actor is null || !actor.IsActive)
            {
                return Fail<bool>(
                    "User is not authenticated.",
                    "UNAUTHENTICATED",
                    401);
            }

            // Business Rule: Non-admin users can only access
            // payments belonging to their assigned property.
            if (actor.PropertyId.HasValue &&
                actor.PropertyId.Value != folio.Reservation.PropertyId)
            {
                return Fail<bool>(
                    "You are not authorized to access this property.",
                    "PROPERTY_ACCESS_DENIED",
                    403);
            }

            var roles =
                await _users.GetRolesAsync(
                    actor.Id,
                    ct);

            // Business Rule: An active Admin can perform
            // payment operations regardless of assigned property.
            if (roles.Any(x =>
                x.IsActive &&
                string.Equals(
                    x.Name,
                    nameof(UserRole.Admin),
                    StringComparison.OrdinalIgnoreCase)))
            {
                return Ok(true);
            }

            var permissions =
                await _users.GetPermissionNamesAsync(
                    actor.Id,
                    ct);

            // Business Rule: Non-admin users must have
            // the required permission to perform the operation.
            return permissions.Contains(permission)
                ? Ok(true)
                : Fail<bool>(
                    "You are not allowed to perform this payment operation.",
                    "FORBIDDEN_PAYMENT_OPERATION",
                    403);
        }

        private static bool IsSuccessful(Payment x)
        {
            return x.Status is
                PaymentStatus.Paid
                or PaymentStatus.PartiallyPaid
                or PaymentStatus.PartiallyRefunded
                or PaymentStatus.Refunded;
        }

        private static void Recalculate(Folio folio)
        {
            // Business Rule: Only posted folio items contribute
            // to the folio total.
            var active =
                folio.Items
                    .Where(x =>
                        x.Status == FolioItemStatus.Posted)
                    .ToList();

            // Business Rule: Charges include room charges,
            // services, taxes, fees, and adjustments.
            var charges =
                active
                    .Where(x =>
                        x.Type is
                            FolioItemType.RoomCharge
                            or FolioItemType.Service
                            or FolioItemType.Tax
                            or FolioItemType.Fee
                            or FolioItemType.Adjustment)
                    .Sum(x => x.Amount);

            // Business Rule: All posted item taxes
            // are included in the folio total.
            var taxes =
                active.Sum(x => x.Tax);

            // Business Rule: Discounts reduce the folio total.
            var discounts =
                active
                    .Where(x =>
                        x.Type == FolioItemType.Discount)
                    .Sum(x => x.Amount);

            folio.Total =
                Money(
                    Math.Max(
                        0m,
                        charges + taxes - discounts));

            // Business Rule: Successful payments reduce
            // the outstanding folio balance.
            var paid =
                folio.Payments
                    .Where(IsSuccessful)
                    .Sum(x => x.Amount);

            // Business Rule: Completed refunds increase
            // the outstanding folio balance.
            var refunded =
                folio.Payments
                    .SelectMany(x => x.Refunds)
                    .Where(x =>
                        x.Status == RefundStatus.Completed)
                    .Sum(x => x.Amount);

            var difference =
                Money(
                    folio.Total - paid + refunded);

            // Business Rule: The folio balance cannot be negative.
            folio.Balance =
                Math.Max(
                    0m,
                    difference);

            // Business Rule: Any amount paid above the folio total
            // is recorded as credit balance.
            folio.CreditBalance =
                Math.Max(
                    0m,
                    -difference);
        }

        private static AuditLog Audit(
            User actor,
            Folio folio,
            Payment payment,
            string action,
            object? oldValues,
            object? newValues)
        {
            return new AuditLog
            {
                UserId = actor.Id,
                PropertyId = folio.Reservation.PropertyId,
                EntityName = nameof(Payment),
                EntityId = payment.Id.ToString(),
                Action = action,
                OldValues =
                    oldValues is null
                        ? null
                        : JsonSerializer.Serialize(oldValues),
                NewValues =
                    newValues is null
                        ? null
                        : JsonSerializer.Serialize(newValues),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actor.Id
            };
        }

        private static PaymentResponseDto Map(Payment x)
        {
            return new PaymentResponseDto
            {
                Id = x.Id,
                FolioId = x.FolioId,
                Amount = x.Amount,
                Method = x.Method,
                Status = x.Status,
                ExternalId = x.ExternalId,
                IdempotencyKey = x.IdempotencyKey,
                PaidAt = x.PaidAt,

                RefundedAmount =
                    Money(
                        x.Refunds
                            .Where(r =>
                                r.Status == RefundStatus.Completed)
                            .Sum(r => r.Amount)),

                Refunds =
                    x.Refunds
                        .Select(r =>
                            new RefundResponseDto
                            {
                                Id = r.id,
                                PaymentId = r.PaymentId,
                                FolioId = r.FolioId,
                                Amount = r.Amount,
                                Status = r.Status,
                                Reason = r.Reason,
                                ExternalId = r.ExternalId
                            })
                        .ToList()
            };
        }

        private static decimal Money(decimal value)
        {
            return decimal.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static ResponseStatus<T> Ok<T>(
            T data,
            string message = "",
            int status = 200)
        {
            return new ResponseStatus<T>(
                data,
                message,
                status);
        }

        private static ResponseStatus<T> Fail<T>(
            string message,
            string? code = null,
            int status = 400)
        {
            return new ResponseStatus<T>(
                message: message,
                statusCode: status,
                code: code);
        }
    }
}