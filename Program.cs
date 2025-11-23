using System.Text.Json;
using CartService.Endpoints;
using CartService.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddStackExchangeRedisCache(options =>
{
    // Load from .env file (safe, not committed)
    options.Configuration = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.MapCartEndpoints();

app.Run();
