#version 450

#extension GL_GOOGLE_include_directive : require

#include "blocks_common.h"

layout (location = 0) out vec4 textureCoordinatesLight;

struct BlockFace {
    uint faceInfo;
};

layout (std430, set = 0, binding = 0) readonly buffer WorldBuffer {
    BlockFace faces[];
} world;

layout (push_constant) uniform MetaChunkInfo {
    mat4 modelViewProjection;
    uint firstFaceIndex;
} chunkInfo;

const uint POSITION_MASK = 4096 - 1;
const uint FACE_MASK = 7 << 12;
const uint LIGHT_LEVEL_MASK = (16 - 1) << 15;
const uint TEXTURE_MASK = ~(0 | LIGHT_LEVEL_MASK | FACE_MASK | POSITION_MASK);

const float TEXTURE_FACE_SIZE = 16.0 / 256.0;

void main(void) {
    uint faceLocation = gl_InstanceIndex;
    uint vertexIndex = gl_VertexIndex;

    uint faceInfo = world.faces[chunkInfo.firstFaceIndex + faceLocation].faceInfo;
    uint position = (faceInfo & POSITION_MASK) >> 0;
    uint faceIndex = (faceInfo & FACE_MASK) >> 12;
    uint lightLevel = (faceInfo & LIGHT_LEVEL_MASK) >> 15;
    uint textureIndex = (faceInfo & TEXTURE_MASK) >> 19;

    vec3 texCoords = vec3(TEXTURE_POSITIONS[vertexIndex], textureIndex);
    textureCoordinatesLight = vec4(texCoords, LIGHT_LEVELS[lightLevel]);

    uint blockY = position / 256;
    uint blockX = (position % 256) / 16;
    uint blockZ = (position % 256) % 16;

    vec3 blockPosition = vec3(float(blockX), float(blockY), float(blockZ));

    gl_Position = chunkInfo.modelViewProjection * vec4(blockPosition + FACE_VERTICES[faceIndex][vertexIndex], 1.0);
}