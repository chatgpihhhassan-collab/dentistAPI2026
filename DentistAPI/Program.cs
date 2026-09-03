using DentistAPI.Repositories;
using DentistAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Repository and Services
builder.Services.AddHttpClient("", client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<DentalRepository>();
builder.Services.AddScoped<IAIDentalNotesRepository, AIDentalNotesRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();

var useMockSpeech = builder.Configuration.GetValue<bool>("UseMockSpeechToText", false);
if (useMockSpeech)
{
    builder.Services.AddScoped<ISpeechToTextService, MockSpeechToTextService>();
}
else
{
    builder.Services.AddScoped<ISpeechToTextService, GeminiSpeechToTextService>();
}

builder.Services.AddScoped<IGeminiDentalNotesService, GeminiDentalNotesService>();
builder.Services.AddScoped<GeminiDentalNotesService>();

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
