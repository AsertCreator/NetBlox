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
    fragNormal = normalize(vec3(instanceTransform * vec4(vertexNormal, 0.0)));
    fragPosition = vec3(instanceTransform * vec4(vertexPosition, 1.0));

    vec4 worldSpace = instanceTransform * vec4(vertexPosition, 1.0); // position of the model in the scene
    vec4 screenSpace = lightVP * worldSpace; // position of the vertex in screen space. equivalent to gl_Position above but for the light
    fragShadowDepth = screenSpace.z / screenSpace.w; // .z component is depth in screen space.
    fragShadowTexCoord = (screenSpace.xy / screenSpace.w) * 0.5 + 0.5; // .xy is position on the screen

    gl_Position = mvp * instanceTransform * vec4(vertexPosition, 1.0);
}