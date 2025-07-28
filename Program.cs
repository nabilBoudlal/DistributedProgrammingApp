using DistributedAppExamUnicam.Messages;
using DistributedAppExamUnicam.Sagas; // Importa il namespace della tua Saga e dei Consumer
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Orleans.Configuration;
using StackExchange.Redis; // Necessario per ClusterOptions
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DistributedAppExamUnicam.Tests.Integration")]

var builder = WebApplication.CreateBuilder(args);

// --- 1. Configurazione dell'Orleans Silo (Host dei Grain) ---
// Questo blocco configura il server Orleans che ospiterà i tuoi Grain.
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder
        .UseLocalhostClustering() // Per un cluster locale (utile per lo sviluppo)
        .UseDashboard(options => // Abilita la dashboard di Orleans (opzionale ma utile)
        {
            options.Port = 8080; // Porta per la dashboard
        })
        // Orleans Streams: Se non li usi attivamente nel flusso di booking con la saga,
        // potresti rimuoverli per semplicità, ma li lascio se hai altri usi.
        .AddMemoryStreams("AppointmentStream")
        .AddMemoryGrainStorage("PubSubStore") // Storage per i metadati degli stream

        // Configurazione dello storage Redis per i Grain (IPersistentState)
        .AddRedisGrainStorage(
            name: "redisStorage", // Questo nome DEVE corrispondere al nome nel [PersistentState("stateName", "redisStorage")]
            configureOptions: options =>
            {
                options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
                {
                    EndPoints = { "localhost:6379" }, // Indirizzo del tuo server Redis
                    DefaultDatabase = 0,
                    AbortOnConnectFail = false // Non abortire se la connessione fallisce subito
                };
            });

    // Configura l'ID del cluster e del servizio per il Silo
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

// --- 3. Configurazione dell'Autenticazione e Autorizzazione ---
builder.Services.AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, DistributedAppExamUnicam.DummyAuthHandler>("Basic", null);
builder.Services.AddAuthorization();

// --- 4. Configurazione di MassTransit ---
builder.Services.AddMassTransit(x =>
{
    // --- Registrazione della SAGA ---
    // Aggiungi la tua Saga State Machine e il suo repository per la persistenza dello stato.
    x.AddSagaStateMachine<BookingAppointmentSaga, BookingState>()
        .RedisRepository(r =>
        {
            r.ConnectionFactory(() => ConnectionMultiplexer.Connect("localhost:6379"));

            // Opzionale: puoi specificare un database Redis se necessario
            // r.Database(1);
        });

    // --- Registrazione dei Consumer ---
    // Questi consumer verranno attivati da MassTransit quando ricevono i messaggi corrispondenti.
    x.AddConsumer<AppointmentCreatedConsumer>(); // Il tuo consumer esistente
    x.AddConsumer<ReserveTimeSlotConsumer>(); // Consumer per orchestrare la prenotazione dello slot
    x.AddConsumer<ReleaseTimeSlotConsumer>(); // Consumer per orchestrare il rilascio dello slot (compensazione)
    x.AddConsumer<PatientAppointmentRegistrarConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => // Configurazione del tuo RabbitMQ host
        {
            h.Username("guest");
            h.Password("guest");
        });

        // --- Configurazione degli Endpoint di Ricezione (Code) ---
        // Ogni Saga o Consumer deve avere un endpoint di ricezione (una coda)
        // dove MassTransit invierà i messaggi a loro destinati.

        // Endpoint per la Saga di Booking: riceve IBookAppointmentCommand
        cfg.ReceiveEndpoint("book-appointment-saga-queue", e =>
        {
            // Collega la saga a questo endpoint. MassTransit gestirà la creazione/caricamento delle istanze della saga.
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

        // Questo configura automaticamente gli endpoint per tutti i messaggi pubblicati
        // che non hanno un endpoint di ricezione esplicito configurato sopra.
        // È una buona pratica per gli eventi che non sono diretti a una coda specifica.
        cfg.ConfigureEndpoints(context);
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // Abilita l'autenticazione
app.UseAuthorization();  // Abilita l'autorizzazione

app.MapControllers(); // Mappa gli endpoint dei controller

app.Run();


public partial class Program { }