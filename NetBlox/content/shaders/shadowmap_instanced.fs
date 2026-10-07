#version 330

// shamelessly taken from the raylib examples

in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;

uniform sampler2D texture0;
uniform vec3 viewPosition;
uniform vec3 lightPosition;
uniform vec4 colDiffuse;

out vec4 finalColor;

void main()
{
    finalColor = vec4(1, 1, 1, 1);
}