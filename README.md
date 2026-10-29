# MRRS

Meeting Room Reservation System for one organization. Employees reserve and cancel their own rooms. Administrators also manage rooms and people. A successful booking or cancellation sends an email invitation over SMTP, with an iCalendar attachment. There is no Microsoft 365 or Graph integration in this version.

## Stack

- ASP.NET Core MVC on .NET 8, with JSON APIs under `/api` and one MVC page at `/host`
- Angular 19 client using NgModules (`standalone: false`)
- Microsoft SQL Server via Entity Framework Core
- ASP.NET Core Identity, passwords hashed in SQL Server (PBKDF2), cookie sign-in
- MailKit for SMTP

## Layout

```
MRRS.sln
src/MRRS.Web/                 ASP.NET Core MVC host
  Controllers/                Account, rooms, bookings, users, plus the MVC host page
  Data/                       EF Core context, migrations, seed
  Models/                     User, room, booking
  Services/                   Conflict checks and SMTP invitations
  ClientApp/                  Angular NgModule app
tests/MRRS.Web.Tests/         Booking conflict, cancel rules, password hash, SMTP no-op
```

## Seed accounts

Created the first time the app starts against an empty database. Change them before any shared deployment.

| Username | Password     | Role     |
| -------- | ------------ | -------- |
| admin    | Admin123!    | Admin    |
| employee | Employee123! | Employee |

Sample rooms (Cedar Conference, Birch Huddle, Oak Boardroom, Maple Interview) and two upcoming reservations are seeded with those accounts.

## Prerequisites

- .NET 8 SDK
- Node.js 20 or newer (the Angular client is launched by `dotnet run` in Development)
- SQL Server reachable from this machine: LocalDB on Windows, SQL Server on Linux, or Docker

Install the EF tool only if you want to add migrations yourself. The app applies the migrations that are already in the repo.

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

## Database

The development connection string lives in `src/MRRS.Web/appsettings.Development.json`:

```text
Server=localhost,1433;Database=MRRS;User Id=sa;Password=ChangeMe_LocalDev!2026;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true
```

`TrustServerCertificate` is for a local SQL Server that presents a self-signed certificate. Use a real certificate and drop that flag outside local development.

Override it without editing files:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=MRRS;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True"
```

Windows LocalDB:

```text
Server=(localdb)\mssqllocaldb;Database=MRRS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Docker:

```bash
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='ChangeMe_LocalDev!2026' \
  -p 1433:1433 --name mrrs-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

On Linux without systemd, after the `mssql-server` package is installed:

```bash
sudo MSSQL_SA_PASSWORD='ChangeMe_LocalDev!2026' MSSQL_PID=developer \
  /opt/mssql/bin/mssql-conf -n setup accept-eula
sudo -u mssql /opt/mssql/bin/sqlservr
```

You do not need to create the `MRRS` database by hand. On startup the host runs `Migrate()` and then seeds roles, users, rooms, and sample bookings. To apply migrations without starting the site:

```bash
dotnet ef database update --project src/MRRS.Web --startup-project src/MRRS.Web
```

## SMTP

`Smtp` in `src/MRRS.Web/appsettings.json`:

| Setting     | Purpose                                                                 |
| ----------- | ----------------------------------------------------------------------- |
| Host        | SMTP server. Leave empty and no mail is sent.                           |
| Port        | Default `587`.                                                          |
| Security    | `StartTls` (default), `SslOnConnect`, or `None`.                        |
| Username    | Optional. Empty means no SMTP authentication.                           |
| Password    | Optional.                                                               |
| FromAddress | Envelope From. Required once Host is set.                               |
| FromName    | Display name, default `MRRS Room Reservations`.                         |

Example for a submission relay:

```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Security": "StartTls",
  "Username": "mrrs@example.com",
  "Password": "secret",
  "FromAddress": "mrrs@example.com",
  "FromName": "MRRS Room Reservations"
}
```

Environment variables use the same shape: `Smtp__Host`, `Smtp__Port`, `Smtp__Password`, and so on.

When `Host` is empty, creating or cancelling a reservation still succeeds. The API returns `emailSent: false` and `emailWarning` explaining that SMTP is not configured. A failed SMTP connection does not roll back the reservation. The message is a plain-text and HTML body plus a `text/calendar` part (`METHOD:REQUEST` on book, `METHOD:CANCEL` on cancel). Recipients are the organizer and any attendee addresses on the booking.

## Run

From the repository root, with SQL Server already listening:

```bash
dotnet run --project src/MRRS.Web
```

That starts the MVC host on `http://127.0.0.1:43123` and the Angular dev server on `http://127.0.0.1:43124`. Opening the host root redirects to the Angular app. Sign in at `http://127.0.0.1:43124`. The dev server proxies `/api` and `/host` to the MVC host, so the auth cookie stays on the Angular origin.

To run the client by itself (the API must already be up):

```bash
cd src/MRRS.Web/ClientApp
npm install
npm start
```

`npm start` serves `http://127.0.0.1:43124` and proxies `/api` and `/host` to the MVC host.

Sign in as `admin` or `employee`. Administrators see Rooms and People in the side navigation. Employees see the day sheet and their own reservations.

A production publish builds the Angular app into `wwwroot` and serves `index.html` for client routes:

```bash
dotnet publish src/MRRS.Web -c Release
```

Set `ASPNETCORE_ENVIRONMENT=Production` and supply the connection string and SMTP settings through the environment. The app does not force HTTPS. Terminate TLS at a reverse proxy if you need it.

## Tests

```bash
dotnet test
```

The suite covers overlap detection (including back-to-back bookings, which are allowed), rejection of a second hold on the same room, cancellation rules, the Identity password hash, and the SMTP no-op when `Host` is empty.

`dotnet build` builds the host. `npm run build` inside `src/MRRS.Web/ClientApp` builds the Angular app.

## What a booking enforces

- Same room, overlapping times, and status Confirmed is rejected with HTTP 409 and the message `Already booked …`.
- A booking that starts when the previous one ends is allowed.
- The same time in a different room is allowed.
- Start and end must fall on the same calendar day, last at least 15 minutes, and be no longer than 12 hours.
- The end must still be in the future, using the server's local clock.
- Closed rooms cannot be booked. Closing a room keeps its history.
- Employees can cancel only their own future reservations. Administrators can cancel any future reservation.
- The conflict check runs inside a serializable transaction on SQL Server.

Times are organization-local wall-clock values. The client sends `2026-10-30T10:00:00` with no offset, and SQL Server stores that clock time. Run the host in the organization's time zone.

## Assumptions

- One organization, one time zone, cookie sign-in with `SameSite=Lax`. Cross-site POSTs do not include the cookie.
- Usernames and passwords live in SQL Server. This version does not use Entra ID.
- Disabling a person blocks the next sign-in and drops an existing session on the next request.
- At least one active administrator must remain, and you cannot disable your own account.
- Attendee addresses are optional and free-form. The organizer's account email is always a recipient when mail goes out.
- Recurring meetings, approval chains, and Microsoft 365 calendar sync are out of scope.
