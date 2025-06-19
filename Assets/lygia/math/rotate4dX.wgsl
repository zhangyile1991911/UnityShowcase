/*
contributors: Patricio Gonzalez Vivo
description: returns a 4x4 rotation matrix
license:
    - Copyright (c) 2021 Patricio Gonzalez Vivo under Prosperity License - https://prosperitylicense.com/versions/3.0.0
    - Copyright (c) 2021 Patricio Gonzalez Vivo under Patron License - https://lygia.xyz/license
*/

fn rotate4dX(r: f32) -> mat4x4<f32> {
    return mat4x4<f32> (vec4f(1.,0.,0.,0),
                        vec4f(0.,cos(r),-sin(r),0.),
                        vec4f(0.,sin(r),cos(r),0.),
                        vec4f(0.,0.,0.,1.));
}
