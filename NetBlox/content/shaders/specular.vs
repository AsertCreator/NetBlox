#version 330

// shamelessly taken from the raylib examples

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 lightVP;

smooth out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragPosition;
out vec2 fragShadowTexCoord;
out float fragShadowDepth;

void main()
{
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    fragNormal = vertexNormal;
    fragPosition = vertexPosition;

    vec4 worldSpace = matModel * vec4(vertexPosition, 1.0); // position of the model in the scene
    vec4 screenSpace = lightVP * worldSpace; // position of the vertex in screen space. equivalent to gl_Position above but for the light
    fragShadowDepth = screenSpace.z / screenSpace.w; // .z component is depth in screen space.
    fragShadowTexCoord = (screenSpace.xy / screenSpace.w) * 0.5 + 0.5; // .xy is position on the screen

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}