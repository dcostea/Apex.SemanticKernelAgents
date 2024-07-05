using NAudio.Wave;

namespace Apex.SemanticKernelAgents.Helpers;

public class AudioHelper
{
    private readonly WaveOutEvent _waveOutDevice;
    private readonly Mp3FileReader _reader;

    public AudioHelper(byte[] data)
    {
        var mp3Stream = new MemoryStream(data);
        _reader = new Mp3FileReader(mp3Stream);
        _waveOutDevice = new WaveOutEvent();
        _waveOutDevice.Init(_reader);
    }

    public void Play()
    {
        _waveOutDevice.Play();
    }

    public void Stop()
    {
        _waveOutDevice.Stop();
        _reader.Close();
        _waveOutDevice.Dispose();
    }
}
