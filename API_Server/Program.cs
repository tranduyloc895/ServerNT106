using API_Server.Models;
using MongoDB.Driver;
using API_Server.Services;
using API_Server.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using NetStudy.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add service
builder.Services.AddSingleton<EmailService>();

builder.Services.AddSingleton<MongoDbService>();

builder.Services.AddSingleton<JwtService>();

builder.Services.AddSingleton<UserService>();

builder.Services.AddSingleton<GroupService>();

builder.Services.AddSingleton<SingleChatService>();

builder.Services.AddSingleton<GroupChatMessageService>();

builder.Services.AddSingleton<QuestionService>();

builder.Services.AddSingleton<ChatBotService>();

builder.Services.AddSingleton<ImageService>();

builder.Services.AddSingleton<RsaService>();

builder.Services.AddSingleton<AesService>();

builder.Services.AddSingleton<HybridEncryptionService>();

builder.Services.AddSingleton<TaskService>();

builder.Services.AddSingleton<DocumentService>();

builder.Services.AddHttpClient();

// Add services to the container.

builder.Services.AddSignalR()
    .AddHubOptions<GroupChatHub>(options =>
    {
        options.EnableDetailedErrors = true;
    }
);
builder.Services.AddSignalR()
    .AddHubOptions<ChatHub>(options =>
    {
        options.EnableDetailedErrors = true;
    }
);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = builder.Configuration["JwtSettings:Secret"] ?? throw new ArgumentNullException("JwtSettings:Secret cannot be null");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer( options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Headers["accessToken"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/groupChatHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
       
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Headers["accessToken"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }

    };
});



builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.UseCors(builder =>
{
    builder.AllowAnyHeader()
           .AllowAnyMethod()
           .SetIsOriginAllowed(origin => true)
           .AllowCredentials();
});

app.MapHub<ChatHub>("/chatHub");
app.MapHub<GroupChatHub>("/groupChatHub");

app.Run();