using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Infrastructure;
using RestaurantSystem.Application.Restaurants;
using RestaurantSystem.Application.Users;
using RestaurantSystem.Domain.Common.Interfaces;
using RestaurantSystem.Infrastructure.Services;
using AutoMapper;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(typeof(RestaurantProfile).Assembly);
builder.Services.AddScoped<RestaurantService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
