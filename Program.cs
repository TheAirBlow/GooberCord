using System.Reflection;
using GooberCord.Server;
using GooberCord.Server.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) 
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console().CreateLogger();

Log.Information("Starting GooberCord Server v1.0");
_ = Controller.Links; // run static constructor
Configuration.Config.Save();

Log.Information("Building ASP.NET application...");
var builder = WebApplication.CreateBuilder();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x => {
        x.RequireHttpsMetadata = false;
        x.TokenValidationParameters = new TokenValidationParameters {
            IssuerSigningKey = JwtHelper.Credentials.Key,
            ValidAudience = JwtHelper.Audience,
            ValidIssuer = JwtHelper.Issuer,
            ValidateAudience = true,
            ValidateIssuer = true
        };
    });
builder.Services.AddAuthorization(x => {
    x.AddPolicy("main", policy => policy.RequireClaim("uuid"));
    x.AddPolicy("begin", policy => policy.RequireClaim("name"));
    x.DefaultPolicy = x.GetPolicy("main")!;
});
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GooberCord API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "JWT Authentication",
        Description = "Enter your JWT token in this field",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }});
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml));
});
builder.Services.AddSerilog();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
        
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/error");
app.MapControllerRoute(name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");
app.UseWebSockets(new WebSocketOptions {
    KeepAliveInterval = TimeSpan.FromSeconds(10)
});
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("v1/swagger.json", "GooberCord API");
});

Log.Information("Initializing Discord bot...");
await Discord.Initialize();

Log.Information("Bot is ready!");
await app.RunAsync();