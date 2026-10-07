#version 330

// shamelessly taken from the raylib examples

in vec2 fragTexCoord;
flat in int fragSurfaceType;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec2 fragShadowTexCoord;
in float fragShadowDepth;

uniform sampler2D blankTexture;
uniform sampler2D studTexture;
uniform sampler2D inletTexture;
uniform sampler2D universalTexture;
uniform sampler2D glueTexture;
uniform sampler2D shadowmap;

uniform vec3 viewPosition;
uniform vec3 lightPosition;

out vec4 finalColor;

vec3 lightColor = vec3(1, 1, 0.85);

vec4 surfaceColor() {
    if (fragSurfaceType == 1 || fragSurfaceType == 2)
        return texture(glueTexture, fragTexCoord);
    else if (fragSurfaceType == 3)
        return texture(studTexture, fragTexCoord);
    else if (fragSurfaceType == 4)
        return texture(inletTexture, fragTexCoord);
    else if (fragSurfaceType == 5)
        return texture(universalTexture, fragTexCoord);
    return texture(blankTexture, fragTexCoord);
}

vec3 specularColor() {
    float shininess = 12;

    vec3 norm = normalize(fragNormal);
    vec3 lightDir = normalize(lightPosition);
    vec3 viewDir = normalize(viewPosition - fragPosition);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    float spec = pow(max(dot(norm, halfwayDir), 0.0), shininess);
    vec3 specular = 0.3 * spec * lightColor;

    return specular;
}
vec4 diffuseColor() {
    vec3 norm = normalize(fragNormal);
    vec3 lightDir = normalize(lightPosition);
    vec4 texelColor = surfaceColor();
    vec4 result = texelColor * min(1, max(dot(norm, lightDir), 0.3)) * fragColor;
    return vec4(result.xyz, 1);
}
vec4 shadowColor() {
    return diffuseColor() + vec4(specularColor(), 0);
}

void main()
{
    finalColor = shadowColor();
}