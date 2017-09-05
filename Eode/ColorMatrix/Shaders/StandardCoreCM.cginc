#ifndef EODE_STANDARD_CORE_COLOR_MATRIX_INCLUDED
#define EODE_STANDARD_CORE_COLOR_MATRIX_INCLUDED

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "ColorMatrix.cginc"

float4x4 _ColorMatrix;
sampler2D _SubMatrices;
float _SubMatricesCount;
float _MainMatrixPosition;

bool _HasMask = false;
sampler2D _Mask;

////////////////////

fixed3 ApplyColorMatrixCM(fixed3 c, float2 pos) {
	if (!_HasMask)
	{
		c.rgb = ApplyColorMatrix(c.rgb, _ColorMatrix, _SubMatricesCount, _MainMatrixPosition, _SubMatrices);
	}
	else
	{
		fixed3 nc = ApplyColorMatrix(c.rgb, _ColorMatrix, _SubMatricesCount, _MainMatrixPosition, _SubMatrices);
		fixed4 mcolor = tex2D(_Mask, pos);
		mcolor.rgb *= mcolor.a;

		c.r = c.r*(1.0-mcolor.r) + nc.r*mcolor.r;
		c.g = c.g*(1.0-mcolor.g) + nc.g*mcolor.g;
		c.b = c.b*(1.0-mcolor.b) + nc.b*mcolor.b;
	}

	return c;
}

#if UNITY_STANDARD_SIMPLE
	half4 fragForwardBaseSimpleInternalCM (VertexOutputBaseSimple i)
	{
		UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	    FragmentCommonData s = FragmentSetupSimple(i);

	    UnityLight mainLight = MainLightSimple(i, s);

	    #if !defined(LIGHTMAP_ON) && defined(_NORMALMAP)
	    half ndotl = saturate(dot(s.tangentSpaceNormal, i.tangentSpaceLightDir));
	    #else
	    half ndotl = saturate(dot(s.normalWorld, mainLight.dir));
	    #endif

	    //we can't have worldpos here (not enough interpolator on SM 2.0) so no shadow fade in that case.
	    half shadowMaskAttenuation = UnitySampleBakedOcclusion(i.ambientOrLightmapUV, 0);
	    half realtimeShadowAttenuation = SHADOW_ATTENUATION(i);
	    half atten = UnityMixRealtimeAndBakedShadows(realtimeShadowAttenuation, shadowMaskAttenuation, 0);

	    half occlusion = Occlusion(i.tex.xy);
	    half rl = dot(REFLECTVEC_FOR_SPECULAR(i, s), LightDirForSpecular(i, mainLight));

	    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);
	    half3 attenuatedLightColor = gi.light.color * ndotl;

	    half3 c = BRDF3_Indirect(s.diffColor, s.specColor, gi.indirect, PerVertexGrazingTerm(i, s), PerVertexFresnelTerm(i));
	    c += BRDF3DirectSimple(s.diffColor, s.specColor, s.smoothness, rl) * attenuatedLightColor;
	    c += Emission(i.tex.xy);

	    UNITY_APPLY_FOG(i.fogCoord, c);

	    c.rgb = ApplyColorMatrixCM(c.rgb, i.tex.xy);

	    return OutputForward (half4(c, 1), s.alpha);
	}

	half4 fragForwardAddSimpleInternalCM (VertexOutputForwardAddSimple i)
	{
		UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	    FragmentCommonData s = FragmentSetupSimpleAdd(i);

	    half3 c = BRDF3DirectSimple(s.diffColor, s.specColor, s.smoothness, dot(REFLECTVEC_FOR_SPECULAR(i, s), i.lightDir));

	    #if SPECULAR_HIGHLIGHTS // else diffColor has premultiplied light color
	        c *= _LightColor0.rgb;
	    #endif

	    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
	    c *= atten * saturate(dot(LightSpaceNormal(i, s), i.lightDir));

	    UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass

	    c.rgb = ApplyColorMatrixCM(c.rgb, i.tex.xy);

	    return OutputForward (half4(c, 1), s.alpha);
	}
#else
	half4 fragForwardBaseInternalCM (VertexOutputForwardBase i) {
		UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	    FRAGMENT_SETUP(s)

	    UNITY_SETUP_INSTANCE_ID(i);
	    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	    UnityLight mainLight = MainLight ();
	    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

	    half occlusion = Occlusion(i.tex.xy);
	    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

	    half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	    c.rgb += Emission(i.tex.xy);

	    UNITY_APPLY_FOG(i.fogCoord, c.rgb);

	    c.rgb = ApplyColorMatrixCM(c.rgb, i.tex.xy);

	    return OutputForward (c, s.alpha);
	}

	half4 fragForwardAddInternalCM (VertexOutputForwardAdd i)
	{
		UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

	    FRAGMENT_SETUP_FWDADD(s)

	    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
	    UnityLight light = AdditiveLight (IN_LIGHTDIR_FWDADD(i), atten);
	    UnityIndirect noIndirect = ZeroIndirect ();

	    half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);

	    UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass

	    c.rgb = ApplyColorMatrixCM(c.rgb, i.tex.xy);

	    return OutputForward (c, s.alpha);
	}
#endif

////////////////////

#if UNITY_STANDARD_SIMPLE
	#include "UnityStandardCoreForwardSimple.cginc"
	half4 fragBaseCM (VertexOutputBaseSimple i) : SV_Target { return fragForwardBaseSimpleInternalCM(i); }
	half4 fragAddCM (VertexOutputForwardAddSimple i) : SV_Target { return fragForwardAddSimpleInternalCM(i); }
#else
	#include "UnityStandardCore.cginc"
	half4 fragBaseCM (VertexOutputForwardBase i) : SV_Target { return fragForwardBaseInternalCM(i); }
	half4 fragAddCM (VertexOutputForwardAdd i) : SV_Target { return fragForwardAddInternalCM(i); }
#endif

#endif