#version 450

#extension GL_GOOGLE_include_directive : require

#include "blocks_common.h"

layout (push_constant) uniform PushConstants {
    mat4 mvp;
    uint face;
    float alpha;
} pushConstants;

void main(void) {
    gl_Position =  pushConstants.mvp * vec4(FACE_VERTICES[pushConstants.face][gl_VertexIndex], 1.0);
}