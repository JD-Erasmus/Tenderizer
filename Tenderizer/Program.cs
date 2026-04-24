using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Services.Implementations;
using Tenderizer.Services.Interfaces;
using Tenderizer.Services.Options;
using Tenderizer.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IReminderScheduler, ReminderScheduler>();
builder.Services.AddScoped<ITenderService, TenderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserLookupService, UserLookupService>();
builder.Services.Configure<DocumentStorageOptions>(builder.Configuration.GetSection("DocumentStorage"));
builder.Services.AddScoped<IPrivateFileStore, PrivateFileStore>();
builder.Services.AddScoped<ILibraryDocumentService, LibraryDocumentService>();
builder.Services.AddScoped<ITenderDocumentService, TenderDocumentService>();
builder.Services.AddSingleton<IChecklistTemplateProvider, ChecklistTemplateProvider>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, IdentityEmailSenderAdapter>();

builder.Services.Configure<IdentitySeedOptions>(builder.Configuration.GetSection("IdentitySeed"));
builder.Services.AddTransient<IdentitySeeder>();

builder.Services.AddHostedService<TenderReminderWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
