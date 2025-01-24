using ClassNotes.API;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

//using del seeder
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
//    try
//    {
//        var transactionalContext = services.GetRequiredService<CONTEXT>();
//        var userManager = services.GetRequiredService<UserManager<USERENTITY>>();
//        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//        await SEEDER.LoadDataAsync(transactionalContext, userManager, roleManager, loggerFactory);
//    }
//    catch (Exception e)
//    {
//        var logger = loggerFactory.CreateLogger<Program>();
//        logger.LogError(e, "Error al ejecutar el Seed de datos");
//    }
//}

app.Run();
