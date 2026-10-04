// using = "אני רוצה להשתמש בדברים שמוגדרים במקום אחר".
// כאן אנחנו מביאים את RequestStatus ו-RequestType מהשכבה Domain.
using Requests.Domain.Entities;

// namespace = "כתובת" של הקבצים. כל הקבצים בתיקייה הזו משתמשים באותה כתובת.
namespace Requests.Application.Requests;

// enum = רשימה סגורה של ערכים אפשריים.
// זו הרשימה של השדות שמותר למיין לפיהם. לקוח לא יכול לבקש מיון לפי שדה אחר.
public enum RequestSortField
{
    CreatedAt,        // מיון לפי תאריך יצירה
    RequestNumber,    // מיון לפי מספר בקשה
    Status,           // מיון לפי סטטוס
    RequestType       // מיון לפי סוג בקשה
}

// רשימה סגורה נוספת: כיוון המיון.
public enum SortDirection
{
    Asc,   // עולה (מהקטן לגדול)
    Desc   // יורד (מהגדול לקטן)
}

// record = "חבילת נתונים". כאן אנחנו אורזים את כל פרטי החיפוש בחבילה אחת.
// ככה לא צריך להעביר 11 פרמטרים נפרדים בין השכבות.
public sealed record RequestSearchCriteria(

    // ה-? אומר "אופציונלי". אם הערך ריק (null), אין סינון לפי מספר בקשה.
    string? RequestNumber,

    // רשימה של סטטוסים. אפשר לבחור כמה. רשימה ריקה = בלי סינון לפי סטטוס.
    IReadOnlyList<RequestStatus> Statuses,

    // סוג בקשה אחד (אופציונלי).
    RequestType? RequestType,

    // תחילת טווח התאריכים (כולל התאריך הזה). אופציונלי.
    DateTime? CreatedFromUtc,

    // סוף טווח התאריכים. Exclusive = התאריך הזה עצמו לא נכלל.
    // אם המשתמש בוחר "עד 30.6", נשמור כאן 1.7, וכך כל יום 30.6 נכלל.
    DateTime? CreatedToUtcExclusive,

    // לפי איזה שדה למיין.
    RequestSortField SortBy,

    // בכיוון עולה או יורד.
    SortDirection SortDirection,

    // מספר העמוד המבוקש (מתחיל מ-1).
    int Page,

    // כמה רשומות בעמוד.
    int PageSize,

    // המספר של המשתמש המחובר. נחוץ כדי לדעת אילו בקשות הוא רשאי לראות.
    int CurrentUserId,

    // האם המשתמש הוא מנהל (Administrator). מנהל רואה הכל.
    bool IsAdministrator);