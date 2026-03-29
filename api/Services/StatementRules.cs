namespace FamilyBudgetApi.Services;

public static class StatementRules
{
    public static (DateTime startDate, DateTime endDate) ParseDateWindow(string startDateRaw, string endDateRaw)
    {
        if (!DateTime.TryParse(startDateRaw, out var startDate))
            throw new ArgumentException("Invalid startDate.");
        if (!DateTime.TryParse(endDateRaw, out var endDate))
            throw new ArgumentException("Invalid endDate.");

        startDate = startDate.Date;
        endDate = endDate.Date;

        if (startDate > endDate)
            throw new ArgumentException("startDate must be less than or equal to endDate.");

        return (startDate, endDate);
    }

    public static double CalculateDelta(double beginningBalance, double selectedTotal, double endingBalance)
        => beginningBalance + selectedTotal - endingBalance;

    public static bool IsZeroDelta(double delta)
        => Math.Abs(delta) < 0.01;
}
