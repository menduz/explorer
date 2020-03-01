Shader "Hidden/Custom/SSAO-Simple"
{
    HLSLINCLUDE

        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		#define E 2.71828182845904523536 // Eulers number
		#define GOLDEN_ANGLE 2.39996322972865332 // PI * (3.0 - sqrt(5.0)) radians. See: https://en.wikipedia.org/wiki/Golden_angle
		#define SAMPLES 8
        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

		float2 _NoiseResolution;
		float _MinDepth; // Depth clamp, reduces haloing at screen edges
		float _Radius; // AO radius
		float _NoiseAmount; // Noise amount
		float _GaussianCenter; // Gauss bell center
		float _GaussianSize; // Gauss bell width
		float _SelfShadowReductionFactor; // Self-shadowing reduction
		
		float readDepth( float2 uv )
		{
			float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
			return depth;
		}

		float compareDepths(float depth1, float depth2, inout int hasDiscontinuity)
		{
			float depthDiff = clamp((depth1 - depth2) * 100.0, 0, 100.0);
			float gaussianSize = _GaussianSize;
			float gaussianCenter = _GaussianCenter;

			// Reduce left bell width to avoid self-shadowing
			if (depthDiff < gaussianCenter)
			{
				gaussianSize = _SelfShadowReductionFactor;
			}
			else
			{
				hasDiscontinuity = 1;
			}

			float finalDiff = (depthDiff - gaussianCenter);
			float gauss = pow(E, -2.0 * finalDiff * finalDiff / (gaussianSize * gaussianSize));
			return gauss;
		}

		// Noise generation for dithering
		float2 rand(float2 coord)
		{
			float noiseX = dot(coord, float2(12.9898, 78.233));
			float noiseY = dot(coord, float2(12.9898, 78.233) * 2.0);
			float2 noise = clamp(frac(sin(float2(noiseX, noiseY)) * 43758.5453), 0.0, 1.0);
			return (noise * 2.0 - 1.0) * _NoiseAmount;
		}

		float estimateAO(float2 uv, float depth, float offsetX, float offsetY)
		{
			float radiusFactor = _Radius - depth * _Radius;
			float2 offset = float2(offsetX, offsetY);
	
			int hasDiscontinuity = 0;

			float2 coord1 = uv + radiusFactor * offset;
			float result = compareDepths(depth, readDepth(coord1), hasDiscontinuity);
	
			// Linear extrapolation to guess a second layer of depth at a discontinuity
			if (hasDiscontinuity > 0)
			{
				float2 coord2 = uv - radiusFactor * offset;
				float tempResult = compareDepths(depth, readDepth(coord2), hasDiscontinuity);
				result += (1.0 - result) * tempResult;
			}

			return result;
		}

        float4 Frag(VaryingsDefault input) : SV_Target
        {
			float2 uv = input.texcoordStereo;

			// Convert depth value to linear space
			float depth = readDepth(uv);
			float ao = 1.0;
	
			if(depth < 1) // Avoid doing SSAO on sky
			{
				float clampedDepth = clamp(depth, _MinDepth, 1.0);
		
				float2 noise = rand(uv);
				float sampleFactorX = (1.0 / _NoiseResolution.x) / clampedDepth + (noise.x * (1.0 - noise.x));
				float sampleFactorY = (1.0 / _NoiseResolution.y) / clampedDepth + (noise.y * (1.0 - noise.y));
		
				// Gets the average estimated AO across sample points on a sphere using fibonacci spiral method
				float zDelta = 1.0 / float(SAMPLES);
				float z = 1.0 - zDelta / 2.0;
				float angle = 0.0;

				UNITY_LOOP
				for (int i = 0; i <= SAMPLES; i++)
				{
					float distance = sqrt(1.0 - z);
					float sampleX = cos(angle) * distance;
					float sampleY = sin(angle) * distance;
					ao += estimateAO(uv, depth, sampleX * sampleFactorX, sampleY * sampleFactorY);

					angle = angle + GOLDEN_ANGLE;
					z = z - zDelta;
				}

				ao /= float(SAMPLES);
				ao = 1 - ao;
				return float4(float3(ao, ao, ao), 1.0);
			}
	
			return 1;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}