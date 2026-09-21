using Microsoft.EntityFrameworkCore;

namespace MilkHisab.Api.Data;

public static class PersonalSchema
{
    // Additive upgrade for existing EnsureCreated databases; legacy milk tables are untouched.
    public static void Upgrade(AppDbContext db)
    {
        using var transaction = db.Database.BeginTransaction();
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS LedgerUsers (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, PasswordHash TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_LedgerUsers_Name ON LedgerUsers(Name);
            CREATE TABLE IF NOT EXISTS LedgerProducts (Id INTEGER PRIMARY KEY AUTOINCREMENT, OwnerId INTEGER NOT NULL REFERENCES LedgerUsers(Id), Name TEXT NOT NULL, Unit TEXT NOT NULL, Rate TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Expenses (Id INTEGER PRIMARY KEY AUTOINCREMENT, OwnerId INTEGER NOT NULL REFERENCES LedgerUsers(Id), Date TEXT NOT NULL, Title TEXT NOT NULL, Category TEXT NOT NULL, Supplier TEXT NULL, ProductId INTEGER NULL REFERENCES LedgerProducts(Id), Quantity TEXT NULL, Rate TEXT NULL, Amount TEXT NOT NULL, Note TEXT NULL);
            CREATE TABLE IF NOT EXISTS ExpensePayments (Id INTEGER PRIMARY KEY AUTOINCREMENT, ExpenseId INTEGER NOT NULL REFERENCES Expenses(Id), OwnerId INTEGER NOT NULL REFERENCES LedgerUsers(Id), Date TEXT NOT NULL, Amount TEXT NOT NULL, Mode TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_Expenses_OwnerId_Date ON Expenses(OwnerId, Date);
            CREATE INDEX IF NOT EXISTS IX_ExpensePayments_OwnerId ON ExpensePayments(OwnerId);
            """);
        transaction.Commit();
    }
}
