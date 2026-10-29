namespace MRRS.Web.Services;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "MRRS Room Reservations";
}

public enum SmtpSecurity
{
    None = 0,
    StartTls = 1,
    SslOnConnect = 2
}
