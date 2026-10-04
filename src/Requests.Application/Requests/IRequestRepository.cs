using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public interface IRequestRepository
{
    // הפעולה הישנה 
    Task<List<Request>> GetAllAsync(CancellationToken cancellationToken = default);
      // הפעולה החדשה: חיפוש לפי קריטריונים.
        Task<PagedResult<RequestDto>> SearchAsync(
        RequestSearchCriteria criteria,
        CancellationToken cancellationToken = default);

}
