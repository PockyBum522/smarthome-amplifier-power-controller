using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace AmplifierControlOnAudioDetection;

public class AudioHelper
{
    private readonly ILogger _logger;

    public AudioHelper(ILogger logger)
    {
        _logger = logger;
    }
    
    public bool IsAudioPlayingOnDefaultAudioDevice()
    {
        using var devEnum = new MMDeviceEnumerator();
        
        using var defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        return defaultDevice.AudioMeterInformation.MasterPeakValue is > 0.0001f;
    }
    
    private MMDevice GetDefaultRenderDevice()
    {
        using var enumerator = new MMDeviceEnumerator();
        
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
    }

    public int GetSystemVolume()
    {
        try
        {
            using var devEnum = new MMDeviceEnumerator();
        
            using var defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            var systemVolumeScalar = defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;

            var systemVolumePercent = (int)systemVolumeScalar;

            if (systemVolumePercent > 99) systemVolumePercent = 1;
            if (systemVolumePercent < 1) systemVolumePercent = 1;
            
            return systemVolumePercent;
        }
        catch
        {
            return 1;
        }
    }
    
    public void SetSystemVolume(int volumeLevelPercent)
    {
        try
        {
            using var devEnum = new MMDeviceEnumerator();
        
            using var defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Validation/fixing
            if (volumeLevelPercent < 1) volumeLevelPercent = 1;
            if (volumeLevelPercent > 99) volumeLevelPercent = 1;
        
            var systemVolumeScalar = volumeLevelPercent / 100f;

            if (systemVolumeScalar > 0.99f) systemVolumeScalar = 0.01f;
            if (systemVolumeScalar < 0.01) systemVolumeScalar = 0.01f;
        
            _logger.LogInformation("Setting system volume (Scalar) to: {scalarVolume}", systemVolumeScalar);
        
            defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = systemVolumeScalar;
        }
        catch
        {
            using var devEnum = new MMDeviceEnumerator();
        
            using var defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = 0.01f;
        }
    }
}