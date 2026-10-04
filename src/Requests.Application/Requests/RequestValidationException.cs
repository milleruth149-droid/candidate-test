namespace Requests.Application.Requests;

// Exception = "תקלה" שאפשר לזרוק ממקום אחד ולתפוס במקום אחר.
// זו תקלה ייעודית שלנו, בשביל קלט לא חוקי.
// Exception מקבילה לכל סוגי השגיאות ב-.NET, ולכן יורשים ממנה (: Exception).
public sealed class RequestValidationException : Exception
{
    // רשימת כל השגיאות שנמצאו (ולא רק הראשונה).
    // כך המשתמש רואה את כל הבעיות בבת אחת ולא צריך לתקן אחת-אחת.
    public IReadOnlyList<string> Errors { get; }

    // Constructor = הפונקציה שרצה כשיוצרים את האובייקט.
    // מקבלת את רשימת השגיאות ושומרת אותה.
    public RequestValidationException(IReadOnlyList<string> errors)
        // base(...) מעביר הודעה כללית ל-Exception הרגיל של .NET,
        // כדי שגם ביומנים (Logs) תופיע הודעה קריאה.
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}