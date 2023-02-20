using System.Diagnostics;

namespace AmplifierControlOnAudioDetection;

public class Worker : BackgroundService
{
    private static string AmplifierTopic => "allenst/house/kitchen/amplifierpower";
    private static string AmplifierCommandPowerOn => "powerOn";
    private static string AmplifierCommandPowerOff => "powerOff";
    
    private TimeSpan _timeout = TimeSpan.FromMinutes(60);
    
    private DateTimeOffset _lastAudioPlayedAtTime = DateTimeOffset.MinValue;

    private bool _amplifierTurnOnActivated;
    private bool _amplifierTurnOffActivated = true;
    
    private readonly ILogger<Worker> _logger;
    private readonly AudioHelper _audioHelper;
    
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        
        _audioHelper = new AudioHelper(logger);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // We don't want to await this because it stays loaded forever to keep the client going 
        // and also to receive and handle messages
#pragma warning disable CS4014
        MqttClient.StartMqttClient();
#pragma warning restore CS4014
        
        await Task.Delay(30 * 1000, stoppingToken);

        _audioHelper.SetSystemVolume(10);

        await Task.Delay(2000);
        
        await TurnOnAmplifierSwitch();

        // Init this so it will turn off in timeout
        _lastAudioPlayedAtTime = DateTimeOffset.Now;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var isAudioPlaying = 
                _audioHelper.IsAudioPlayingOnDefaultAudioDevice();

            if (isAudioPlaying)
                _lastAudioPlayedAtTime = DateTimeOffset.Now;

            if (LastAudioPlayedWithinTimeout() && !_amplifierTurnOnActivated)
            {
                await TurnOnAmplifierSwitch();
            }
            
            if (TimeoutExpiredSinceLastAudioPlayed() && !_amplifierTurnOffActivated)
            {
                await TurnOffAmplifierSwitch();
            }
            
            await Task.Delay(100, stoppingToken);
        }
    }

    private bool LastAudioPlayedWithinTimeout()
    {
        return _lastAudioPlayedAtTime > DateTimeOffset.Now - _timeout &&
               _lastAudioPlayedAtTime > DateTimeOffset.MinValue;
    }

    private bool TimeoutExpiredSinceLastAudioPlayed()
    {
        return _lastAudioPlayedAtTime < DateTimeOffset.Now - _timeout &&
               _lastAudioPlayedAtTime > DateTimeOffset.MinValue;
    }

    private async Task TurnOnAmplifierSwitch()
    {
        var systemOriginalVolume = _audioHelper.GetSystemVolume();
        
        _logger.LogInformation("Original volume: {originalVolume}", systemOriginalVolume);

        _audioHelper.SetSystemVolume(0);
        
        _amplifierTurnOnActivated = true;
        _amplifierTurnOffActivated = false;
        
        _logger.LogInformation("Turning on amplifier!");
        
        await MqttClient.SendMqttMessage(AmplifierTopic, AmplifierCommandPowerOn);

        await Task.Delay(2000);
        
        await RampVolumeBackUpTo(systemOriginalVolume);
    }

    private async Task RampVolumeBackUpTo(int systemOriginalVolume)
    {
        // Ramp system volume slowly back up
        for (var i = 0; i < systemOriginalVolume; i++)
        {
            var preAdjustmentVolume = _audioHelper.GetSystemVolume();
            
            await Task.Delay(750);

            // Check if user adjusted volume
            if (_audioHelper.GetSystemVolume() != preAdjustmentVolume) break;
            
            // Otherwise
            var newVolume = i + 1;
            _logger.LogInformation("Upping system volume to: {newVolume}", newVolume);
            _audioHelper.SetSystemVolume(newVolume);
        }
    }

    private async Task TurnOffAmplifierSwitch()
    {
        _amplifierTurnOnActivated = false;
        _amplifierTurnOffActivated = true;
        
        _logger.LogInformation("Turning off amplifier!");

        await MqttClient.SendMqttMessage(AmplifierTopic, AmplifierCommandPowerOff);

        // Just so we can't turn it back on real quick. Popping and such happens if we do that.
        await Task.Delay(10 * 1000);
    }
}