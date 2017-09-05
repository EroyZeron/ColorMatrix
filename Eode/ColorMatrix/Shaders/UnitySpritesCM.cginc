#ifndef EODE_UNITY_SPRITES_COLOR_MATRIX_INCLUDED
#define EODE_UNITY_SPRITES_COLOR_MATRIX_INCLUDED

#include "UnitySprites.cginc"
#include "ColorMatrix.cginc"

float4x4 _ColorMatrix;
sampler2D _SubMatrices;
float _SubMatricesCount;
float _MainMatrixPosition;

bool _HasMask = false;
sampler2D _Mask;

fixed4 SpriteFragCM(v2f IN) : SV_Target
{
    fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;

    if (!_HasMask)
    {
        c.rgb = ApplyColorMatrix(c.rgb, _ColorMatrix, _SubMatricesCount, _MainMatrixPosition, _SubMatrices);
    }
    else
    {
        fixed3 nc = ApplyColorMatrix(c.rgb, _ColorMatrix, _SubMatricesCount, _MainMatrixPosition, _SubMatrices);
        fixed4 mcolor = tex2D(_Mask, IN.texcoord);
        mcolor.rgb *= mcolor.a;

        c.r = c.r*(1.0-mcolor.r) + nc.r*mcolor.r;
        c.g = c.g*(1.0-mcolor.g) + nc.g*mcolor.g;
        c.b = c.b*(1.0-mcolor.b) + nc.b*mcolor.b;
    }

    c.rgb *= c.a;
    return c;
}

#endif