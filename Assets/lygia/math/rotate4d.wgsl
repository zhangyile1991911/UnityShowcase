/*
contributors: Patricio Gonzalez Vivo
description: returns a 4x4 rotation matrix
license:
    - Copyright (c) 2021 Patricio Gonzalez Vivo under Prosperity License - https://prosperitylicense.com/versions/3.0.0
    - Copyright (c) 2021 Patricio Gonzalez Vivo under Patron License - https://lygia.xyz/license
*/

fn rotate4d(a: vec3f, r: f32) -> mat4x4<f32> {
    let s = sin(r);
    let c = cos(r);
    let oc = 1.0 - c;
    return mat4x4<f32>( oc * a.x * a.x + c,         oc * a.x * a.y - a.z * s,   oc * a.z * a.x + a.y * s,   0.0,
                        oc * a.x * a.y + a.z * s,   oc * a.y * a.y + c,         oc * a.y * a.z - a.x * s,   0.0,
                        oc * a.z * a.x - a.y * s,   oc * a.y * a.z + a.x * s,   oc * a.z * a.z + c,         0.0,
                        0.0,                        0.0,                        0.0,                        1.0);
}
