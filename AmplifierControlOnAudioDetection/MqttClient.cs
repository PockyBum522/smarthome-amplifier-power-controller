using MQTTnet;
using MQTTnet.Client;

namespace AmplifierControlOnAudioDetection;

public static class MqttClient
{
    private static IMqttClient? _mqttClient = null;
    
    public static async Task StartMqttClient()
    {
        var mqttFactory = new MqttFactory();

        _mqttClient = mqttFactory.CreateMqttClient();
        
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithClientId("media_pc_amplifier_control_client")
            .WithTcpServer("192.168.1.100", 1883)
            .WithCredentials("########################", "########################")
            .Build();

        _mqttClient.DisconnectedAsync += async e =>
        {
            if (e.ClientWasConnected)
            {
                // Use the current options as the new options.
                await _mqttClient.ConnectAsync(_mqttClient.Options);
            }
        };
        
        // Setup message handling before connecting 
        _mqttClient.ApplicationMessageReceivedAsync += HandleIncomingMessage;

        await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        var mqttSubscribeOptions = 
            mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => { f.WithTopic("allenst/house/frontdoor/a"); })
                .Build();

        await _mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        //_logger.Information("MQTT client subscribed to topics");

        // Pause forever to wait for incoming messages
        while (true){ await Task.Delay(9999); }
     
        // ReSharper disable once FunctionNeverReturns because it's not supposed to
    }

    public static async Task SendMqttMessage(string topicToSendOn, string messagePayload)
    {
        if (_mqttClient is null) throw new NullReferenceException();
        
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topicToSendOn)
            .WithPayload(messagePayload)
            .WithQualityOfServiceLevel(0)
            .Build();

        await _mqttClient.PublishAsync(message, CancellationToken.None); // Since 3.0.5 with CancellationToken
    }
    
    private static Task HandleIncomingMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        return Task.CompletedTask;
    }
}