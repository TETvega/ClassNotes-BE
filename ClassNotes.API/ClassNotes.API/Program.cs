using ClassNotes.API;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//JA: Configuramos Serilog desde appsettings.json para saber donde se almacenaran
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()  // Enriquecer los logs con el contexto
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    ////JA: Registra el middleware de logging de solicitud
    //app.UseMiddleware<RequestLoggingMiddleware>();

    startup.Configure(app, app.Environment);

    // AM: using para cargar la data del seeder
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        try
        {
            var context = services.GetRequiredService<ClassNotesContext>();
            var userManager = services.GetRequiredService<UserManager<UserEntity>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await ClassNotesSeeder.LoadDataAsync(context, loggerFactory, userManager, roleManager);
        }
        catch (Exception e)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(e, "Error al ejecutar el Seed de datos");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al arrancar");
}
finally
{
    //JA: Es importante para asegurarse de que todos los logs pendientes se escriban en MongoDB
    Log.CloseAndFlush();
}
