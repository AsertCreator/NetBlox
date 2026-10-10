#version 330

// shamelessly taken from the raylib examples

in vec2 fragTexCoord;
flat in int fragSurfaceType;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec3 fragTangent;
in vec4 fragLightSpacePosition;

uniform sampler2D blankTexture;
uniform sampler2D studTexture;
uniform sampler2D inletTexture;
uniform sampler2D universalTexture;
uniform sampler2D glueTexture;
uniform sampler2D studNormalTexture;
uniform sampler2D inletNormalTexture;
uniform sampler2D universalNormalTexture;
uniform sampler2D glueNormalTexture;
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
vec4 surfaceNormalColor() {
    if (fragSurfaceType == 1 || fragSurfaceType == 2)
        return texture(glueNormalTexture, fragTexCoord);
    else if (fragSurfaceType == 3)
        return texture(studNormalTexture, fragTexCoord);
    else if (fragSurfaceType == 4)
        return texture(inletNormalTexture, fragTexCoord);
    else if (fragSurfaceType == 5)
        return texture(universalNormalTexture, fragTexCoord);
    return texture(studNormalTexture, vec2(0));
}

vec3 specularColor() {
    float shininess = 12;

    vec3 norm = normalize(fragNormal);
    vec3 tangent = normalize(fragTangent);
    vec3 B = normalize(cross(norm, tangent));
    vec3 lightDir = normalize(lightPosition);
    vec3 viewDir = normalize(viewPosition - fragPosition);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    vec3 normalSample = surfaceNormalColor().rgb;
    vec3 tangentNormal = normalSample * 2.0 - 1.0;
    tangentNormal.y = -tangentNormal.y;
    tangentNormal = normalize(tangentNormal);

    mat3 tbn = mat3(tangent, B, norm);

    norm = normalize(tbn * tangentNormal);

    float spec = pow(max(dot(norm, halfwayDir), 0.0), shininess);

    if (dot(norm, -lightDir) > 0)
        return vec3(0, 0, 0);

    vec3 specular = 0.3 * spec * lightColor;

    return specular;
}
vec4 regularColor() {
    return surfaceColor() * fragColor;
}
vec4 diffuseColor() {
    vec3 norm = normalize(fragNormal);
    vec3 lightDir = normalize(lightPosition);
    vec4 texelColor = surfaceColor();
    vec4 result = regularColor() * min(1, max(dot(norm, lightDir), 0.3));
    return vec4(result.xyz, 1);
}
vec4 shadowColor() {
    vec3 lightNdc = fragLightSpacePosition.xyz / fragLightSpacePosition.w;
    vec3 shadowCoord = lightNdc * 0.5 + 0.5;

    if (any(lessThan(shadowCoord, vec3(0.0))) || any(greaterThan(shadowCoord, vec3(1.0)))) {
        return diffuseColor() + vec4(specularColor(), 0);
    }

    vec3 normaldir = normalize(fragNormal);
    vec3 lightdir = normalize(lightPosition);
    float ndotL = max(dot(normaldir, lightdir), 0.0);

    float minBias = 0.00008;
    float slopeBias = 0.00016 * (1.0 - ndotL);
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

    return mix(diffuseColor() + vec4(specularColor(), 0), vec4((regularColor() * 0.3).xyz, 1), shadow);
}

void main()
{
    finalColor = vec4(shadowColor().xyz, 1);
}