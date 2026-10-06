using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public sealed class RequestService : IRequestService

{
   // תקרות אבטחה להגנה על השרת מפני בקשות קיצוניות 
        private const int MaxPageSize = 100;
    private const int MaxPage = 1_000_000;
    private const int MaxRequestNumberLength = 50;


    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }


//שליפת הסטטוסים האפשריים דינמית מתוך ה-Enum במערכת
    public async Task<IEnumerable<object>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var statuses = Enum.GetValues<RequestStatus>()
            .Select(s => new 
            { 
                Id = (int)s, 
                Label = s.ToString() 
            });

        return await Task.FromResult(statuses);
    }

// שליפת סוגי הבקשות האפשריים דינמית מתוך ה-Enum במערכת
    public async Task<IEnumerable<object>> GetTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = Enum.GetValues<RequestType>()
            .Select(t => new 
            { 
                Id = (int)t, 
                Label = t.ToString() 
            });

        return await Task.FromResult(types);
    }

    // הפעולה הישנה: מחזירה את כל הבקשות, עם סינון לפי משתמש אם הוא לא מנהל.
    public async Task<IReadOnlyList<RequestDto>> GetRequestsAsync(
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var requests = await _repository.GetAllAsync(cancellationToken);

        if (!isAdministrator)
        {
            requests = requests
                .Where(x => x.OwnerId == currentUserId || x.AssignedToUserId == currentUserId)
                .ToList();
        }

        return requests.Select(x => new RequestDto(
            x.Id,
            x.RequestNumber,
            x.CustomerId,
            x.OwnerId,
            x.AssignedToUserId,
            x.Status,
            x.RequestType,
            x.CreatedAt)).ToList();
    }

    // חיפוש וסינון בקשות מתקדם הכולל תמיכה בדפדוף (Pagination)
    public async Task<PagedResult<RequestDto>> SearchAsync(
        RequestSearchRequest request,
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
// שלב 1: בדיקת תקינות הקלט (Validation)

        Validate(request);

        // המרת תאריכים ל-UTC (תחילת היום עד חצות היום העוקב)
        DateTime? fromUtc = request.CreatedFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime? toExclusiveUtc = request.CreatedTo?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);



        var criteria = new RequestSearchCriteria(
            RequestNumber: request.RequestNumber?.Trim(),
            // Distinct = מסירים כפילויות 
            Statuses: request.Status.Distinct().ToList(),
            RequestType: request.Type,
            CreatedFromUtc: fromUtc,
            CreatedToUtcExclusive: toExclusiveUtc,
            SortBy: request.SortBy,
            SortDirection: request.SortDir,
            Page: request.Page,
            PageSize: request.PageSize,
            CurrentUserId: currentUserId,
            IsAdministrator: isAdministrator);

   // שליחה ל-Repository לביצוע השאילתה במסד הנתונים
        return await _repository.SearchAsync(criteria, cancellationToken);
    }

//בדיקת קלט מקיפה: אוספת את כל השגיאות יחד כדי להציג למשתמש תמונת מצב מלאה  
    private static void Validate(RequestSearchRequest request)
    {
        var errors = new List<string>();

        if (request.Page < 1 || request.Page > MaxPage)
            errors.Add($"page must be between 1 and {MaxPage}.");

        // גודל עמוד: תקרה מגינה על השרת מפני בקשה למיליון רשומות בבת אחת.
        if (request.PageSize < 1 || request.PageSize > MaxPageSize)
            errors.Add($"pageSize must be between 1 and {MaxPageSize}.");

        // אורך מקסימלי למספר בקשה, כדי לא לקבל מחרוזת ענקית.
        if (request.RequestNumber is { Length: > MaxRequestNumberLength })
            errors.Add($"requestNumber must be at most {MaxRequestNumberLength} characters.");

        // מספר כמו status=99 מתקבל כ-enum, אבל הוא לא ערך קיים. בודקים בעצמנו.
        if (request.Status.Any(s => !Enum.IsDefined(s)))
            errors.Add("status contains an unknown value.");

        // אותה בדיקה לסוג בקשה.
        if (request.Type.HasValue && !Enum.IsDefined(request.Type.Value))
            errors.Add("type is not a valid request type.");

        // ובדיקה דומה לשדה המיון ולכיוון המיון.
        if (!Enum.IsDefined(request.SortBy))
            errors.Add("sortBy is not a valid sort field.");

        if (!Enum.IsDefined(request.SortDir))
            errors.Add("sortDir must be Asc or Desc.");

        // תאריך התחלה לא יכול להיות אחרי תאריך הסיום.
        if (request.CreatedFrom.HasValue && request.CreatedTo.HasValue
            && request.CreatedFrom.Value > request.CreatedTo.Value)
            errors.Add("createdFrom must not be later than createdTo.");

        // התאריך המקסימלי האפשרי לא מאפשר להוסיף יום (זה היה קורס).
        if (request.CreatedTo == DateOnly.MaxValue)
            errors.Add("createdTo is out of range.");

        // אם נמצאה שגיאה כלשהי, זורקים את כולן יחד.
        if (errors.Count > 0)
            throw new RequestValidationException(errors);
    }

}
