#version 450

layout (location = 0) in vec4 textureCoordinatesLight;

layout (location = 0) out vec4 outColor;

layout (set = 1, binding = 0) uniform sampler2DArray textureSampler;

void main(void) {
    vec4 tex = texture(textureSampler, textureCoordinatesLight.xyz);
    outColor = vec4(tex.xyz * textureCoordinatesLight.w, tex.w);
}