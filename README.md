# ALSA.NET
ALSA (Advanced Linux Sound Architecture) implemented by .NET Core. The required minimum version of .NET Core is 2.1. Theoretically support all Linux devices running .NET Core (.NET Core JIT depends on ARMv7 instructions).

## Install Dependencies
```
sudo apt-get install libasound2-dev
```

## Getting Started
1. Create a `SoundConnectionSettings` and set the parameters for recording (Optional).
    ```C#
    SoundConnectionSettings settings = new SoundConnectionSettings();
    ```
2. Create a communications channel to a sound device.
    ```C#
    using SoundDevice device = SoundDevice.Create(settings);
    ```
3. Play a WAV file
    ```C#
    device.Play("/home/pi/music.wav");
    ```
4. Record some sounds
    ```C#
    // Record for 10 seconds and save in "/home/pi/record.wav"
    device.Record(10, "/home/pi/record.wav");
    ```

## Run the sample
```
cd Alsa.Samples
dotnet publish -c release -r linux-arm -o YOUR_FOLDER
sudo dotnet YOUR_FOLDER/Alsa.Samples.dll
```

## Run the sample with Docker
Before build docker image, you need to modify SDK, runtime and apt sources(in China) to adapt to the corresponding Linux platform.

```
docker build -t alsa-sample -f Dockerfile .
docker run --rm -it --device /dev/snd -v /home/pi:/home/pi alsa-sample
```
