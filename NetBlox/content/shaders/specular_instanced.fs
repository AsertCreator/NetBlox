#version 330

// shamelessly taken from the raylib examples

in vec2 fragTexCoord;
flat in int fragSurfaceType;
flat in int fragDecalType;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec3 fragTangent;
in vec4 fragLightSpacePosition;
in vec2 faceSize;

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

// this code just gets more and more horrible oh god

uniform sampler2D decalTop;
uniform sampler2D decalLeft;
uniform sampler2D decalRight;
uniform sampler2D decalBottom;
uniform sampler2D decalFront;
uniform sampler2D decalBack;

uniform vec4 decalTopTint;
uniform vec4 decalLeftTint;
uniform vec4 decalRightTint;
uniform vec4 decalBottomTint;
uniform vec4 decalFrontTint;
uniform vec4 decalBackTint;

uniform vec3 viewPosition;
uniform vec3 lightPosition;

out vec4 finalColor;

float kernel[25] = float[25](
    1, 4, 7, 4, 1,
    4, 16, 26, 16, 4,
    7, 26, 41, 26, 7,
    4, 16, 26, 16, 4,
    1, 4, 7, 4, 1
);

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
vec4 decalColor() {
    if (fragDecalType == 1)
        return texture(decalTop, fragTexCoord / faceSize * 2);
    else if (fragDecalType == 2)
        return texture(decalBottom, fragTexCoord / faceSize * 2);
    else if (fragDecalType == 3)
        return texture(decalLeft, fragTexCoord / faceSize * 2);
    else if (fragDecalType == 4)
        return texture(decalRight, fragTexCoord / faceSize * 2);
    else if (fragDecalType == 5)
        return texture(decalFront, fragTexCoord / faceSize * 2);
    else if (fragDecalType == 6)
        return texture(decalBack, fragTexCoord / faceSize * 2);
    return vec4(0);
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

    if (fragColor.w < 0.25)
        return vec3(0, 0, 0);

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
    vec4 result = surfaceColor() * fragColor;
    vec4 decal = decalColor();
    return mix(result, decal, decal.w);
}
vec4 diffuseColor() {
    vec3 norm = normalize(fragNormal);
    vec3 lightDir = normalize(lightPosition);
    vec4 texelColor = surfaceColor();
    vec4 result = regularColor() * min(1, max(dot(norm, lightDir), 0.3));
    return vec4(result.xyz, fragColor.w);
}
vec4 shadowColor() {
    vec3 lightNdc = fragLightSpacePosition.xyz / fragLightSpacePosition.w;
    vec3 shadowCoord = lightNdc * 0.5 + 0.5;

    if (fragColor.w < 0.25 || any(lessThan(shadowCoord, vec3(0.0))) || any(greaterThan(shadowCoord, vec3(1.0)))) {
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

    for (int x = -2; x <= 2; x++) {
        for (int y = -2; y <= 2; y++) {
            float shadowDepth = texture(shadowmap, shadowCoord.xy + vec2(x, y) * texelSize).r;
            float core = shadowCoord.z - bias > shadowDepth ? 1.0 : 0.0;

            shadow += core * kernel[(x + 2) * 5 + (y + 2)];
        }
    }

    shadow /= 256.0;

    return mix(diffuseColor() + vec4(specularColor(), 0), vec4((regularColor() * 0.3).xyz, fragColor.w), shadow);
}

void main()
{
    vec4 color = shadowColor();
    float alpha = color.w;
    vec3 gammaCorrected = pow(color.xyz, vec3(1.0 / 1.0));
    finalColor = vec4(gammaCorrected, alpha);
}