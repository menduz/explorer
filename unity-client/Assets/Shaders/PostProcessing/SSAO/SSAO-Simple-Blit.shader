Shader "Hidden/Custom/SSAO-Simple-Blit"
{
    HLSLINCLUDE

        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_BlurTexture2, sampler_BlurTexture2);
        TEXTURE2D_SAMPLER2D(_BlurTexture, sampler_BlurTexture);
		TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
		TEXTURE2D_SAMPLER2D(_AmbientOcclusionTexture, sampler_AmbientOcclusionTexture);
		

		
		float2 _NoiseResolution;
		float _Intensity;
		float _BlurAmount;
		float _DebugMode;

		float _GaussianCenter;
		float4 _Color;

		float readDepth( float2 uv )
		{
			float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
			return depth;
		}

		float compareDepths(float depth1, float depth2)
		{
			float depthDiff = clamp((depth1 - depth2) * 100.0, 0, 100.0);
			
			// Reduce left bell width to avoid self-shadowing
			if (depthDiff < _GaussianCenter)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		float blurSample(float2 uvCenter, float2 uvSample, float weight, inout float discCount)
		{
			float discontinuityFactor = compareDepths(readDepth(uvCenter), readDepth(uvSample));
			
			if ( discontinuityFactor < 1 )
				return float(0);
			
			discCount += weight;
			return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvSample).r * weight;
		}

		//NOTE(Brian): taken from https://github.com/Jam3/glsl-fast-gaussian-blur/blob/master/9.glsl
		float blur9(float2 uv, float2 resolution, float2 direction, inout float discCount) 
		{
			float color = 0;
			float2 off1 = direction * 1.3846153846;
			float2 off2 = direction * 3.2307692308;
			color += blurSample( uv, uv, 0.2270270270, discCount );
			color += blurSample( uv, uv + (off1 / resolution), 0.3162162162, discCount );
			color += blurSample( uv, uv - (off1 / resolution), 0.3162162162, discCount );
			color += blurSample( uv, uv + (off2 / resolution), 0.0702702703, discCount );
			color += blurSample( uv, uv - (off2 / resolution), 0.0702702703, discCount );
			color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * (1 - discCount);
			return color;
		}

		float4 blur13(float2 uv, float2 resolution, float2 direction, inout float discCount) 
		{
			float4 color = 0;
			float2 off1 = direction * 1.411764705882353;
			float2 off2 = direction * 3.2941176470588234;
			float2 off3 = direction * 5.176470588235294;
		  
			color += blurSample( uv, uv, 0.1964825501511404, discCount );
			color += blurSample( uv, uv + (off1 / resolution), 0.2969069646728344, discCount);
			color += blurSample( uv, uv - (off1 / resolution), 0.2969069646728344, discCount);
			color += blurSample( uv, uv + (off2 / resolution), 0.09447039785044732, discCount);
			color += blurSample( uv, uv - (off2 / resolution), 0.09447039785044732, discCount);
			color += blurSample( uv, uv + (off3 / resolution), 0.010381362401148057, discCount);
			color += blurSample( uv, uv - (off3 / resolution), 0.010381362401148057, discCount);
			color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * (1 - discCount);
		  
			return color;
		}

        float4 FragBlurH(VaryingsDefault input) : SV_Target
		{
			float2 uv = input.texcoordStereo;
			float discCount = 0;

			float blur = blur9(uv, _NoiseResolution, float2(1 * _BlurAmount, 0), discCount);

			return float4(blur, blur, blur, 1);
		}

        float4 FragBlurV(VaryingsDefault input) : SV_Target
		{
			float2 uv = input.texcoordStereo;
			float discCount = 0;

			float blur = blur9(uv, _NoiseResolution, float2(0, 1 * _BlurAmount), discCount);

			return float4(blur, blur, blur, 1);
		}

        float4 Frag(VaryingsDefault input) : SV_Target
        {
			float2 uv = input.texcoordStereo;
			float4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
			float ao = SAMPLE_TEXTURE2D(_BlurTexture2, sampler_BlurTexture2, uv).r;

			//NOTE(Brian): We have to encode it this way because the AO is a RFloat texture
			float4 finalAo = pow( float4(ao, ao, ao, 1), _Intensity );

			if ( _DebugMode > 0 )
				return finalAo;

			float4 occludedColor = lerp( _Color, originalColor, finalAo.r );
			return lerp(originalColor, occludedColor, 1 / ao); 
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

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlurH

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlurV

            ENDHLSL
        }
    }
}