using MilkHisab.Api.Models;

namespace MilkHisab.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Customers.Any()) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var c1 = new Customer
        {
            Name = "Ramesh Patel",
            Phone = "9876543210",
            Address = "Near Temple, Lane 2",
            DefaultRate = 60m,
            IsActive = true
        };
        var c2 = new Customer
        {
            Name = "Sita Shah",
            Phone = "9123456780",
            Address = "Society B-12",
            DefaultRate = 58m,
            IsActive = true
        };
        var c3 = new Customer
        {
            Name = "Imran Khan",
            Phone = "9988776655",
            Address = "Shop Road",
            DefaultRate = 62m,
            IsActive = true
        };

        db.Customers.AddRange(c1, c2, c3);
        db.SaveChanges();

        var entries = new List<DailyEntry>();
        for (var d = monthStart; d <= today; d = d.AddDays(1))
        {
            // skip Sundays for demo variety on customer 2
            var skipC2 = d.DayOfWeek == DayOfWeek.Sunday;
            entries.Add(new DailyEntry
            {
                CustomerId = c1.Id,
                Date = d,
                QuantityLitres = 1.5m,
                Rate = c1.DefaultRate,
                Note = null
            });
            if (!skipC2)
            {
                entries.Add(new DailyEntry
                {
                    CustomerId = c2.Id,
                    Date = d,
                    QuantityLitres = 1.0m,
                    Rate = c2.DefaultRate
                });
            }
            if (d.Day % 2 == 1)
            {
                entries.Add(new DailyEntry
                {
                    CustomerId = c3.Id,
                    Date = d,
                    QuantityLitres = 2.0m,
                    Rate = c3.DefaultRate,
                    Note = "Extra morning"
                });
            }
        }

        db.DailyEntries.AddRange(entries);
        db.SaveChanges();

        var pay1 = new Payment
        {
            CustomerId = c1.Id,
            Amount = 500m,
            Mode = "cash",
            PaidOn = today.AddDays(-2),
            Note = "Partial month payment",
            ReceiptNumber = $"MH-{today:yyyyMM}-0001"
        };
        var pay2 = new Payment
        {
            CustomerId = c2.Id,
            Amount = 300m,
            Mode = "upi",
            PaidOn = today.AddDays(-1),
            Note = "UPI received offline — recorded manually",
            ReceiptNumber = $"MH-{today:yyyyMM}-0002"
        };
        db.Payments.AddRange(pay1, pay2);
        db.SaveChanges();
    }
}
