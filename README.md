# Spond-Birthday-Calendar

A Blazor Web API that generates an ICS calendar file containing recurring birthday events for all members of a Spond group. The calendar can be subscribed to in any calendar application (Google Calendar, Outlook, Apple Calendar, etc.).

## Features

- 🎂 Automatically fetches birthdays from a Spond group
- 📅 Generates ICS calendar with recurring yearly birthday events
- ⚙️ Configurable via appsettings.json
- 🔄 Calendar is regenerated fresh on every request
- 🔒 Secure configuration management

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A Spond account with access to at least one group
- Spond Group ID (can be found in the Spond app)

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/ArizonaGreenTea05/Spond-Birthday-Calendar.git
   cd Spond-Birthday-Calendar
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

## Configuration

Edit the `appsettings.json` file and configure your Spond credentials:

```json
{
  "Spond": {
    "Username": "your-email@example.com",
    "Password": "your-password",
    "GroupId": "your-group-id",
    "CalendarPath": "/calendar.ics"
  }
}
```

### Configuration Parameters

- **Username**: Your Spond email address
- **Password**: Your Spond password
- **GroupId**: The ID of the Spond group containing the members whose birthdays you want to track
- **CalendarPath**: (Optional) The URL path where the calendar will be accessible. Default: `/calendar.ics`

### Security Note

⚠️ **Never commit your actual credentials to version control!** 

For production use, consider using:
- Environment variables
- Azure Key Vault
- User secrets for development: `dotnet user-secrets set "Spond:Password" "your-password"`

## Usage

### Running the Application

1. Start the application:
   ```bash
   dotnet run
   ```

2. The API will start on `http://localhost:5000` (or the port specified in launchSettings.json)

3. Access the calendar at: `http://localhost:5000/calendar.ics`

### Subscribing to the Calendar

Once the application is running and accessible via a public URL:

#### Google Calendar
1. Open Google Calendar
2. Click the "+" next to "Other calendars"
3. Select "From URL"
4. Enter your calendar URL: `http://your-server/calendar.ics`
5. Click "Add calendar"

#### Apple Calendar
1. Open Calendar app
2. Go to File → New Calendar Subscription
3. Enter your calendar URL: `http://your-server/calendar.ics`
4. Click Subscribe

#### Outlook
1. Open Outlook
2. Go to Calendar
3. Click "Add calendar" → "Subscribe from web"
4. Enter your calendar URL: `http://your-server/calendar.ics`
5. Click Import

## Deployment

### Docker (Recommended)

Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SpondBirthdayCalendar.csproj", "./"]
RUN dotnet restore "SpondBirthdayCalendar.csproj"
COPY . .
RUN dotnet build "SpondBirthdayCalendar.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpondBirthdayCalendar.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpondBirthdayCalendar.dll"]
```

Build and run:
```bash
docker build -t spond-birthday-calendar .
docker run -d -p 5000:80 \
  -e Spond__Username="your-email@example.com" \
  -e Spond__Password="your-password" \
  -e Spond__GroupId="your-group-id" \
  spond-birthday-calendar
```

### Azure App Service / Other Cloud Providers

Configure the application settings in your cloud provider's environment variables using the same keys from `appsettings.json`.

## How It Works

1. When the calendar endpoint is accessed, the API authenticates with Spond using the configured credentials
2. It fetches the specified group and all its members
3. For each member with a birthday, it creates a recurring yearly event
4. The calendar is serialized to ICS format and returned to the client
5. Each request generates a fresh calendar with the latest data from Spond

## Troubleshooting

### Authentication Issues
- Verify your Spond credentials are correct
- Check that your account has access to the specified group

### Group Not Found
- Ensure the GroupId is correct
- Check that the group exists and you have access to it

### Calendar Not Updating
- Most calendar applications cache subscribed calendars
- Refresh intervals vary by application (typically 1-24 hours)
- You can manually refresh in most calendar apps

## Dependencies

- [Spond.API](https://www.nuget.org/packages/Spond.API/) - Spond API client
- [Ical.Net](https://www.nuget.org/packages/Ical.Net/) - ICS calendar generation

## License

This project is licensed under the terms specified in the LICENSE file.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
