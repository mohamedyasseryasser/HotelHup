using HotelHup.APPLICATION.DTO.folio;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IFolioService
    {
        Task<ResponseStatus<FolioResponseDto>> GetAsync(User actor, int folioId, CancellationToken ct = default);
        Task<ResponseStatus<FolioResponseDto>> AddItemAsync(User actor, int folioId, AddFolioItemRequestDto request, CancellationToken ct = default);
        Task<ResponseStatus<FolioResponseDto>> VoidItemAsync(User actor, int folioId, int itemId, VoidFolioItemRequestDto request, CancellationToken ct = default);
        Task<ResponseStatus<FolioResponseDto>> ReopenAsync(User actor, int folioId, ReopenFolioRequestDto request, CancellationToken ct = default);
        Task<ResponseStatus<FolioResponseDto>> CloseAsync(User actor, int folioId, CloseFolioRequestDto request, CancellationToken ct = default);
        Task<ResponseStatus<FolioItemPageDto>> GetItemsAsync(User actor, int folioId, FolioItemPageRequest request, CancellationToken ct = default);
        Task<ResponseStatus<FolioSummaryDto>> GetSummaryAsync(User actor, int folioId, CancellationToken ct = default);
    }

}
