using Microsoft.EntityFrameworkCore;
using _1013.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<tryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("tryContextConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

// �t�m�������ҩM Cookie �{��
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Home/SignIn"; // �����v�ɭ��w�V�쪺�n�J����
            options.AccessDeniedPath = "/Home/AccessDenied"; // �S�����v�v���ɭ��w�V������
        });

// 配置服務新增
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); // 使 CaptchaService 能夠存取 HttpContext
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 設置 Session 的過期時間
    options.Cookie.HttpOnly = true; // 增強安全性
    options.Cookie.IsEssential = true; // 讓 Session Cookie 成為必須

});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});
// 啟用 Session 中介軟體
app.UseSession();
app.UseRouting();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// �ҥΨ�������
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();