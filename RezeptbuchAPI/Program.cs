using Microsoft.EntityFrameworkCore;
using RezeptbuchAPI.Models;
using Microsoft.OpenApi.Models;
using AutoMapper;
using RezeptbuchAPI.Models.DTO;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options =>
    {
        options.OutputFormatters.Insert(0, new Microsoft.AspNetCore.Mvc.Formatters.HttpNoContentOutputFormatter());
        options.OutputFormatters.Insert(0, new Microsoft.AspNetCore.Mvc.Formatters.StreamOutputFormatter());
    })
    .AddXmlSerializerFormatters();
builder.Services.AddDbContext<RecipeBookContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAutoMapper(typeof(RecipeMappingProfile));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rezeptbuch API", Version = "v1" });
    c.AddServer(new OpenApiServer { Url = "http://localhost:5112" });
    c.AddServer(new OpenApiServer { Url = "https://localhost:7042" });
});
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipeBookContext>();
    db.Database.Migrate();
}

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
app.UseSwagger();
app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();