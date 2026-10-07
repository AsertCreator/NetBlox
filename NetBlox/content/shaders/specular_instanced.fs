#version 330

// shamelessly taken from the raylib examples

in vec2 fragTexCoord;
flat in int fragSurfaceType;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec4 fragLightSpacePosition;

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
    if (fragLightSpacePosition.w <= 0.0) {
        return diffuseColor() + vec4(specularColor(), 0);
    }

    vec3 lightNdc = fragLightSpacePosition.xyz / fragLightSpacePosition.w;
    vec3 shadowCoord = lightNdc * 0.5 + 0.5;

    if (any(lessThan(shadowCoord, vec3(0.0))) || any(greaterThan(shadowCoord, vec3(1.0)))) {
        return diffuseColor() + vec4(specularColor(), 0);
    }

    vec3 normaldir = normalize(fragNormal);
    vec3 lightdir = normalize(lightPosition);
    float ndotL = max(dot(normaldir, lightdir), 0.0);

    float minBias = 0.0002;
    float slopeBias = 0.001 * (1.0 - ndotL);
    float bias = max(minBias, slopeBias);

    vec2 texelSize = 1.0 / textureSize(shadowmap, 0);
    float shadow = 0;

    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            float shadowDepth = texture(shadowmap, shadowCoord.xy + vec2(x, y) * texelSize).r;
            shadow += shadowCoord.z - bias > shadowDepth ? 1.0 : 0.0;
        }
    }

    shadow /= 9.0;

    return mix(diffuseColor() + vec4(specularColor(), 0), vec4((diffuseColor() / 3).xyz, 1), shadow);
}

void main()
{
    finalColor = shadowColor();
}