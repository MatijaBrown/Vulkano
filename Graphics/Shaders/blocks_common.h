const vec3 FACE_VERTICES[6][4] = vec3[6][4] (
    vec3[4] ( vec3(0, 1, 1), vec3(1, 1, 1), vec3(1, 1, 0), vec3(0, 1, 0) ),   // Top
    vec3[4] ( vec3(0, 0, 0), vec3(1, 0, 0), vec3(1, 0, 1), vec3(0, 0, 1) ),   // Bottom
    vec3[4] ( vec3(0, 0, 0), vec3(0, 0, 1), vec3(0, 1, 1), vec3(0, 1, 0) ),   // West (Left)
    vec3[4] ( vec3(1, 0, 1), vec3(1, 0, 0), vec3(1, 1, 0), vec3(1, 1, 1) ),   // East (Right)
    vec3[4] ( vec3(1, 0, 0), vec3(0, 0, 0), vec3(0, 1, 0), vec3(1, 1, 0) ),   // South (Front)
    vec3[4] ( vec3(0, 0, 1), vec3(1, 0, 1), vec3(1, 1, 1), vec3(0, 1, 1) )    // North (Back)
);

const vec2 TEXTURE_POSITIONS[4] = vec2[4] (
    vec2(0, 1),
    vec2(1, 1),
    vec2(1, 0),
    vec2(0, 0)
);

const float LIGHT_LEVELS[2] = float[2] (
    0.12,
    1.0
);