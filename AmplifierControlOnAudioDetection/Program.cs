using AmplifierControlOnAudioDetection;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices(services => { services.AddHostedService<Worker>(); })
    .Build();

host.Run();