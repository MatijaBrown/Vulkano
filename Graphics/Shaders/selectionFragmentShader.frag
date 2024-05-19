#version 450

layout (location = 0) out vec4 out_Colour;

layout (push_constant) uniform PushConstants {
    mat4 mvp;
    uint face;
    float alpha;
} pushConstants;

void main(void) {
    out_Colour = vec4(1.0, 1.0, 1.0, pushConstants.alpha);
}