using DistributedAppExamUnicam.Messages;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Streams;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder
        .UseLocalhostClustering()
        .UseDashboard()

        .AddMemoryStreams("AppointmentStream")
        .AddMemoryGrainStorage("PubSubStore")

        .AddRedisGrainStorage(
            name: "redisStorage",
            configureOptions: options =>
            {
                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
            {
                EndPoints = { "localhost:6379" },
                DefaultDatabase = 0,
                AbortOnConnectFail = false

            };
    });
    
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AppointmentCreatedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("appointment-created-queue", e =>
        {
            e.ConfigureConsumer<AppointmentCreatedConsumer>(context);
        });

    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
