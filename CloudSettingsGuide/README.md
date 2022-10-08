# Summer-Clouds Reference

## Introduction of Cloud Generation

### Boundary calculations with fixed Cloud Type

#### Base Clouds

#### Head Clouds

### Boundary calculations with varying Cloud Type

### Particles

### Settings Tips

It is a good idea to start with the default settings. The values and Linear Maps work for any cloud type. From the type of cloud cover you want to recreate, choose a suitable range of cloud types, and work on the Linear Maps from there. In the default cloud profile, 0 means no clouds, 0.5 means overcast with some head clouds, and 1 generates the most head clouds.

## List of Settings

Reference for all available cloud profile settings.

### CloudGenerator section

Name | Type | Description
:--- | :--- | :---
Version | string | The version of the cloud generator that the settings are made for. Currently unused.

### CloudGeneratorSettings section

This section provides settings common to both the Base and Head cloud layers.

Name | Type | Description
:--- | :--- | :---
MinParticles | int | Cloud generation will be halted once this number of particles is reached. Greater values increase performance cost, but note that a device can handle more particles if they are spaced further apart.
MaxParticles | int | Size of the particle buffer. It should be slightly larger than MinParticles (e.g. by 100) to prevent exceptions from being thrown.
NoiseScale | float | One unit in world coordinates corresponds to this length in the Perlin Noise used to generate cloud patterns. Greater values cause steeper cloud slopes.
CloudTypeNoiseScale | float | Controls the Perlin Noise that sets the cloud intensity for each sample point. Greater values cause more variations in cloud sizes and heights.
OverrideWeather | int/string | Optionally set the weather when the cloud profile is loaded. Values from 0 to 7 correspond to weather choices in the game. Use other values (e.g. -1) to keep the existing weather.
StartAltitude | float | The lowest altitude that clouds can generate. It can be used to apply a vertical offset to all clouds. Note that the cloud ceiling will appear lower due to the size of particles, and random particle offsets can cause particles to appear below this altitude.
IntervalX | float | Distance between sample points along the X-axis. Greater values mean fewer sample points and particles.
IntervalY | float | The mean height between particles in a columnn. Greater values mean fewer particles in each column.
IntervalZ | float | Distance between sample points along the Z-axis. Greater values mean fewer sample points and particles.
MaxChunks | int 1..1000 | The maximum number of sample point chunks. The chunks start at the aircraft's position and spiral outwards. Greater values allow a larger area to be covered by clouds, but can lead to longer cloud generation times.
ChunkSize | float | Length of a square that clouds are generated in. Greater values allow a larger area to be covered by clouds, but can lead to longer cloud generation times.
Octaves | int | Number of Perlin Noise layers. Greater values create more natural-looking clouds by reducing uniformity, but increase cloud generation times.
Persistence | float | Amplitude of one Perlin Noise layer compared to the last.
Lacunarity | float | Frequency of one Perlin Noise layer compared to the last.
CastShadows | bool | Particles cast shadows in a short radius around the aircraft. Very heavy performance cost.
ReceiveShadows | bool | Particles receive shadows (not sure if it does anything). Very heavy performance cost.
MaxParticleSizeOnScreen | float | If too small, particles passing the aircraft will shrink unnaturally quickly. If too large, particles will fail to shrink and cause excessive flashing lights. Greater values also increase performance cost.
CameraFarFade | float | Fade out particles that pass close to the camera. At this distance, particles start to fade. Greater values lead to higher performance costs.
CameraNearFade | float | Particles closer than this distance will be faded completely.

### BaseClouds section

The settings in this section only control cloud generation in the base layer.

Name | Type | Description
:--- | :--- | :---
Scale | float | Maximum height
MinParticleSize | float | Smallest possible size of particles in the world. This should be at least twice of IntervalX and IntervalZ to minimize holes between particles.
MaxParticleSize | float | Largest possible size of particles. Larger particles can have more performance costs, but are often easier to run than a larger number of particles.
RandomOffsetX | float | A random translation of the particle from its usual position.
RandomOffsetY | float | A random translation of the particle from its usual position.
RandomOffsetZ | float | A random translation of the particle from its usual position.

### HeadClouds section

The settings in this section only control cloud generation in the head layer.

Name | Type | Description
:--- | :--- | :---
Scale | float | Maximum height of the dividing line between the two halves of the head clouds from the start altitude.
UpperHalfScale | float | Coefficient of the scale applied to the upper half.
LowerHalfScale | float | Coefficient of the scale applied to the lower half.
IntervalCount | int | Multiplies the interval for head clouds, i.e., this number of base particles is generated every head particle along each axis. This reduces the number of particles used to form the head clouds, which are usually very large.
MinParticleSize | float | Smallest possible size of particles in the world. This should be at least twice of IntervalX and IntervalZ, multiplied by the IntervalCount, to minimize holes between particles.
MaxParticleSize | float | Largest possible size of particles. Larger particles can have more performance costs, but are often easier to run than a larger number of particles.
RandomOffsetX | float | A random translation of the particle from its usual position.
RandomOffsetY | float | A random translation of the particle from its usual position.
RandomOffsetZ | float | A random translation of the particle from its usual position.

### Linear Map Sections

Each Linear Map has the values `t0`, `t1`, `x0`, and `x1`. When a value of `t` is applied to a linear map, a corresponding value of `x` is returned, which is used as a coefficient in various cloud altitude calculations.

Notes:

- `x` is always in the range of `x0` to `x1`, inclusive.
- `x` is constant if `x0` and `x1` are equal.
- Typically, a larger value of `t` produces a larger value of `x`. However, this is reversed if `x0` is larger than `x1`.
- The calculation can be written as follows: `x = lerp(x0, x1, inverselerp(t0, t1, t))`

Name | Input "t" | Description
:--- | :--- | :---
CloudTypeMap | Cloud Type Perlin Noise | Defines a simulated weather intensity.
BaseHeightMap | CloudTypeMap | Scales the maximum height of the base clouds.
BaseSinkFactorMap | CloudTypeMap | Lowers the base clouds by a coefficient of the base scale. If the cloud pattern is lower than this value at any given point, base clouds will be below the start altitude and will not appear. Increase this value if straight vertical edges are appearing on head clouds.
HeadHeightMap | CloudTypeMap | Scales the height of the divider from the start altitude.
HeadSinkFactorMap | CloudTypeMap | Moves the upper and lower halves of the head clouds towards the dividing line. If the cloud pattern is lower than this value, head clouds will not appear.
HeadLowerFactorMap | CloudTypeMap | Lowers the head clouds by a multiple of the head scale. Increase the x-values if the head and base clouds are not connected.
HeadGenerateThresholdMap | CloudTypeMap | Only generate head clouds if the cloud pattern is greater than this value.
