namespace Requests.Application.Requests;

// החוזה של ה-Service: מה אפשר לבקש ממנו.
public interface IRequestService
{
    // הפעולה הישנה: מחזירה הכל. נשארת זמנית ונמחק אותה בהמשך.
    Task<IReadOnlyList<RequestDto>> GetRequestsAsync(
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    // הפעולה החדשה: חיפוש וסינון.
    // request = מה שהמשתמש שלח בכתובת (עדיין לא נבדק).
    // currentUserId ו-isAdministrator = מי המשתמש. מגיעים מה-Controller, לא מהמשתמש עצמו.
    // מחזירה עמוד אחד של תוצאות.
    // אם הקלט לא חוקי, תזרוק RequestValidationException.
    Task<PagedResult<RequestDto>> SearchAsync(
        RequestSearchRequest request,
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}