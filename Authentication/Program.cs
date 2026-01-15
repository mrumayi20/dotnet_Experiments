using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// 1. Define the "Rules" for our Passport (JWT) validation
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "my-api",
            ValidAudience = "my-users",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyMustBeVeryLong1234!"))
        };
    });

//2. we register this service so that we can use authentication and authorization middlewares.
builder.Services.AddAuthorization();

var app = builder.Build();

// 3. The Middleware Pipeline (Order is critical!)
app.UseAuthentication(); // "Who are you?"
app.UseAuthorization();  // "Are you allowed?"

/*  Custome Middleware for API Key Authentication
// app.UseMiddleware is for your own custom code, while app.UseAuthentication is for standard .NET built-in security.
app.UseMiddleware<Authentication.ApiKeyMiddleware>();
*/

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// 4. A Protected Endpoint, RequireAuthorization() make this endpoint protected by JWT
//meaning I need JWT token to access it.
app.MapGet("/protected", () => "You accessed a JWT protected area!")
   .RequireAuthorization();


app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

//This logic creates the Token (Token Generator)
//In a real app, you'd have a Login Controller. For this experiment
app.MapGet("/login", () =>
{
    var claims = new[] { new Claim(ClaimTypes.Name, "YourName") };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSecretKeyMustBeVeryLong1234!"));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "my-api",
        audience: "my-users",
        claims: claims,
        expires: DateTime.Now.AddMinutes(30),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
