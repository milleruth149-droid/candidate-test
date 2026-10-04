using Microsoft.EntityFrameworkCore;
using Requests.Application.Requests;
using Requests.Domain.Entities;
using Requests.Infrastructure.Persistence;
using Requests.Infrastructure.Repositories;
using Xunit;

namespace Requests.Tests;

public class RequestRepositoryTests
{
    // ---------- בדיקות הרשאות ----------

    [Fact]
    public async Task RegularUser_SeesOnlyOwnedOrAssignedRequests()
    {
        // הכנה: 4 בקשות עם בעלים והקצאות שונים.
        var repo = CreateRepository(
            Make(1, owner: 1, assigned: 5),      // בבעלות משתמש 1
            Make(2, owner: 3, assigned: 1),      // מוקצית למשתמש 1
            Make(3, owner: 3, assigned: 5),      // לא קשורה למשתמש 1
            Make(4, owner: 1, assigned: null));  // בבעלות משתמש 1, בלי הקצאה

        // פעולה: משתמש 1 (לא מנהל) מחפש בלי שום פילטר.
        var result = await repo.SearchAsync(Criteria(userId: 1, admin: false));

        // בדיקה: רואה רק את 1, 2, 4. בקשה 3 לא מופיעה.
        Assert.Equal(3, result.TotalCount);
        Assert.DoesNotContain(result.Items, x => x.Id == 3);
    }

    [Fact]
    public async Task Administrator_SeesAllRequests()
    {
        var repo = CreateRepository(
            Make(1, owner: 1, assigned: 5),
            Make(2, owner: 3, assigned: 1),
            Make(3, owner: 3, assigned: 5),
            Make(4, owner: 1, assigned: null));

        // מנהל: רואה את כולן, גם אלה שלא קשורות אליו.
        var result = await repo.SearchAsync(Criteria(userId: 99, admin: true));

        Assert.Equal(4, result.TotalCount);
    }

    [Fact]
    public async Task Filters_AreCombinedWithPermissions_NotBypassingThem()
    {
        // בקשה 2 היא Completed, אבל לא שייכת למשתמש 1.
        var repo = CreateRepository(
            Make(1, owner: 1, assigned: null, status: RequestStatus.Completed),
            Make(2, owner: 3, assigned: 5, status: RequestStatus.Completed));

        // משתמש 1 מסנן לפי Completed.
        var result = await repo.SearchAsync(Criteria(
            userId: 1,
            statuses: new[] { RequestStatus.Completed }));

        // הפילטר לא "עוקף" את ההרשאה: רק בקשה 1 חוזרת.
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
    }

    // ---------- בדיקות פילטרים ----------

    [Fact]
    public async Task StatusFilter_WithMultipleValues_ReturnsAnyOfThem()
    {
        var repo = CreateRepository(
            Make(1, owner: 1, assigned: null, status: RequestStatus.New),
            Make(2, owner: 1, assigned: null, status: RequestStatus.InProgress),
            Make(3, owner: 1, assigned: null, status: RequestStatus.Completed));

        // מבקשים New או Completed (שני סטטוסים).
        var result = await repo.SearchAsync(Criteria(
            admin: true,
            statuses: new[] { RequestStatus.New, RequestStatus.Completed }));

        // חוזרות 1 ו-3, ולא 2.
        Assert.Equal(new[] { 1, 3 }, result.Items.Select(x => x.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task RequestNumber_MatchesFromTheStart_NotInTheMiddle()
    {
        var repo = CreateRepository(
            Make(1, owner: 1, assigned: null, number: "REQ-100"),
            Make(2, owner: 1, assigned: null, number: "REQ-101"),
            Make(3, owner: 1, assigned: null, number: "REQ-200"));

        // "REQ-10" מתחיל ב-REQ-100 וב-REQ-101.
        var byStart = await repo.SearchAsync(Criteria(admin: true, number: "REQ-10"));
        Assert.Equal(2, byStart.TotalCount);

        // "00" קיים באמצע המספרים, אבל לא בתחילתם, ולכן אין תוצאות.
        // זה מה שמבדיל "מתחיל ב-" מ"מכיל".
        var inMiddle = await repo.SearchAsync(Criteria(admin: true, number: "00"));
        Assert.Equal(0, inMiddle.TotalCount);
    }

    [Fact]
    public async Task DateRange_EndIsExclusive_SoWholeLastDayIsIncluded()
    {
        var repo = CreateRepository(
            Make(1, owner: 1, assigned: null, createdAt: new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc)),
            Make(2, owner: 1, assigned: null, createdAt: new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc)),
            Make(3, owner: 1, assigned: null, createdAt: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));

        // "עד 30.6" מגיע ל-Repository כ"קטן מ-1.7 בחצות".
        var result = await repo.SearchAsync(Criteria(
            admin: true,
            toExclusive: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));

        // בקשות 1 ו-2 נכללות (גם זו שנוצרה ב-23:59 ב-30.6). בקשה 3 (בדיוק בחצות 1.7) לא.
        Assert.Equal(new[] { 1, 2 }, result.Items.Select(x => x.Id).OrderBy(id => id));
    }

    // ---------- בדיקות עימוד ומיון ----------

    [Fact]
    public async Task Paging_ReturnsRequestedPage_AndTotalCountOfAllMatches()
    {
        // 25 בקשות.
        var repo = CreateRepository(
            Enumerable.Range(1, 25).Select(i => Make(i, owner: 1, assigned: null)).ToArray());

        // עמוד 3 עם 10 בעמוד: אמורות להיות רק 5 (בקשות 21 עד 25).
        var result = await repo.SearchAsync(Criteria(admin: true, page: 3, pageSize: 10));

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);   // הסך הכל, לא רק העמוד
        Assert.Equal(3, result.Page);
    }

