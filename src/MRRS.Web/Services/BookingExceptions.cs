namespace MRRS.Web.Services;

public class BookingConflictException : Exception
{
    public BookingConflictException(string message) : base(message)
    {
    }
}

public class InvalidBookingException : Exception
{
    public InvalidBookingException(string message) : base(message)
    {
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
