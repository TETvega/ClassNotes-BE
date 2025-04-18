using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Services.Audit;
using ClassNotes.API.Helpers.Automapper;
using ClassNotes.API.Services.Auth;
using ClassNotes.API.Services.Activities;
using ClassNotes.API.Services.Centers;
using ClassNotes.API.Services.CourseNotes;
using ClassNotes.API.Services.Courses;
using ClassNotes.API.Services.Students;
using ClassNotes.API.Services.Attendances;
using ClassNotes.API.Services.CoursesSettings;
using ClassNotes.API.Services.Emails;
using ClassNotes.API.Services.Otp;
using ClassNotes.API.Services.Users;
using CloudinaryDotNet;
using ClassNotes.API.Services.Cloudinary;
using Microsoft.Extensions.Configuration;
using ClassNotes.API.Services.DashboardHome;
using ClassNotes.API.Services.DashboarCenter;
using ClassNotes.API.Services.TagsActivities;
using ClassNotes.API.Services.DashboardCourses;
using ClassNotes.API.Services.Date;
using ClassNotes.API.Services.Distance;
using ClassNotes.API.Services;
using ClassNotes.API.Services.Notes;
using ClassNotes.API.Services.AllCourses;
using Serilog;
using ClassNotes.API.Services.AttendanceRealTime;
using ClassNotes.API.Hubs;
using ClassNotes.API.BackgroundServices;
using ClassNotes.API.Models;
using System.Collections.Concurrent;
using ClassNotes.API.Services.ConcurrentGroups;
using System.Threading.Channels;
using ClassNotes.Models;


namespace ClassNotes.API;

public class Startup
{
    private readonly IConfiguration Configuration;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHttpContextAccessor();

		// Centro de cargas en Tiempo real
        services.AddSignalR();

        // Contexto de la base de datos
        services.AddDbContext<ClassNotesContext>(options =>
			options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
            x => x.UseNetTopologySuite() // para almacenar datos para geolocalizacion en el formato necesitado
            ));

        // para monitoreo de la cache de las asistencias
        services.AddSingleton<ConcurrentDictionary<Guid, AttendanceGroupCache>>();
        services.AddHostedService<AttendanceExpirationService>();
        services.AddSingleton<IAttendanceGroupCacheManager, AttendanceGroupCacheManager>();

        //Para servicio de enviar reportes en segundo plano, aqui se declara el canal y el servicio...
        var messageQueue = Channel.CreateUnbounded<EmailStudentListRequest>();
        services.AddSingleton(messageQueue); 
        services.AddHostedService<LoggingBackgroundService>();

        // Servicios personalizados
        services.AddTransient<IActivitiesService, ActivitiesService>();
		services.AddTransient<IAttendancesService, AttendancesService>();
		services.AddTransient<INotesService, NotesService>();
		services.AddTransient<ICourseNotesService, CourseNotesService>();
		services.AddTransient<ICourseSettingsService, CourseSettingsService>();
		services.AddTransient<ICoursesService, CoursesService>();
		services.AddTransient<IStudentsService, StudentsService>();
		services.AddTransient<IUsersService, UsersService>();
		services.AddTransient<IDashboardHomeService, DashboardHomeService>();
		services.AddTransient<ITagsActivitiesService, TagsActivitiesService>();
		services.AddTransient<IDashboardCoursesService, DashboardCoursesService>();
		services.AddTransient<ICloudinaryService, CloudinaryService>();
    	services.AddTransient<IDashboardCenterService, DashboardCenterService>();
		services.AddTransient<ICoursesFilterService, CoursesFilterService>();
		services.AddTransient<ICentersService, CentersService>();

        services.AddSingleton<DistanceService>(); //
        services.AddScoped<IEmailAttendanceService, EmailAttendanceService>(); //

        services.AddScoped<QRService>();
        services.AddHostedService<QRService>();
        services.AddSingleton<OTPCleanupService>();
        services.AddHostedService(provider => provider.GetRequiredService<OTPCleanupService>());
        services.AddSingleton<EmailScheduleService>();
        services.AddHostedService<ScheduledEmailSender>();
        services.AddSingleton<IDateTimeService, DateTimeService>();


        //Para Asistencias en Tiepo real
        services.AddTransient<IAttendanceRSignalService, AttendanceRSignalService>();



        // Servicios de seguridad
        services.AddTransient<IAuditService, AuditService>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IOtpService, OtpService>();

        // Servicio para el envio de correos (SMTP)
        services.AddTransient<IEmailsService, EmailsService>();

        // Servicio para la subida de archivos de imagenes en la nube (Cloudinary)
        services.AddTransient<ICloudinaryService, CloudinaryService>();

        // Servicio para el mapeo automático de Entities y DTOs (AutoMapper)
        services.AddAutoMapper(typeof(AutoMapperProfile));

        // Habilitar cache en memoria
        services.AddMemoryCache();

        // Configuración del IdentityUser
        services.AddIdentity<UserEntity, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
        }).AddEntityFrameworkStores<ClassNotesContext>()
          .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidAudience = Configuration["JWT:ValidAudience"],
                ValidIssuer = Configuration["JWT:ValidIssuer"],
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"]))
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/attendance"))// cambiar por la URL al final 
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });


        // CORS Configuration
        services.AddCors(opt =>
        {
            var allowURLS = Configuration.GetSection("AllowURLS").Get<string[]>();

            opt.AddPolicy("CorsPolicy", builder => builder
            .WithOrigins(allowURLS)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseSerilogRequestLogging(); //JA: Log de cada peticion HTTP

        app.UseCors("CorsPolicy");

        app.UseAuthentication();

        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

			// conexion al cual estara directamente FE
            endpoints.MapHub<AttendanceHub>("/hubs/attendance");
        });
    }
}
