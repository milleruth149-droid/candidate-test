using Requests.Domain.Entities;
namespace Requests.Application.Requests;

public sealed class RequestService : IRequestService

 {   // תקרות שאנחנו קובעות. הן מגינות על השרת מבקשות קיצוניות.
    private const int MaxPageSize = 100;
    private const int MaxPage = 1_000_000;
    private const int MaxRequestNumberLength = 50;


    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }

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

     // הפעולה החדשה: חיפוש.
    public async Task<PagedResult<RequestDto>> SearchAsync(
        RequestSearchRequest request,
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        // שלב 1: בודקים את הקלט. אם יש בעיה, נזרקת RequestValidationException.
        Validate(request);

        // שלב 2: ממירים תאריכים. DateOnly (תאריך בלי שעה) הופך לרגע מדויק ב-UTC.
        // "מ-" מתחיל בחצות של אותו יום.
        DateTime? fromUtc = request.CreatedFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // "עד-" כולל את היום כולו, ולכן לוקחים את חצות של היום שאחריו, וה-DB יבדוק "קטן מ-".
        // למשל: "עד 30.6" הופך ל-"קטן מ-1.7 בחצות".
        DateTime? toExclusiveUtc = request.CreatedTo?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // שלב 3: בונים את החבילה הפנימית. כאן נכנסים פרטי המשתמש מהשרת.
        var criteria = new RequestSearchCriteria(
            RequestNumber: request.RequestNumber?.Trim(),
            // Distinct = מסירים כפילויות, למשל status=1&status=1.
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

        // שלב 4: מעבירים ל-Repository, שמריץ את השאילתה ב-DB.
        return await _repository.SearchAsync(criteria, cancellationToken);
    }

    // בדיקת הקלט. אוספים את כל השגיאות ורק בסוף זורקים, כדי שהמשתמש יראה הכל בבת אחת.
    private static void Validate(RequestSearchRequest request)
    {
        var errors = new List<string>();

        // עמוד חייב להיות 1 ומעלה. יש גם תקרה, כדי למנוע חישוב דילוג ענקי.
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
