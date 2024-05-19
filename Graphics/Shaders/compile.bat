glslc %1/chunkVertexShader.vert -o %2/chunkVertexShader.spv
glslc %1/chunkFragmentShader.frag -o %2/chunkFragmentShader.spv

glslc %1/selectionVertexShader.vert -o %2/selectionVertexShader.spv
glslc %1/selectionFragmentShader.frag -o %2/selectionFragmentShader.spv

glslc %1/debugLineVertexShader.vert -o %2/debugLineVertexShader.spv
glslc %1/debugLineFragmentShader.frag -o %2/debugLineFragmentShader.spv

pause