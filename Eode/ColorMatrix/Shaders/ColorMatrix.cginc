#ifndef EODE_COLOR_MATRIX_INCLUDED
#define EODE_COLOR_MATRIX_INCLUDED

inline float GetRatioColorRange(fixed3 colorRGB, float4 filter) {
    float red = abs(colorRGB.r - filter.x);
    float green = abs(colorRGB.g - filter.y);
    float blue = abs(colorRGB.b - filter.z);

    float alpha = (1.0 / filter.w);

    return clamp(alpha-((red + green + blue) * alpha), 0.0, 1.0);
}

inline float4 transformValue(float4 column) {
	return float4((column[0]-0.5)*4.0, (column[1]-0.5)*4.0, (column[2]-0.5)*4.0, (column[3]-0.5)*4.0);
}

fixed3 SubMatrixApply(fixed3 c, float4 matcolumn1, float4 matcolumn2, float4 matcolumn3, float4 matcolumn4) {
    if (matcolumn4.w > 0.001 && matcolumn4.w < 0.999)
    {
        float rat = GetRatioColorRange(c, matcolumn4);

        float r2 = c.r * matcolumn1[0] + c.g * matcolumn1[1] + c.b * matcolumn1[2] + matcolumn1[3];
        float g2 = c.r * matcolumn2[0] + c.g * matcolumn2[1] + c.b * matcolumn2[2] + matcolumn2[3];
        float b2 = c.r * matcolumn3[0] + c.g * matcolumn3[1] + c.b * matcolumn3[2] + matcolumn3[3];

        c.r = c.r * (1.0 - rat) + r2 * rat;
        c.g = c.g * (1.0 - rat) + g2 * rat;
        c.b = c.b * (1.0 - rat) + b2 * rat;
    }

    return c;
}

fixed3 ApplyColorMatrix(fixed3 colorRGB, float4x4 mat) {
	float r = colorRGB.r*mat[0][0] + colorRGB.g*mat[1][0] + colorRGB.b*mat[2][0] + mat[3][0];
	float g = colorRGB.r*mat[0][1] + colorRGB.g*mat[1][1] + colorRGB.b*mat[2][1] + mat[3][1];
	float b = colorRGB.r*mat[0][2] + colorRGB.g*mat[1][2] + colorRGB.b*mat[2][2] + mat[3][2];

	return fixed3(r, g, b);
}

fixed3 ApplyColorMatrix(fixed3 colorRGB, float4x4 mat, float subMatricesCount, float mainMatrixPosition, sampler2D subMatrices) {
	fixed3 result = fixed3(colorRGB.r, colorRGB.g, colorRGB.b);

	if (subMatricesCount > 0.0)
	{
		float lineHeight = 1.0/subMatricesCount;

		int counter = 0.0;
		bool mainapp = false;
		for (float y = lineHeight*0.5; y < 1.0; y += lineHeight)
		{
			if (!mainapp && counter >= mainMatrixPosition)
			{
				result = ApplyColorMatrix(result, mat);
				mainapp = true;
			}

			result = SubMatrixApply(result,
				transformValue(tex2D(subMatrices, float2(0.125, y))),
				transformValue(tex2D(subMatrices, float2(0.375, y))),
				transformValue(tex2D(subMatrices, float2(0.625, y))),
				transformValue(tex2D(subMatrices, float2(0.875, y)))
			);
			counter += 1.0;
		}

		if (!mainapp)
		{
			result = ApplyColorMatrix(result, mat);
		}
	}
	else
	{
		result = ApplyColorMatrix(result, mat);
	}

	return result;
}

inline fixed3 ApplyColorMatrixr1(fixed3 colorRGB, float4x4 mat, float ratio) {
	return (colorRGB*(1.0-(ratio)))
		+(ApplyColorMatrix(colorRGB, mat)*ratio);
}

inline fixed3 ApplyColorMatrix2(fixed3 colorRGB, float4x4 mat1, float4x4 mat2, float ratio) {
	return (ApplyColorMatrix(colorRGB, mat1)*ratio)
		+(ApplyColorMatrix(colorRGB, mat2)*(1.0-ratio));
}

inline fixed3 ApplyColorMatrix2r2(fixed3 colorRGB, float4x4 mat1, float4x4 mat2, float2 ratios) {
	return (colorRGB*(1.0-(ratios.x+ratios.y)))
		+(ApplyColorMatrix(colorRGB, mat1)*ratios.x)
		+(ApplyColorMatrix(colorRGB, mat2)*ratios.y);
}

inline fixed3 ApplyColorMatrix3(fixed3 colorRGB, float4x4 mat1, float4x4 mat2, float4x4 mat3, float3 ratios) {
	return (colorRGB*(1.0-(ratios.x+ratios.y+ratios.z)))
		+(ApplyColorMatrix(colorRGB, mat1)*ratios.x)
		+(ApplyColorMatrix(colorRGB, mat2)*ratios.y)
		+(ApplyColorMatrix(colorRGB, mat3)*ratios.z);
}

#endif