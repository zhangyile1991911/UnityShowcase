#include "rgb2lch.wgsl"
#include "srgb2rgb.wgsl"

/*
contributors: Patricio Gonzalez Vivo
description: Converts a sRGB color to Lab
license:
    - Copyright (c) 2021 Patricio Gonzalez Vivo under Prosperity License - https://prosperitylicense.com/versions/3.0.0
    - Copyright (c) 2021 Patricio Gonzalez Vivo under Patron License - https://lygia.xyz/license
*/


fn srgb2lch(srgb: vec3f) -> vec3f { return rgb2lch(srgb2rgb(srgb)); }
