using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using VibeMe.Microservice.Repository;
using VibeMeMicroservice.Infraestructure;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 1_073_741_824;
    serverOptions.Limits.MinRequestBodyDataRate = null; // Para WebSockets
});

// Configuración estándar
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

// Configuración mejorada de Swagger
builder.Services.AddSwaggerGen();

// Repositorios
builder.Services.AddScoped<IFollowerRepository, FollowerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();


// Base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        NameClaimType = ClaimTypes.NameIdentifier,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // Si la solicitud es para el hub de chat
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",                   // Entorno local
                "https://tu-frontend.vercel.app",          // Producción en Vercel
                "https://*.vercel.app"                     // Todos los subdominios Vercel
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // Solo si usas cookies/auth
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});



// Luego en el middleware:

builder.Services.AddAuthorization();
builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Mantiene la conexión activa
}).AddJsonProtocol(options => {
    options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
var app = builder.Build();

// Middleware para manejar errores 413
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BadHttpRequestException ex) when (ex.StatusCode == 413)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 413;
        await context.Response.WriteAsJsonAsync(new
        {
            Error = "File too large",
            MaxSizeMB = 10000,
            Details = "The maximum allowed file size is 500MB",
            Solution = "Reduce the file size or compress it"
        });
    }
});

// Configuración de la carpeta de uploads
var uploadsPath = Path.Combine(builder.Environment.WebRootPath,
                              builder.Configuration["FileStorage:LocalPath"] ?? "uploads");

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    Console.WriteLine($"Uploads folder created at: {uploadsPath}");
}

// Configuración de archivos estáticos
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".mp4"] = "video/mp4",
            [".mov"] = "video/quicktime",
            [".avi"] = "video/x-msvideo"
        }
    },
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        if (ctx.File.Name.EndsWith(".mp4") || ctx.File.Name.EndsWith(".mov"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=86400");
        }
    }
});

// Configuración del middleware
app.UseRouting();

app.UseCors("DevelopmentPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

// Configuración de Swagger UI para usar /swagger/index.html
    app.UseSwagger();
    app.UseSwaggerUI();


app.MapHub<ChatHub>("/chatHub");
app.MapControllers();
app.Run();