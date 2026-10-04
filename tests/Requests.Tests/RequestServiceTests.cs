using Requests.Application.Requests;
using Requests.Domain.Entities;
using Xunit;

namespace Requests.Tests;

public class RequestServiceTests
{
    // [Fact] = "זו בדיקה". xUnit מזהה אותה ומריץ אותה.

    [Fact]
    public async Task Search_PageSizeTooLarge_ThrowsValidationError()
    {
        // Arrange (הכנה): יוצרים Repository מזויף ו-Service.
        var repository = new FakeRequestRepository();
        var service = new RequestService(repository);

        // Act + Assert (פעולה ובדיקה): מצפים שתיזרק שגיאת ולידציה.
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.SearchAsync(
                new RequestSearchRequest { PageSize = 1000 },   // יותר מהתקרה (100)
                currentUserId: 1,
                isAdministrator: false));

        // וודאי שההודעה מזכירה את pageSize.
        Assert.Contains(ex.Errors, e => e.Contains("pageSize"));
    }

    [Fact]
    public async Task Search_FromDateAfterToDate_ThrowsValidationError()
    {
        var service = new RequestService(new FakeRequestRepository());

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.SearchAsync(
                new RequestSearchRequest
                {
                    CreatedFrom = new DateOnly(2026, 6, 30),
                    CreatedTo = new DateOnly(2026, 1, 1)    // לפני תאריך ההתחלה
                },
                currentUserId: 1,
                isAdministrator: false));

        Assert.Contains(ex.Errors, e => e.Contains("createdFrom"));
    }

    [Fact]
    public async Task Search_UnknownStatusValue_ThrowsValidationError()
    {
        var service = new RequestService(new FakeRequestRepository());

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.SearchAsync(
                // 99 הוא מספר שלא קיים ב-RequestStatus.
                new RequestSearchRequest { Status = new List<RequestStatus> { (RequestStatus)99 } },
                currentUserId: 1,
                isAdministrator: false));

        Assert.Contains(ex.Errors, e => e.Contains("status"));
    }

    [Fact]
    public async Task Search_MultipleProblems_ReturnsAllErrorsTogether()
    {
        var service = new RequestService(new FakeRequestRepository());

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.SearchAsync(
                new RequestSearchRequest { Page = 0, PageSize = 0 },   // שתי בעיות
                currentUserId: 1,
                isAdministrator: false));

        // המשתמש רואה את שתי השגיאות בבת אחת.
        Assert.Equal(2, ex.Errors.Count);
    }

    [Fact]
    public async Task Search_CreatedTo_IncludesWholeDay()
    {
        var repository = new FakeRequestRepository();
        var service = new RequestService(repository);

        await service.SearchAsync(
            new RequestSearchRequest { CreatedTo = new DateOnly(2026, 6, 30) },
            currentUserId: 1,
            isAdministrator: false);

        // "עד 30.6" צריך להפוך ל"קטן מ-1.7 בחצות", כדי שכל יום 30.6 ייכלל.
        Assert.Equal(
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            repository.LastCriteria!.CreatedToUtcExclusive);
    }

    [Fact]
    public async Task Search_UserIdentityComesFromServer_NotFromRequest()
    {
        var repository = new FakeRequestRepository();
        var service = new RequestService(repository);

        await service.SearchAsync(
            new RequestSearchRequest(),
            currentUserId: 7,
            isAdministrator: false);

        // המשתמש וההרשאה שהועברו ל-Repository הם אלה שה-Controller נתן.
        Assert.Equal(7, repository.LastCriteria!.CurrentUserId);
        Assert.False(repository.LastCriteria.IsAdministrator);
    }

    // Repository מזויף: לא מדבר עם DB, רק שומר את מה שקיבל כדי שנוכל לבדוק אותו.
    private sealed class FakeRequestRepository : IRequestRepository
    {
        // הקריטריונים האחרונים שה-Service העביר לנו.
        public RequestSearchCriteria? LastCriteria { get; private set; }

        public Task<List<Request>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<Request>());

        public Task<PagedResult<RequestDto>> SearchAsync(
            RequestSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult(new PagedResult<RequestDto>(
                new List<RequestDto>(), criteria.Page, criteria.PageSize, 0));
        }
    }
}