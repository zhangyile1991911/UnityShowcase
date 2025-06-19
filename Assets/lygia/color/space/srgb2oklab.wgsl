#include "srgb2rgb.wgsl"
#include "rgb2oklab.wgsl"

/*
contributors: Bjorn Ottosson (@bjornornorn)
description: 'sRGB to OKLab https://bottosson.github.io/posts/oklab/'
license: 
    - MIT License (MIT) Copyright (c) 2020 Björn Ottosson
*/


fn srgb2oklab(srgb: vec3f) -> vec3f { return rgb2oklab( srgb2rgb(srgb) ); }
