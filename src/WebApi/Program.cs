using ApplicationCore.Automapper;
using ApplicationCore.Constants;
//INTERFACE REPOSITORIES
using ApplicationCore.Interfaces.Repositories.Cache;
using ApplicationCore.Interfaces.Repositories.CustomerType;
using ApplicationCore.Interfaces.Repositories.Customer;


//INTERFACE SERVICES
using ApplicationCore.Interfaces.Services;
using ApplicationCore.Interfaces.Services.CustomerType;
using ApplicationCore.Interfaces.Services.Customer;

using ApplicationCore.Services.Customer;
using ApplicationCore.Services.CustomerType;
//APPCORE SERVICES
using ApplicationCore.Services.HTTP;
using Infrastructure.Repositories.Cache.Redis;
using Infrastructure.Repositories.CustomerType;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Shared.Middelwares;
using System.IO.Compression;
using System.Text;
using Infrastructure.Repositories.Customer;
using ApplicationCore.Interfaces.Repositories.Products;
using Infrastructure.Repositories.Products;
using ApplicationCore.Services.ContactTypeService;
using ApplicationCore.Interfaces.Services.ContactType;
using ApplicationCore.Interfaces.Repositories.ContactTypeRepository;
using Infrastructure.Repositories.ContactTypeRepository;
using ApplicationCore.Interfaces.Services.Country;
using ApplicationCore.Services.CountryService;
using ApplicationCore.Interfaces.Repositories.Country;
using Infrastructure.Repositories.Country;
using ApplicationCore.Interfaces.Services.DocumentType;
using ApplicationCore.Services.DocumentType;
using ApplicationCore.Interfaces.Repositories.DocumentType;
using Infrastructure.Repositories.DocumentType;
using ApplicationCore.Interfaces.Services.Department;
using ApplicationCore.Services.Department;
using Infrastructure.Repositories.DepartmentRepository;
using ApplicationCore.Interfaces.Repositories.Department;
using ApplicationCore.Interfaces.Services.PaymentMethod;
using ApplicationCore.Interfaces.Repositories.PaymentMethod;
using ApplicationCore.Services.PaymentMethod;
using Infrastructure.Repositories.PaymentMethod;
using ApplicationCore.Interfaces.Services.ProductCategory;
using ApplicationCore.Interfaces.Repositories.ProductCategory;
using ApplicationCore.Services.ProductCategory;
using Infrastructure.Repositories.ProductCategory;
using ApplicationCore.Services.InvoiceIndicator;
using Infrastructure.Repositories.InvoiceIndicator;
using ApplicationCore.Interfaces.Repositories.InvoiceIndicator;
using ApplicationCore.Interfaces.Services.InvoiceIndicator;
using ApplicationCore.Interfaces.Repositories.VoucherType;
using Infrastructure.Repositories.VoucherType;
using ApplicationCore.Services.VoucherType;
using ApplicationCore.Interfaces.Services.VoucherType;
using ApplicationCore.Interfaces.Services.ContactDetail;
using ApplicationCore.Services;
using Infrastructure.Repositories;
using ApplicationCore.Interfaces.Repositories.ContactDetail;
using Infrastructure.Repositories.Supplier;
using ApplicationCore.Interfaces.Repositories.Supplier;
using ApplicationCore.Interfaces.Services.Supplier;
using ApplicationCore.Services.Supplier;
using EFactura.Application.Common.Context;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Context;
using WebApi.CrossCutting.Correlation;
using WebApi.CrossCutting.Errors;


var builder = WebApplication.CreateBuilder(args);

#region Controllers

builder.Services.AddControllers(options =>
              options.Filters.Add(new ApiGlobalExceptionHandlerAttribute()));

builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
);

builder.Services.AddEndpointsApiExplorer();

#endregion Controllers

