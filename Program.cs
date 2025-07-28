using DistributedAppExamUnicam.Messages;
using DistributedAppExamUnicam.Sagas; 
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Orleans.Configuration;
using StackExchange.Redis; 
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DistributedAppExamUnicam.Tests.Integration")]

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder
        .UseLocalhostClustering() 
        .UseDashboard(options => 
        {
            options.Port = 8080; // Porta per la dashboard
        })
       
        .AddMemoryStreams("AppointmentStream")
        .AddMemoryGrainStorage("PubSubStore") 

        // Configurazione dello storage Redis per i Grain (IPersistentState)
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

    siloBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "dev";
        options.ServiceId = "DistributedAppExamUnicam";
    });
});


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, DistributedAppExamUnicam.DummyAuthHandler>("Basic", null);
builder.Services.AddAuthorization();

builder.Services.AddMassTransit(x =>
{
   
    x.AddSagaStateMachine<BookingAppointmentSaga, BookingState>()
        .RedisRepository(r =>
        {
            r.ConnectionFactory(() => ConnectionMultiplexer.Connect("localhost:6379"));
        });

    
    x.AddConsumer<AppointmentCreatedConsumer>(); 
    x.AddConsumer<ReserveTimeSlotConsumer>(); 
    x.AddConsumer<ReleaseTimeSlotConsumer>(); 
    x.AddConsumer<PatientAppointmentRegistrarConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => // Configurazione di RabbitMQ 
        {
            h.Username("guest");
            h.Password("guest");
        });


        // Endpoint per la Saga di Booking: riceve IBookAppointmentCommand
        cfg.ReceiveEndpoint("book-appointment-saga-queue", e =>
        {
            e.ConfigureSaga<BookingState>(context);
        });

        // Endpoint per il Consumer di ReserveTimeSlotCommand
        cfg.ReceiveEndpoint("reserve-slot-command-queue", e =>
        {
            e.ConfigureConsumer<ReserveTimeSlotConsumer>(context);
        });

        // Endpoint per il Consumer di ReleaseTimeSlotCommand
        cfg.ReceiveEndpoint("release-slot-command-queue", e =>
        {
            e.ConfigureConsumer<ReleaseTimeSlotConsumer>(context);
        });

        // Endpoint per il tuo consumer esistente AppointmentCreatedConsumer
        cfg.ReceiveEndpoint("appointment-created-queue", e =>
        {
            e.ConfigureConsumer<AppointmentCreatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("patient-appointment-registrar-queue", e =>
        {
            e.ConfigureConsumer<DistributedAppExamUnicam.Messages.PatientAppointmentRegistrarConsumer>(context);
        });

     
        cfg.ConfigureEndpoints(context);
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); 
app.UseAuthorization();  

app.MapControllers(); 

app.Run();


public partial class Program { }