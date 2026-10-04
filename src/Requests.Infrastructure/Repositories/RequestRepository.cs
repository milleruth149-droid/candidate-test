using Microsoft.EntityFrameworkCore;
using Requests.Application.Requests;
using Requests.Domain.Entities;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Repositories;

public sealed class RequestRepository : IRequestRepository
{
    // החיבור ל-DB. מגיע מבחוץ (Dependency Injection), ולכן לא יוצרים אותו כאן.
    private readonly RequestsDbContext _db;

    public RequestRepository(RequestsDbContext db)
    {
        _db = db;
    }

    // הפעולה הישנה. נשארת זמנית כדי שהפרויקט ימשיך להיבנות.
    public Task<List<Request>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _db.Requests.ToListAsync(cancellationToken);
    }

    // הפעולה החדשה: חיפוש.
    public async Task<PagedResult<RequestDto>> SearchAsync(
        RequestSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // מתחילים משאילתה על הטבלה. היא עדיין לא רצה ב-DB.
        // AsNoTracking = רק קוראות, לא משנות, ולכן זה מהיר יותר.
        IQueryable<Request> query = _db.Requests.AsNoTracking();

        // מוסיפים את ההרשאות לשאילתה (ב-DB, לא בזיכרון).
        query = ApplyPermissions(query, criteria);

        // מוסיפים את הפילטרים של המשתמש.
        query = ApplyFilters(query, criteria);

        // סופרים כמה רשומות מתאימות בסך הכל. כאן ה-DB רץ בפעם הראשונה.
        var totalCount = await query.CountAsync(cancellationToken);

        // מוסיפים את המיון לפי בחירת המשתמש.
        // המיון חייב לבוא לפני Skip ו-Take.
        var sortedQuery = ApplySorting(query, criteria);

        var items = await sortedQuery
            // מדלגים על העמודים הקודמים. עמוד 3 עם 20 בעמוד = מדלגים על 40.
            .Skip((criteria.Page - 1) * criteria.PageSize)

            // לוקחים רק את הרשומות של העמוד הנוכחי.
            .Take(criteria.PageSize)

            // ממירים ל-DTO כבר ב-DB: נשלפות רק העמודות שצריך.
            .Select(x => new RequestDto(
                x.Id,
                x.RequestNumber,
                x.CustomerId,
                x.OwnerId,
                x.AssignedToUserId,
                x.Status,
                x.RequestType,
                x.CreatedAt))

            // כאן ה-DB רץ בפעם השנייה ומחזיר את הרשומות.
            .ToListAsync(cancellationToken);

        return new PagedResult<RequestDto>(
            items,
            criteria.Page,
            criteria.PageSize,
            totalCount);
    }

    // פונקציית עזר: מוסיפה את תנאי ההרשאה.
    private static IQueryable<Request> ApplyPermissions(
        IQueryable<Request> query,
        RequestSearchCriteria criteria)
    {
        // מנהל רואה הכל, ולכן לא מוסיפים שום תנאי.
        if (criteria.IsAdministrator)
        {
            return query;
        }

        // משתמש רגיל: רואה רק בקשות שבבעלותו או שהוקצו אליו.
        var userId = criteria.CurrentUserId;
        return query.Where(x => x.OwnerId == userId || x.AssignedToUserId == userId);
    }

    // פונקציית עזר: מוסיפה את הפילטרים. כל פילטר נוסף רק אם המשתמש שלח אותו.
    private static IQueryable<Request> ApplyFilters(
        IQueryable<Request> query,
        RequestSearchCriteria criteria)
    {
        // חיפוש חלקי לפי מספר בקשה: "מתחיל ב-".
        // StartsWith ב-DB אמיתי יכול להשתמש באינדקס, בניגוד ל-Contains.
        if (!string.IsNullOrWhiteSpace(criteria.RequestNumber))
        {
            var number = criteria.RequestNumber.Trim();
            query = query.Where(x => x.RequestNumber.StartsWith(number));
        }

        // סטטוס מרובה: "הסטטוס הוא אחד מאלה". תורגם ל-IN ב-SQL.
        if (criteria.Statuses.Count > 0)
        {
            var statuses = criteria.Statuses.ToList();
            query = query.Where(x => statuses.Contains(x.Status));
        }

        // סוג בקשה. HasValue = האם נשלח ערך.
        if (criteria.RequestType.HasValue)
        {
            var type = criteria.RequestType.Value;
            query = query.Where(x => x.RequestType == type);
        }

        // תחילת הטווח (כולל).
        if (criteria.CreatedFromUtc.HasValue)
        {
            var from = criteria.CreatedFromUtc.Value;
            query = query.Where(x => x.CreatedAt >= from);
        }

        // סוף הטווח (לא כולל, ולכן סימן "קטן מ-").
        if (criteria.CreatedToUtcExclusive.HasValue)
        {
            var to = criteria.CreatedToUtcExclusive.Value;
            query = query.Where(x => x.CreatedAt < to);
        }

        return query;
    }

    // פונקציית עזר: מוסיפה את המיון לשאילתה.
    private static IQueryable<Request> ApplySorting(
        IQueryable<Request> query,
        RequestSearchCriteria criteria)
    {
        // האם המיון יורד? שומרים את התשובה פעם אחת.
        var desc = criteria.SortDirection == SortDirection.Desc;

        // switch = לפי הערך של SortBy, בוחרים איך למיין.
        // רק שדות מהרשימה הסגורה שהגדרנו אפשריים.
        IOrderedQueryable<Request> ordered = criteria.SortBy switch
        {
            RequestSortField.RequestNumber => desc
                ? query.OrderByDescending(x => x.RequestNumber)
                : query.OrderBy(x => x.RequestNumber),

            RequestSortField.Status => desc
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),

            RequestSortField.RequestType => desc
                ? query.OrderByDescending(x => x.RequestType)
                : query.OrderBy(x => x.RequestType),

            // ברירת מחדל (כולל CreatedAt): מיון לפי תאריך יצירה.
            _ => desc
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };

        // מיון משני לפי Id, באותו כיוון, כדי שהסדר יהיה קבוע
        // גם כשיש ערכים שווים, והרשומות לא "יקפצו" בין עמודים.
        return desc
            ? ordered.ThenByDescending(x => x.Id)
            : ordered.ThenBy(x => x.Id);
    }
}