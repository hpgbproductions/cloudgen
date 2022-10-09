# Summer-Clouds Reference

## Introduction of Cloud Generation

This section discusses how the cloud generator works, as well as some of its main settings. The principle comes from calculating cloud boundaries and filling them with particles.

### Linear Maps

A Linear Map accepts a value of `t`, and performs a linear conversion from the range `t0..t1` to the range `x0..x1`. This is such that:

- When `t` is `t0`, the output value `x` is `x0`.
- When `t` is `t1`, `x` is `x1`.
- If `t` is in the middle of `t0` and `t1`, `x` will be equally as far from the values `x0` and `x1`.
- The output value `x` is always clamped between `x0` and `x1`.

The implementation is equal to the Funky Trees:
`lerp(x0, x1, inverselerp(t0, t1, t))`

### Boundary calculations with fixed Cloud Type

Assume that the Cloud Type is constant through the entire cloud generation area.

When the Cloud Type is fixed, the only variation in cloud generation comes from one Perlin Noise set. This is referred to as the Pattern Noise and directly controls the shape of the clouds. The Pattern Noise has a range of 0 to 1, inclusive. A variety of settings modify the generation of the clouds.

#### Base Clouds

The cloud generator uses the Pattern Noise to determine the maximum height of the base clouds. The minimum height of the base clouds is equal to the start altitude.

![img1](../CloudSettingsGuide/graph/Slide1.PNG?raw=true)

The cloud sinking coefficient lowers the clouds by a height relative to the base height. Alternatively, it can be said that the sinking coefficient causes the line to shift to the right.

When the cloud pattern is lower than the cloud sinking coefficient, the maximum height is lower than the minimum height. No clouds will be produced.

![img2](../CloudSettingsGuide/graph/Slide2.PNG?raw=true)

Setting the cloud sinking coefficient is the main way to control the coverage of base clouds.

#### Head Clouds

Head clouds consist of upper and lower halves which meet at the dividing altitude. The dividing altitude is controlled using the cloud type, so when the cloud type is constant, the dividing altitude is the same for all the clouds.

The difference between the start altitude and dividing altitude is the head height.

![img3](../CloudSettingsGuide/graph/Slide3.PNG?raw=true)

The Pattern Noise can now be applied to set the shape of the clouds. The clouds generate from the dividing altitude, instead of the start altitude.

Separate scale values are provided for the upper and lower halves of the clouds. The head height is used to set the maximum height, and the steepness, of each half when the scale is 1.

Head clouds will not generate below the start altitude.

![img4](../CloudSettingsGuide/graph/Slide4.PNG?raw=true)

The sinking function for the head clouds works the same as the base clouds. The upper and lower halves are pulled towards the dividing line.

![img5](../CloudSettingsGuide/graph/Slide5.PNG?raw=true)

The lowering coefficient is then applied, which lowers both the upper and lower halves of the head clouds by a proportion of the head height.

![img6](../CloudSettingsGuide/graph/Slide6.PNG?raw=true)

Finally, a generation threshold can be added. When the Pattern Noise is less than the threshold value, no particles will be generated. It is possible to create vertical walls of clouds with this setting.

![img7](../CloudSettingsGuide/graph/Slide7.PNG?raw=true)

### Boundary calculations with varying Cloud Type

A second set of Perlin Noise can be used to control the Cloud Type. This is known as the Type Noise. The Type Noise is the input value of the CloudTypeMap Linear Map section.

The mapped value is passed to the other Linear Maps to modify cloud generation.

### Particles

Particle generation starts with Cloud Chunks. They are grid squares that are used to help center clouds around the player's aircraft. A pre-computed set of 1000 grid coordinates are used, starting with (0, 0) and spiral outwards.

Each chunk has a number of sample points which are evenly spaced apart. Each sample point has a unique X and Z coordinate. Sample points in a chunk start from (0, 0) at one corner and end at the opposite corner. The boundaries of base and head clouds are calculated for each sample point, taking into consideration the cloud type at the location as well. A column of particles can be produced at the sample point.

To reduce the appearance of uniformity, each particle has a random size and can be offset by random distances along each axis.

It is important that the length of a chunk is a multiple of the sample point interval. If the head interval multiplier, the number of base cloud particles produced every head cloud particle in each direction, is more than 1, the length of the chunk must be a multiple of this head sampling interval. This is to ensure that lines will not be visible between chunk boundaries.

### Settings Tips

It is a good idea to start with the default settings. The values and Linear Maps work for any cloud type. From the type of cloud cover you want to recreate, choose a suitable range of cloud types, and work on the Linear Maps from there. In the default cloud profile, 0 means no clouds, 0.5 means overcast with some head clouds, and 1 generates the most head clouds.

Also try out the prototype version of the cloud generator in Processing [here](https://github.com/hpgbproductions/_dump2/blob/main/CLOUDGEN/CLOUDGEN.pde). While there are some differences, many key settings were retained.

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
