using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

        // ----------------- CG  -----------------
        //DbContext
        //        services.AddDbContext<NOMBRECONTEXT>(options =>
        //options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        //servicios de interfaces 

        //Identity (falta que agregar la entidad de usuario para que pueda implementarse)
        //    services.AddIdentity<UserEntity, IdentityRole>(options =>
        //    {
        //        options.SignIn.RequireConfirmedAccount = false;
        //    }).AddEntityFrameworkStores<ApplicaContext>()
        //.AddDefaultTokenProviders();

        //    services.AddAuthentication(options =>
        //    {
        //        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        //    }).AddJwtBearer(options =>
        //    {
        //        options.SaveToken = true;
        //        options.RequireHttpsMetadata = false;
        //        options.TokenValidationParameters = new TokenValidationParameters
        //        {
        //            ValidateIssuer = true,
        //            ValidateAudience = false,
        //            ValidAudience = Configuration["JWT:ValidAudience"],
        //            ValidIssuer = Configuration["JWT:ValidIssuer"],
        //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"]))    //llave 
        //        };
        //    });

        //Automapper (falta que definir el AutoMapperProfile)
        //services.AddAutoMapper(typeof(AutoMapperProfile));

        //Cors
        //services.AddCors(opt =>
        //{
        //    var allowURLS = Configuration.GetSection("AllowURLS").Get<string[]>();

        //    opt.AddPolicy("CorsPolicy", builder => builder
        //    .WithOrigins(allowURLS)
        //    .AllowAnyMethod()
        //    .AllowAnyHeader()
        //    .AllowCredentials());
        //});

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

        //useCors
        //app.UseCors("CorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
