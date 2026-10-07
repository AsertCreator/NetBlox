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
uniform int surfaceTop;
uniform int surfaceBottom;
uniform int surfaceLeft;
uniform int surfaceRight;
uniform int surfaceFront;
uniform int surfaceBack;
uniform int useSurfaces;

smooth out vec2 fragTexCoord;
flat out int fragSurfaceType;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragPosition;
out vec4 fragLightSpacePosition;

void main()
{
    vec3 instanceSize = vec3(
        length(instanceTransform[0].xyz),
        length(instanceTransform[1].xyz),
        length(instanceTransform[2].xyz)
    );
    vec2 faceSize = instanceSize.xy;

    // chatgpt told me to do this

    if (abs(vertexNormal.x) > 0.5) {
        fragSurfaceType = vertexNormal.x > 0.0 ? surfaceRight : surfaceLeft;
        faceSize = instanceSize.zy;
    }
    else if (abs(vertexNormal.y) > 0.5) {
        fragSurfaceType = vertexNormal.y > 0.0 ? surfaceTop : surfaceBottom;
        faceSize = instanceSize.xz;
    }
    else {
        fragSurfaceType = vertexNormal.z > 0.0 ? surfaceFront : surfaceBack;
    }

    if (useSurfaces == 0) {
        fragSurfaceType = 0;
        faceSize = vec2(1.0);
    }

    mat3 normalMatrix = transpose(inverse(mat3(instanceTransform)));

    fragTexCoord = vertexTexCoord * faceSize * 0.5;
    fragColor = vertexColor;
    fragNormal = normalize(normalMatrix * vertexNormal);
    fragPosition = vec3(instanceTransform * vec4(vertexPosition, 1.0));

    vec4 worldSpace = instanceTransform * vec4(vertexPosition, 1.0); // position of the model in the scene
    fragLightSpacePosition = lightVP * worldSpace;

    gl_Position = mvp * instanceTransform * vec4(vertexPosition, 1.0);
}