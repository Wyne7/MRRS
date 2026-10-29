namespace MRRS.Web.Services;

public static class BookingOverlap
{
    public static bool Overlaps(DateTime start, DateTime end, DateTime otherStart, DateTime otherEnd) =>
        start < otherEnd && otherStart < end;
}
