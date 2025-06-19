#include "../space/rgb2hsv.wgsl"
#include "../space/hsv2rgb.wgsl"

/*
contributors: Romain Dura
description: Color Blend mode creates the result color by combining the luminance of the base color with the hue and saturation of the blend color.
use: blendColor(<float|vec3> base, <float|vec3> blend [, <float> opacity])
license: MIT License (MIT) Copyright (c) 2015 Jamie Owen
*/

fn blendColor(base : vec3f, blend : vec3f) -> vec3f {
  let baseHSL = rgb2hsv(base);
  let blendHSL = rgb2hsv(blend);

  return hsv2rgb(vec3f(blendHSL.x, blendHSL.y, baseHSL.z));
}

fn blendColorOpacity(base : vec3f, blend : vec3f, opacity : f32) -> vec3f { 
  return blendColor(base, blend) * opacity + base * (1.0 - opacity); 
}
