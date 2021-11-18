using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DynDNS Updater API", Version = "v1" });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();

if ((app.Configuration["path-base"] ?? app.Configuration["PathBase"]) is string pathBase)
    app.UsePathBase(pathBase);
app.Use(async (context, next) =>
{
    bool isMatch = false;
    var originalPath = context.Request.Path;
    var originalPathBase = context.Request.PathBase;

    if (context.Request.Headers.TryGetValue("X-Forwarded-Path", out StringValues values) && values.Count > 0)
    {
        foreach (var path in values)
        {
            if (context.Request.Path.StartsWithSegments("/" + path.Trim('/'), out var matched, out var remaining))
            {
                isMatch = true;
                context.Request.Path = remaining;
                context.Request.PathBase = context.Request.PathBase.Add(matched);
                break;
            }
        }
    }

    try
    {
        await next();
    }
    finally
    {
        if (isMatch)
        {
            context.Request.Path = originalPath;
            context.Request.PathBase = originalPathBase;
        }
    }
});
app.UseForwardedHeaders();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "";
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DynDNS Updater API V1");
});

app.Run();
