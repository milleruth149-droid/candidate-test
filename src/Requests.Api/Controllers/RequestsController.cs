using Microsoft.AspNetCore.Mvc;
using Requests.Application.Requests;

namespace Requests.Api.Controllers;

// ApiController = ASP.NET בודק אוטומטית את הקלט, ומחזיר 400 אם הוא שבור לגמרי.
[ApiController]
// הכתובת: api/requests
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _service;

    public RequestsController(IRequestService service)
    {
        _service = service;
    }

    // GET api/requests?status=1&status=2&page=1&pageSize=20 ...
    // לצורך המבחן, המשתמש הנוכחי מגיע דרך headers:
    //   X-User-Id: מספר המשתמש
    //   X-Is-Admin: true או false
    // כשמגדירים אותם כפרמטרים כאן, הם גם מופיעים ב-Swagger ואפשר למלא אותם שם.
    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestDto>>> Get(
        // FromQuery = ה-Request נבנה מהפרמטרים שבכתובת.
        [FromQuery] RequestSearchRequest request,
        // FromHeader = הערך נלקח מה-header.
        [FromHeader(Name = "X-User-Id")] int? userId,
        [FromHeader(Name = "X-Is-Admin")] bool? isAdmin,
        CancellationToken cancellationToken)
    {
        // אם אין מספר משתמש תקין, לא מנחשים (הקוד הישן הניח "משתמש 1" בשקט).
        // מחזירים 401 = "לא מזוהה".
        if (userId is null or < 1)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                detail: "X-User-Id header is required and must be a positive integer.");
        }

        try
        {
            // isAdmin ?? false = אם לא נשלח, המשתמש הוא לא מנהל.
            var result = await _service.SearchAsync(
                request,
                userId.Value,
                isAdmin ?? false,
                cancellationToken);

            // 200 + התוצאה.
            return Ok(result);
        }
        catch (RequestValidationException ex)
        {
            // ה-Service מצא קלט לא חוקי. מחזירים 400 עם רשימת השגיאות.
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["request"] = ex.Errors.ToArray()
                }));
        }
    }
}