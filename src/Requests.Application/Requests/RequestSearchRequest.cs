using Requests.Domain.Entities;

namespace Requests.Application.Requests;

// מה שהמשתמש שולח לנו בכתובת (Query String), למשל:
// /api/requests?requestNumber=REQ-0001&status=1&status=2&page=2
// כל השדות כאן אופציונליים, ולכן יש להם ערכי ברירת מחדל.
public sealed class RequestSearchRequest
{
    // חיפוש חלקי לפי מספר בקשה. אם לא נשלח, אין סינון.
    public string? RequestNumber { get; init; }

    // רשימת סטטוסים. אם שולחים status=1&status=2 נקבל רשימה עם שני ערכים.
    // ברירת מחדל: רשימה ריקה (בלי סינון).
    public List<RequestStatus> Status { get; init; } = new();

    // סוג בקשה אחד. אם לא נשלח, אין סינון.
    public RequestType? Type { get; init; }

    // תאריך התחלה (כולל), בפורמט 2026-01-31. DateOnly = תאריך בלי שעה.
    public DateOnly? CreatedFrom { get; init; }

    // תאריך סיום (כולל את היום עצמו), בפורמט 2026-06-30.
    public DateOnly? CreatedTo { get; init; }

    // לפי איזה שדה למיין. ברירת מחדל: תאריך יצירה.
    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;

    // כיוון המיון. ברירת מחדל: יורד (מהחדש לישן).
    public SortDirection SortDir { get; init; } = SortDirection.Desc;

    // מספר העמוד (מתחיל מ-1).
    public int Page { get; init; } = 1;

    // כמה רשומות בעמוד. ברירת מחדל: 20.
    public int PageSize { get; init; } = 20;
}