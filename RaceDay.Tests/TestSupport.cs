using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

/// <summary>Boots the real API in memory, swapping SQL Server for an isolated in-memory database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class Api
{
    public const string OrganiserCode = "RACEDAY-ORG-2026";
    public const string Password = "StrongPass123!";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<HttpResponseMessage> Post<T>(this HttpClient c, string url, T body) => c.PostAsJsonAsync(url, body, Json);
    public static Task<HttpResponseMessage> Put<T>(this HttpClient c, string url, T body) => c.PutAsJsonAsync(url, body, Json);
    public static Task<T?> Read<T>(this HttpResponseMessage r) => r.Content.ReadFromJsonAsync<T>(Json);

    public static RegisterRequest NewRegistration(UserRole role, string? email = null) => new()
    {
        FirstName = "Test", LastName = role.ToString(),
        Email = email ?? $"{Guid.NewGuid():N}@raceday.test",
        Password = Password, Role = role,
        OrganiserCode = role == UserRole.Organiser ? OrganiserCode : null
    };

    /// <summary>Registers a new user, logs in, and returns a client holding that user's session cookie.</summary>
    public static async Task<(HttpClient Client, UserDto User)> LoginAs(ApiFactory f, UserRole role)
    {
        var client = f.CreateClient();   // each client has its own cookie jar = its own session
        var reg = NewRegistration(role);
        (await client.Post("/api/auth/register", reg)).EnsureSuccessStatusCode();
        var login = await client.Post("/api/auth/login", new LoginRequest { Email = reg.Email, Password = Password });
        login.EnsureSuccessStatusCode();
        return (client, (await login.Read<UserDto>())!);
    }

    public static EventRequest NewEvent(string name = "City 10K") => new()
    {
        Name = name, Description = "Road race through the city", Date = DateTime.UtcNow.AddDays(30),
        Location = "Johannesburg", DistanceKm = 10, EventType = EventType.Run
    };

    public static async Task<EventDto> CreateEvent(HttpClient organiser, string name = "City 10K")
    {
        var r = await organiser.Post("/api/events", NewEvent(name));
        r.EnsureSuccessStatusCode();
        return (await r.Read<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategory(HttpClient organiser, int eventId, string name = "Senior")
    {
        var r = await organiser.Post($"/api/events/{eventId}/categories", new CategoryRequest { Name = name });
        r.EnsureSuccessStatusCode();
        return (await r.Read<CategoryDto>())!;
    }
}
