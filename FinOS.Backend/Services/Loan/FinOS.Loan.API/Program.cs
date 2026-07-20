using System.Text;
using FinOS.Common.Extensions;
using FinOS.Common.Middleware;
using FinOS.Loan.Application.Commands;
using FinOS.Loan.Application.Validators;
using FinOS.Loan.Infrastructure.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ADO.NET Data Access (replaces EF Core)
builder.Services.AddFinOSDataAccess();
builder.Services.AddInfrastructureServices(builder.Configuration);

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateLoanCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateLoanRequestValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "CHANGE_ME_JWT_SECRET_MINIMUM_32_CHARACTERS";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => {
        o.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinOS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FinOS",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(o => o.AddPolicy("FinOSCors", p => {
    p.WithOrigins("http://localhost:5173","http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinOS Loan API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Description="JWT Authorization", Name="Authorization", In=ParameterLocation.Header, Type=SecuritySchemeType.ApiKey, Scheme="Bearer" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type=ReferenceType.SecurityScheme, Id="Bearer" } }, Array.Empty<string>() } });
});

builder.Services.AddControllers();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("FinOSCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) {
        if (_validators.Any()) {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)))).SelectMany(r => r.Errors).Where(f => f != null).ToList();
            if (failures.Count != 0) throw new FinOS.Common.Exceptions.ValidationException(failures.GroupBy(f=>f.PropertyName).ToDictionary(g=>g.Key, g=>g.Select(f=>f.ErrorMessage).ToArray()));
        }
        return await next();
    }
}
