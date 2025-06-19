#include "oklab2rgb.wgsl"
#include "rgb2srgb.wgsl"

/*
contributors: Bjorn Ottosson (@bjornornorn)
description: oklab to sRGB https://bottosson.github.io/posts/oklab/
use: <vec3\vec4> oklab2rgb(<vec3|vec4> oklab)
license: 
    - MIT License (MIT) Copyright (c) 2020 Björn Ottosson
*/

fn oklab2srgb(oklab: vec3f) -> vec3f { return rgb2srgb(oklab2rgb(oklab)); }
