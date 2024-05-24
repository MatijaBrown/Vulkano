#version 450

layout (location = 0) in vec2 textureCoords;

layout (location = 0) out vec4 out_Colour;

layout (set = 0, binding = 0) uniform sampler2D textureSampler;

void main(void) {
    out_Colour = texture(textureSampler, textureCoords);
}