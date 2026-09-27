using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Payments;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface IPaymentService
    {
        Task<ResponseStatus<PaymentResponseDto>> CreatePaymentAsync(User actor, int folioId, CreatePaymentRequest request, string? headerIdempotencyKey, CancellationToken ct = default);
        Task<ResponseStatus<PaymentResponseDto>> GetPaymentByIdAsync(User actor, int paymentId, CancellationToken ct = default);
        Task<ResponseStatus<IReadOnlyList<PaymentResponseDto>>> GetPaymentsByFolioAsync(User actor, int folioId, CancellationToken ct = default);
        Task<ResponseStatus<PaymentSummaryDto>> GetSummaryAsync(User actor, int folioId, CancellationToken ct = default);
        Task<ResponseStatus<PaymentResponseDto>> RefundPaymentAsync(User actor, int paymentId, RefundPaymentRequest request, CancellationToken ct = default);
    }

}
