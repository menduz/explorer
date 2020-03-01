Shader "Hidden/GTAO"
{
    HLSLINCLUDE

        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Colors.hlsl"

		#define PI				3.1415926535897932
		#define TWO_PI			6.2831853071795864
		#define HALF_PI			1.5707963267948966
		#define ONE_OVER_PI		0.3183098861837906

		#define NUM_STEPS		4.0
		#define RADIUS			4.0		// in world space
		
		#define NUM_MIP_LEVELS		5.0
		#define PREFETCH_CACHE_SIZE	8.0

		#define NUM_DIRECTIONS	2
		
		//#define EXCLUDE_FAR_PLANE

		#define _Opacity 1

		float4x4 unity_CameraInvProjection;
		float4x4 _InverseView;
		
		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
		TEXTURE2D_SAMPLER2D(_NoiseTexture, sampler_NoiseTexture);
		TEXTURE2D_SAMPLER2D(_NormalTexture, sampler_NormalTexture);

		struct Varyings
		{
			float4 position : SV_Position;
			float2 texcoord : TEXCOORD0;
			float3 ray : TEXCOORD1;
		};

		float readDepth( float2 uv, float lod )
		{
			//float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
			//return depth;
			return SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, uv, lod);
		}
		float4 ComputeViewSpacePosition(Varyings input, float2 uv, float currStep)
		{

			// Render settings
			float near = _ProjectionParams.y;
			float far = _ProjectionParams.z;
			float isOrtho = unity_OrthoParams.w; // 0: perspective, 1: orthographic

			// Z buffer sample
			int miplevel = clamp(int(floor(log2(currStep / PREFETCH_CACHE_SIZE))), 0, NUM_MIP_LEVELS - 1);
			float z = readDepth(uv, miplevel);

			// Far plane exclusion
			#if !defined(EXCLUDE_FAR_PLANE)
			float mask = 1;
			#elif defined(UNITY_REVERSED_Z)
			float mask = z > 0;
			#else
			float mask = z < 1;
			#endif

			// Perspective: view space position = ray * depth
			float3 vposPers = input.ray * Linear01Depth(z);

			// Orthographic: linear depth (with reverse-Z support)
			#if defined(UNITY_REVERSED_Z)
			float depthOrtho = -lerp(far, near, z);
			#else
			float depthOrtho = -lerp(near, far, z);
			#endif

			// Orthographic: view space position
			float3 vposOrtho = float3(input.ray.xy, depthOrtho);

			float4 result = 0;

			if ( z == 0 )
			{
				result.w = 1;
			}
			// Result: view space position
			result.xyz = lerp(vposPers, vposOrtho, isOrtho) * mask;

			return result;
		}

		
		float4 GetPos(Varyings input, float2 uv, float currStep)
		{
			//return ComputeViewSpacePosition(input, uv, currStep).xyzz;
			float4 vp = ComputeViewSpacePosition(input, uv, currStep);
			float4 tmp_vp = vp;
			tmp_vp.w = 1;
			float3 wp = mul(_InverseView, tmp_vp);
				
			return float4(wp.x, wp.y, wp.z, vp.w);
		}

		float4 GetViewPos(Varyings input, float2 uv, float currStep)
		{
			return ComputeViewSpacePosition(input, uv, currStep);
			//return mul(_InverseView, float4(ComputeViewSpacePosition(input, uv, currStep), 1)).xyzz;
		}



		float4 GetViewPosition(float2 uv, float currstep)
		{
			int miplevel = clamp(int(floor(log2(currstep / PREFETCH_CACHE_SIZE))), 0, NUM_MIP_LEVELS - 1);

			float d = readDepth(uv, 1);
			float2 basesize = _ScreenParams.xy;

			float4 ret = float4(0.0, 0.0, 0.0, d);

			//float4 _ZBufferParams;            // x: 1-far/near,     y: far/near, z: x/far,     w: y/far
			//Math::Vector4 projinfo	= { 2.0f / (width * proj._11), 2.0f / (height * proj._22), -1.0f / proj._11, -1.0f / proj._22 };
			float4 projInfo = float4( 2.0 / (basesize.x * unity_CameraProjection._11),
                                      2.0 / (basesize.y * unity_CameraProjection._22),
			                         -1.0 / unity_CameraProjection._11,
			                         -1.0 / unity_CameraProjection._22 );

			ret.z = _ProjectionParams.z + d * (_ProjectionParams.y - _ProjectionParams.z);
			ret.xy = (uv * projInfo.xy + projInfo.zw) * ret.z;
			return ret;
		}

		float Falloff(float dist2, float cosh)
		{
			#define FALLOFF_START2	0.001
			#define FALLOFF_END2	RADIUS * 0.2

			return RADIUS * clamp((dist2 - FALLOFF_START2) / (FALLOFF_END2 - FALLOFF_START2), 0.0, 1.0);
		}

		float3 GetNormalTrucazo(float2 uv )
		{
			// Depth of the current pixel
			float dhere = readDepth( uv, 0 );
			// Vector from camera to the current pixel's position
			float3 ray = GetViewPosition(uv, 0);//GetCameraVec(tc_original) * dhere;
			const float normalSampleDist = .05;

			float4 viewsizediv = float4( 1 / _ScreenParams.x, 1 / _ScreenParams.y, 0, 0);
   
			// Calculate normal from the 4 neighbourhood pixels
			float2 tmp_uv = uv + float2(viewsizediv.x * normalSampleDist, 0.0);
			float3 p1 = ray - GetViewPosition(tmp_uv, 0);//ray - GetCameraVec(uv) * textureLod(gDepth, uv, 0.0).x;
   
			tmp_uv = uv + float2(0.0, viewsizediv.y * normalSampleDist);
			float3 p2 = ray - GetViewPosition(tmp_uv, 0);//ray - GetCameraVec(uv) * textureLod(gDepth, uv, 0.0).x;
   
			tmp_uv = uv + float2(-viewsizediv.x * normalSampleDist, 0.0);
			float3 p3 = ray - GetViewPosition(tmp_uv, 0);//ray - GetCameraVec(uv) * textureLod(gDepth, uv, 0.0).x;
   
			tmp_uv = uv + float2(0.0, -viewsizediv.y * normalSampleDist);
			float3 p4 = ray - GetViewPosition(tmp_uv, 0);//ray - GetCameraVec(uv) * textureLod(gDepth, uv, 0.0).x;
   
			float3 normal1 = normalize(cross(p1, p2));
			float3 normal2 = normalize(cross(p3, p4));
   
			return normalize(normal1 + normal2);
		}

		float3 GetNormal( float2 uv )
		{
			return SAMPLE_TEXTURE2D(_NormalTexture, sampler_NormalTexture, uv).rgb;		
		}

		float GetAspectRatio()
		{
			float t = unity_CameraProjection._11;
			const float Rad2Deg = 180 / PI;
			float fov = atan(1.0 / t) * 2.0 * Rad2Deg;

			//clipinfo.z = 0.5f * (app->GetClientHeight() / (2.0f * tanf(camera.GetFov() * 0.5f)));
			return 0.5 * (_ScreenParams.y / (2.0 * tan(fov * 0.5))); 
		}


		// Vertex shader that procedurally outputs a full screen triangle
		Varyings Vertex(uint vertexID : SV_VertexID)
		{
			// Render settings
			//float far = _ProjectionParams.z;
			//float2 orthoSize = unity_OrthoParams.xy;
			//float isOrtho = unity_OrthoParams.w; // 0: perspective, 1: orthographic

			// Vertex ID -> clip space vertex position
			float x = (vertexID != 1) ? -1 : 3;
			float y = (vertexID == 2) ? -3 : 1;
			float3 vpos = float3(x, y, 1.0);

			// Perspective: view space vertex position of the far plane
			//float3 rayPers = mul(unity_CameraInvProjection, vpos.xyzz * far).xyz;

			// Orthographic: view space vertex position
			//float3 rayOrtho = float3(orthoSize * vpos.xy, 0);

			Varyings o;
			o.position = float4(vpos.x, -vpos.y, 1, 1);
			o.texcoord = (vpos.xy + 1) / 2;
			//o.ray = lerp(rayPers, rayOrtho, isOrtho);
			return o;
		}




		half4 VisualizePosition(Varyings input, float3 pos)
		{
			const float grid = 5;
			const float width = 3;

			pos *= grid;

			// Detect borders with using derivatives.
			float3 fw = fwidth(pos);
			half3 bc = saturate(width - abs(1 - 2 * frac(pos)) / fw);

			// Frequency filter
			half3 f1 = smoothstep(1 / grid, 2 / grid, fw);
			half3 f2 = smoothstep(2 / grid, 4 / grid, fw);
			bc = lerp(lerp(bc, 0.5, f1), 0, f2);

			// Blend with the source color.
			half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
			c.rgb = SRGBToLinear(lerp(LinearToSRGB(c.rgb), bc, _Opacity));

			return c;
		}


        float4 Frag(Varyings input) : SV_Target
        {
			float2 uv = input.texcoord;

			float4 params = float4(0,5,0.1,0.1);
			int2 loc = int2(uv * _ScreenParams.xy);
			//float4 vpos = GetViewPosition(uv, 1.0) / 100.0;

			float4 vpos = GetViewPosition(uv, 1.0);
			//float3 vpos = ComputeViewSpacePosition(input);

			//return VisualizePosition(input, wpos); 

			if (vpos.w == 1.0) {
				return 1;
			}

			float4 s;
			float3 vnorm = GetNormalTrucazo(uv);
			//return float4(vnorm.x, vnorm.y, vnorm.z, 1);
			float3 vdir	= normalize(-vpos.xyz);
			float3 dir, ws;

			// calculation uses left handed system
			vnorm.z = -vnorm.z;

			float2 noises = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, (loc % 4) / _ScreenParams.xy).rg;
			float2  offset;
			float2  horizons = float2(-1.0, -1.0);

			float radius = (RADIUS * GetAspectRatio()) / vpos.z;
			radius = max(NUM_STEPS, radius);

			float stepsize	= radius / NUM_STEPS;
			float phi		= (params.x + noises.x) * PI;
			float ao		= 0.0;
			//float division	= noises.y * stepsize;
			//float currstep	= 1.0 + division + 0.25 * stepsize * params.y;
			float dist2, invdist, falloff, cosh;

			dir = float3(cos(phi), sin(phi), 0.0);
			horizons = float2(-1.0, -1.0);

			UNITY_LOOP
			for (int k = 0; k < NUM_DIRECTIONS; ++k) 
			{
				phi = float(k) * ((noises.x * PI) / NUM_DIRECTIONS);
				float division	= noises.y * stepsize;
				float currstep	= 1.0 + division + 0.25 * stepsize;

				dir = float3(cos(phi), sin(phi), 0.0);
				horizons = float2(-1.0, -1.0);
				// calculate horizon angles
				UNITY_LOOP
				for (int j = 0; j < NUM_STEPS; ++j) 
				{
					offset = round(dir.xy * currstep) * 0.005;

					// h1
					s = GetViewPosition(uv.xy + offset, currstep);
					ws = s.xyz - vpos.xyz;

					dist2 = dot(ws, ws);
					invdist = rsqrt(dist2);
					cosh = invdist * dot(ws, vdir);

					falloff = Falloff(dist2, cosh);
					horizons.x = max(horizons.x, cosh - falloff);

					// h2
					s = GetViewPosition(uv.xy - offset, currstep);
					ws = s.xyz - vpos.xyz;

					dist2 = dot(ws, ws);
					invdist = rsqrt(dist2);
					cosh = invdist * dot(ws, vdir);

					falloff = Falloff(dist2, cosh);
					horizons.y = max(horizons.y, cosh - falloff);

					// increment
					currstep += stepsize;
				}

				horizons = acos(horizons);


				// calculate gamma
				float3 bitangent	= normalize(cross(dir, vdir));
				float3 tangent		= cross(vdir, bitangent);
				float3 nx			= vnorm - bitangent * dot(vnorm, bitangent);

				float nnx		= length(nx);
				float invnnx	= 1.0 / (nnx + 1e-6);			// to avoid division with zero
				float cosxi		= dot(nx, tangent) * invnnx;	// xi = gamma + HALF_PI
				float gamma		= acos(cosxi) - HALF_PI;
				float cosgamma	= dot(nx, vdir) * invnnx;
				float singamma2	= -2.0 * cosxi;					// cos(x + HALF_PI) = -sin(x)

				// clamp to normal hemisphere
				horizons.x = gamma + max(-horizons.x - gamma, -HALF_PI);
				horizons.y = gamma + min(horizons.y - gamma, HALF_PI);

				// Riemann integral is additive
				ao += nnx * 1.0 * (
					(horizons.x * singamma2 + cosgamma - cos(2.0 * horizons.x - gamma)) +
					(horizons.y * singamma2 + cosgamma - cos(2.0 * horizons.y - gamma)));
			}

			ao = ao / float(NUM_DIRECTIONS);
			//ao = pow(ao, 2);
			return float4( ao, ao, ao, 1 );
		}

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vertex
                #pragma fragment Frag

            ENDHLSL
        }
    }
}