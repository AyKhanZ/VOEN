using WebApplication2;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

//builder.Services.AddSingleton<TwoCaptchaSolver>(sp =>
//{
//    // Здесь нужно использовать ваш реальный API ключ 2Captcha
//    string apiKey = "5eff7a0509edf4c26a2b174670242016";
//    return new TwoCaptchaSolver(apiKey);
//});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
