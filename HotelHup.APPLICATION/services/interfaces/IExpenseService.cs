using HotelHup.APPLICATION.DTO.Expense;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface IExpenseService
    {
        Task<ResponseStatus<ExpenseResponseDto>> CreateAsync(CreateExpenseDto dto, User actor, CancellationToken ct = default);
        Task<ResponseStatus<PagedResponse<ExpenseResponseDto>>> GetPagedAsync(ExpenseQueryDto dto, User actor, CancellationToken ct = default);
        Task<ResponseStatus<ExpenseResponseDto>> GetByIdAsync(int id, User actor, CancellationToken ct = default);
        Task<ResponseStatus<ExpenseResponseDto>> UpdateAsync(int id, UpdateExpenseDto dto, User actor, CancellationToken ct = default);
        Task<ResponseStatus<ExpenseResponseDto>> VoidAsync(int id, VoidExpenseDto dto, User actor, CancellationToken ct = default);
    }

}
