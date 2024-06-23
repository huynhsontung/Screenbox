precision mediump float;

const float kPi = 3.1415927;

uniform float u_time;
uniform float u_speed;
uniform float u_blend;
uniform bool u_square;
uniform bool u_center;
uniform sampler2D u_tex0;
uniform sampler2D u_tex1;
uniform vec2 u_resolution;
uniform vec3 u_center_color;
uniform float u_center_radius;

varying vec2 vUv;

void main() {
    // normalized coordinates
    vec2 p = (2. * gl_FragCoord.xy - u_resolution.xy) / u_resolution.y;

    // angle of each pixel to the center of the screen
    float a = atan(p.y, p.x);

    float r = 0.;
    if(u_square) {
        // square tunnel
        vec2 p2 = p * p, p4 = p2 * p2, p8 = p4 * p4;
        r = pow(p8.x + p8.y, 1.0 / 8.0);
    } else {
        // cylindrical tunne
        r = length(p);
    }

    // index texture by radious and angle 
    vec2 uv = vec2(0.3 / r + 0.2 * u_time * u_speed, 0.5 + a / kPi);

    // naive fetch color
    vec3 col = texture2D(u_tex0, uv).xyz;
    vec3 col1 = texture2D(u_tex1, uv).xyz;
    // blend, transition
    col = mix(col, col1, u_blend);

    if(u_center) {
        // fade out from center
        float fadeAmount = 1.0 - smoothstep(0.0, u_center_radius, r);
        col = mix(col, u_center_color, fadeAmount);
    }

    gl_FragColor = vec4(col, 1.);
}