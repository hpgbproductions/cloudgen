# Cloud Settings Reference

Reference for all available cloud profile settings.

## CloudGenerator section

Name | Type | Description
:--- | :--- | :---
Version | string | The version of the cloud generator that the settings are made for. Currently unused.

## CloudGeneratorSettings section

This section provides settings common to both the Base and Head cloud layers.

Name | Type | Description
:--- | :--- | :---
MinParticles | int | Cloud generation will be halted once this number of particles is reached. Leave some from the end to prevent exceptions from being thrown.
MaxParticles | int | Size of the particle buffer.
NoiseScale | float | One unit in world coordinates corresponds to this length in the Perlin Noise used to generate cloud patterns. Greater values cause steeper cloud slopes.
CloudTypeNoiseScale | float | Controls the Perlin Noise that sets the cloud intensity for each sample point. Greater values cause more variations in cloud sizes and heights.
OverrideWeather | int/string | Optionally set the weather when the cloud profile is loaded. Values from 0 to 7 correspond to weather choices in the game. Use other values (e.g. -1) to keep the existing weather.
StartAltitude | float | The lowest altitude that clouds can generate. Note that the cloud ceiling will appear lower due to the size of particles, and random particle offsets can cause particles to appear below this altitude.
IntervalX | float | Distance between sample points along the X-axis.
IntervalY | float | The mean height between particles in a columnn.
IntervalZ | float | Distance between sample points along the Z-axis.
MaxChunks | int 1..1000 | The maximum number of sample point chunks. The chunks start at the aircraft's position and spiral outwards. Greater values allow a larger area to be covered by clouds, but can lead to longer cloud generation times.
ChunkSize | float | Length of a square that clouds are generated in. Greater values allow a larger area to be covered by clouds, but can lead to longer cloud generation times.
Octaves | int | Number of Perlin Noise layers. Greater values create more natural-looking clouds by reducing uniformity, but increase cloud generation times.
Persistence | float | Amplitude of one Perlin Noise layer compared to the last.
Lacunarity | float | Frequency of one Perlin Noise layer compared to the last.
CastShadows | bool | Particles cast shadows. Very heavy performance cost.
ReceiveShadows | bool | Particles receive shadows (not sure if it does anything). Very heavy performance cost.
MaxParticleSizeOnScreen | float | If too small, particles passing the aircraft will shrink unnaturally quickly. If too large, particles will fail to shrink and cause excessive flashing lights. Greater values also increase performance cost.
CameraFarFade | float | Fade out particles that pass close to the camera. At this distance, particles start to fade. Greater values lead to higher performance costs.
CameraNearFade | float | Particles closer than this distance will be faded completely.
