using Microsoft.EntityFrameworkCore;
using Requests.Domain.Entities;

namespace Requests.Infrastructure.Persistence;

public class RequestsDbContext : DbContext
{
    public RequestsDbContext(DbContextOptions<RequestsDbContext> options) : base(options)
    {
    }

    // הטבלה של הבקשות.
    public DbSet<Request> Requests => Set<Request>();

    // כאן מגדירים את מבנה הטבלה, כולל אינדקסים.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Request>(entity =>
        {
            // חיפוש "מתחיל ב-" לפי מספר בקשה. האינדקס הזה מאפשר ל-DB למצוא אותו בלי לסרוק הכל.
            entity.HasIndex(x => x.RequestNumber);

            // הרשאות: משתמש רגיל מסונן לפי בעלות או הקצאה, ולכן שניהם צריכים אינדקס.
            entity.HasIndex(x => x.OwnerId);
            entity.HasIndex(x => x.AssignedToUserId);

            // טווח תאריכים ומיון ברירת המחדל (תאריך יצירה).
            entity.HasIndex(x => x.CreatedAt);

            // סינון לפי סטטוס, הפילטר הנפוץ ביותר.
            entity.HasIndex(x => x.Status);
        });
    }
}