    [Fact]
    public async Task Sorting_WithEqualValues_IsStableAcrossPages()
    {
        // 10 בקשות עם אותו Status. בלי מיון משני, הסדר ביניהן לא מובטח.
        var repo = CreateRepository(
            Enumerable.Range(1, 10)
                .Select(i => Make(i, owner: 1, assigned: null, status: RequestStatus.New))
                .ToArray());

        // ממיינים לפי Status (כולם שווים) ומבקשים שני עמודים של 5.
        var page1 = await repo.SearchAsync(Criteria(
            admin: true, sortBy: RequestSortField.Status, page: 1, pageSize: 5));
        var page2 = await repo.SearchAsync(Criteria(
            admin: true, sortBy: RequestSortField.Status, page: 2, pageSize: 5));

        var allIds = page1.Items.Concat(page2.Items).Select(x => x.Id).ToList();

        // אין כפילויות ואין רשומות חסרות: כל 10 הבקשות מופיעות בדיוק פעם אחת.
        Assert.Equal(10, allIds.Distinct().Count());
    }

    [Fact]
    public async Task Sorting_ByRequestNumberAscending_OrdersResults()
    {
        var repo = CreateRepository(
            Make(1, owner: 1, assigned: null, number: "REQ-300"),
            Make(2, owner: 1, assigned: null, number: "REQ-100"),
            Make(3, owner: 1, assigned: null, number: "REQ-200"));

        var result = await repo.SearchAsync(Criteria(
            admin: true,
            sortBy: RequestSortField.RequestNumber,
            direction: SortDirection.Asc));

        // סדר עולה: 100, 200, 300.
        Assert.Equal(
            new[] { "REQ-100", "REQ-200", "REQ-300" },
            result.Items.Select(x => x.RequestNumber));
    }

    // ---------- פונקציות עזר ----------

    // יוצר Repository על DB בזיכרון, ממולא בבקשות שקיבלנו.
    // שם ייחודי (Guid) לכל בדיקה, כדי שהבדיקות לא ישפיעו זו על זו.
    private static RequestRepository CreateRepository(params Request[] requests)
    {
        var options = new DbContextOptionsBuilder<RequestsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new RequestsDbContext(options);
        db.Requests.AddRange(requests);
        db.SaveChanges();

        return new RequestRepository(db);
    }

    // יוצר בקשה לבדיקה, עם ערכי ברירת מחדל סבירים. מציינים רק מה שחשוב לבדיקה.
    private static Request Make(
        int id,
        int owner,
        int? assigned,
        RequestStatus status = RequestStatus.New,
        string? number = null,
        DateTime? createdAt = null)
        => new()
        {
            Id = id,
            RequestNumber = number ?? $"REQ-{id:000}",
            CustomerId = 1,
            OwnerId = owner,
            AssignedToUserId = assigned,
            Status = status,
            RequestType = RequestType.General,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    // יוצר קריטריוני חיפוש עם ברירות מחדל. כל בדיקה מציינת רק מה שמשתנה אצלה.
    private static RequestSearchCriteria Criteria(
        int userId = 1,
        bool admin = false,
        string? number = null,
        RequestStatus[]? statuses = null,
        DateTime? from = null,
        DateTime? toExclusive = null,
        RequestSortField sortBy = RequestSortField.CreatedAt,
        SortDirection direction = SortDirection.Desc,
        int page = 1,
        int pageSize = 20)
        => new(
            number,
            statuses ?? Array.Empty<RequestStatus>(),
            null,
            from,
            toExclusive,
            sortBy,
            direction,
            page,
            pageSize,
            userId,
            admin);
}