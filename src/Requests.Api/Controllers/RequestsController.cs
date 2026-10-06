using Microsoft.AspNetCore.Mvc;
using Requests.Application.Requests;

namespace Requests.Api.Controllers;

// ApiController = ASP.NET בודק אוטומטית את הקלט, ומחזיר 400 אם הוא שבור לגמרי.
[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _service;

    public RequestsController(IRequestService service)
    {
        _service = service;
    }

   
    [HttpGet("statuses")]
    public async Task<ActionResult<IEnumerable<object>>> GetStatuses(CancellationToken cancellationToken)
    {
        var statuses = await _service.GetStatusesAsync(cancellationToken);
        return Ok(statuses);
    }

   
    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<object>>> GetTypes(CancellationToken cancellationToken)
    {
        var types = await _service.GetTypesAsync(cancellationToken);
        return Ok(types);
    }
//  מחזיר רשימה של בקשות לפי פרמטרי החיפוש, עם תמיכה בדפים.
// ומבצע אימות שהמשתמש מזוהה ותקין דרך ה-Headers.    
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