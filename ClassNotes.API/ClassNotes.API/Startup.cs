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

        // ----------------- CG -----------------
        // Contexto de la base de datos
        services.AddDbContext<ClassNotesContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

		// Servicios personalizados
		services.AddTransient<IActivitiesService, ActivitiesService>();
		services.AddTransient<IAttendancesService, AttendancesService>();
		services.AddTransient<ICentersService, CentersService>();
		services.AddTransient<ICourseNotesService, CourseNotesService>();
		services.AddTransient<ICourseSettingsService, CourseSettingsService>();
		services.AddTransient<ICoursesService, CoursesService>();
		services.AddTransient<IStudentsService, StudentsService>();

		// Servicios de seguridad
		services.AddTransient<IAuditService, AuditService>();
		services.AddTransient<IAuthService, AuthService>();

		// Servicio para el envio de correos (SMTP)
		services.AddTransient<IEmailsService, EmailsService>();
		services.AddTransient<IOtpService, OtpService>();

		// Servicio de AutoMapper
		services.AddAutoMapper(typeof(AutoMapperProfile));

		// Habilitar cache en memoria
		services.AddMemoryCache();

		// Identity 
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

		// ----------------- CG  -----------------
	}

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if(env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors("CorsPolicy");

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
