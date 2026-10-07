#version 330

// shamelessly taken from the raylib examples

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

in mat4 instanceTransform;

uniform mat4 mvp;
uniform mat4 lightVP;
uniform mat4 matNormal;

void main()
{
    gl_Position = mvp * instanceTransform * vec4(vertexPosition, 1.0);
}