#region API v1 Cross-cutting Context and Authorization

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorContextAccessor, HttpActorContextAccessor>();
builder.Services.AddScoped<ICorrelationContextAccessor, HttpCorrelationContextAccessor>();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, V1AuthorizationMiddlewareResultHandler>();

#endregion API v1 Cross-cutting Context and Authorization

#region JWT

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var key = builder.Configuration["Jwt:Key"];
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

#endregion JWT

#region Cors

builder.Services.AddCors();

#endregion Cors

#region Swagger

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = builder.Configuration["App:Name"], Version = builder.Configuration["App:Version"] });
    var securitySchema = new OpenApiSecurityScheme
    {
        Description = "Using the Authorization header with the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securitySchema);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } });
});

#endregion Swagger

#region Serilog

var telemetryConfiguration = TelemetryConfiguration
    .CreateDefault();

telemetryConfiguration.InstrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"];

var logger = new LoggerConfiguration()
  .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
  .Enrich.FromLogContext()
  .WriteTo.Async(f => f.File("Logs/webapi-.log", outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} {Level:u3}] {Username} {Message:lj}{NewLine}{Exception}", rollingInterval: RollingInterval.Day))
  .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces)
  .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

#endregion Serilog

#region ApplicationInsights

builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:InstrumentationKey"]);

#endregion ApplicationInsights

#region RequestCompression

builder.Services.AddResponseCompression();
builder.Services.Configure<GzipCompressionProviderOptions>(opt =>
{
    opt.Level = CompressionLevel.Fastest;
});

#endregion RequestCompression

#region Redis

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisCache");
});

#endregion Redis

#region Automapper

builder.Services.AddAutoMapper(typeof(AutomapperProfiles));

#endregion Automapper

#region Service and Repository Dependency Injection


// JWT


// EF Context
builder.Services.AddDbContext<Infrastructure.DataBase.Context.DBContext>((DbContextOptionsBuilder options) =>
        options.UseNpgsql(builder.Configuration.GetConnectionString(AppConstants.NOMBRE_CADENA_CONEXION)));

// builder.Services.AddSingleton(new BlobServiceClient(builder.Configuration.GetConnectionString("BlobStorage")));

builder.Services.AddScoped<IHTTPService, HTTPService>();
builder.Services.AddSingleton<ICacheService, RedisDistributedCacheService>();


// CustomerTypes
builder.Services.AddScoped<ICustomerTypeService, CustomerTypeService>();
builder.Services.AddScoped<ICustomerTypeRepository, CustomerTypeRepository>();

// ContactTypes
builder.Services.AddScoped<IContactTypeService, ContactTypeService>();
builder.Services.AddScoped<IContactTypeRepository, ContactTypeRepository>();

// ContactDetail
builder.Services.AddScoped<IContactDetailService, ContactDetailService>();
builder.Services.AddScoped<IContactDetailRepository, ContactDetailRepository>();

// Country
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();

// Customer
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// Country
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

// Country
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();

// InvoiceIndicator
builder.Services.AddScoped<IInvoiceIndicatorService, InvoiceIndicatorService>();
builder.Services.AddScoped<IInvoiceIndicatorRepository, InvoiceIndicatorRepository>();

// Products
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();

// Products
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();

// DocumentType
builder.Services.AddScoped<IDocumentTypeService, DocumentTypeService>();
builder.Services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();

// DocumentType
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();

// DocumentType
builder.Services.AddScoped<IVoucherTypeService, VoucherTypeService>();
builder.Services.AddScoped<IVoucherTypeRepository, VoucherTypeRepository>();
#endregion Service and Repository Dependency Injection

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", builder.Configuration["App:Name"]));
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<V1ProblemDetailsMiddleware>();

app.UseHttpsRedirection();

app.UseCors(c => c
              .AllowAnyOrigin()
              .AllowAnyMethod()
              .SetIsOriginAllowed((host) => true)
              .AllowAnyHeader());

app.UseAuthentication();
app.UseMiddleware<ActorLogContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();


