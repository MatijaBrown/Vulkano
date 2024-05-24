#version 450

layout (location = 0) in vec3 position;
layout (location = 1) in vec2 textureCoords;

layout (location = 0) out vec2 pass_textureCoords;

layout (push_constant) uniform PushConstants {
    mat4 model;
    mat4 viewProjection;
} transform;

void main(void) {
    gl_Position = transform.viewProjection * transform.model * vec4(position, 1.0);
    pass_textureCoords = textureCoords;
}