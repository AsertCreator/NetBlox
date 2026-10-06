#version 330

// shamelessly taken from the raylib examples

in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in vec2 fragShadowTexCoord;
in float fragShadowDepth;

uniform sampler2D texture0;
uniform sampler2D shadowmap;
uniform vec3 viewPosition;
uniform vec3 lightPosition;
uniform vec4 colDiffuse;

out vec4 finalColor;

vec3 lightColor = vec3(1, 1, 0.85);

vec3 regularColor() {
    vec3 texelColor = texture(texture0, fragTexCoord).xyz;
    return texelColor;
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
    vec3 texelColor = regularColor();
    return vec4(texelColor * min(1, max(dot(norm, lightDir), 0.3)), 1) * colDiffuse * fragColor;
}
vec4 shadowColor() {
    return diffuseColor() + vec4(specularColor(), 0);
}

void main()
{
    finalColor = shadowColor();
}