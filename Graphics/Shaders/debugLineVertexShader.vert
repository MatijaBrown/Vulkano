#version 450

layout (set = 0, binding = 0) uniform LinesVertices {
    vec4 verts[200];
} vertices;

layout (push_constant) uniform PushConstants {
    mat4 mvp;
} pushConstants;

void main(void) {
    gl_Position =  pushConstants.mvp * vec4(vertices.verts[2 * gl_InstanceIndex + gl_VertexIndex].xyz, 1.0);
}