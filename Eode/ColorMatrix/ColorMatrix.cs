using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace EODE {
    [CreateAssetMenu(fileName = "ColorMatrix", menuName = "ColorMatrix", order = 350)]
    public class ColorMatrix : ScriptableObject {
        #region Values
        public bool Local = false;
        public float Saturation = 1f;
        public float Lightness = 0f;
        public float HueRotation = 0f;
        public float SwapRatio = 1f;
        public int[] Swap = new int[0];
        public bool Inversion = false;
        public List<Matrix4x4> Matrices = new List<Matrix4x4>();
        public List<float> MatrixRatios = new List<float>();
        public Vector4 Ranges = Vector4.zero; 
        public List<SubMatrixData> ColorMatrices = new List<SubMatrixData>();
        #endregion

        #region export
        [System.Serializable]
        public class SubMatrixData {
            public bool Priority = true; // before/after all
            public float Saturation = 1f;
            public float Lightness = 0f;
            public float HueRotation = 0f;
            public float SwapRatio = 1f;
            public int[] Swap = new int[0];
            public bool Inversion = false;
            public List<Matrix4x4> Matrices = new List<Matrix4x4>();
            public List<float> MatrixRatios = new List<float>();
            public Vector4 Ranges = Vector4.zero;
        }

        public ColorMatrix SubMatrixGen(int index) {
            if (index < ColorMatrices.Count)
            {
                ColorMatrix cm = ScriptableObject.CreateInstance<ColorMatrix>();
                cm.Saturation = ColorMatrices[index].Saturation;
                cm.Lightness = ColorMatrices[index].Lightness;
                cm.HueRotation = ColorMatrices[index].HueRotation;
                cm.SwapRatio = ColorMatrices[index].SwapRatio;
                cm.Swap = ColorMatrices[index].Swap;
                cm.Inversion = ColorMatrices[index].Inversion;
                cm.Matrices = ColorMatrices[index].Matrices;
                cm.MatrixRatios = ColorMatrices[index].MatrixRatios;
                cm.Ranges = ColorMatrices[index].Ranges;
                cm.Local = true;

                return cm;
            }

            return null;
        }

        public void SubMatrixUpdate(int index, ColorMatrix cm) {
            if (index < ColorMatrices.Count)
            {
                ColorMatrices[index].Saturation = cm.Saturation;
                ColorMatrices[index].Lightness = cm.Lightness;
                ColorMatrices[index].HueRotation = cm.HueRotation;
                ColorMatrices[index].SwapRatio = cm.SwapRatio;
                ColorMatrices[index].Swap = cm.Swap;
                ColorMatrices[index].Inversion = cm.Inversion;
                ColorMatrices[index].Matrices = cm.Matrices;
                ColorMatrices[index].MatrixRatios = cm.MatrixRatios;
                ColorMatrices[index].Ranges = cm.Ranges;
            }
        }
        #endregion

        #region Constants
        static readonly float[] _luminance = {0.3086f, 0.6094f, 0.0820f};
        #endregion

        #region static functions
        public static Matrix4x4 MakeMatrix4x4(float[] values) {
            Matrix4x4 mat = Matrix4x4.zero;

            if (values.Length == 4)
            {
                for (int y = 0; y < 4; ++y)
                {
                    mat[y,0] = values[y];
                    mat[y,1] = values[y];
                    mat[y,2] = values[y];
                    mat[y,3] = values[y];
                }
            }
            else
            {
                for (int y = 0; y < 4; ++y)
                {
                    for (int x = 0; x < 4; ++x)
                    {
                        mat[y,x] = values[y*4+x];
                        mat[y,x] = values[y*4+x];
                        mat[y,x] = values[y*4+x];
                        mat[y,x] = values[y*4+x];
                    }
                }
            }

            return mat;
        }

        public static Matrix4x4 Matrixmult4x4(Matrix4x4 a, Matrix4x4 b) {
            int x, y;
            Matrix4x4 response = Matrix4x4.zero;

            for(y=0; y<4 ; y++)
                for(x=0 ; x<4 ; x++) {
                    response[y,x] = b[y,0] * a[0,x]
                        + b[y,1] * a[1,x]
                        + b[y,2] * a[2,x]
                        + b[y,3] * a[3,x];
                }

            return response;
        }

        public static Matrix4x4 RatioMatrix4x4(Matrix4x4 mat, float ratio=1f) {
            if (ratio == 0f)
                return mat;

            mat.m00 = mat.m00+((1f-mat.m00)*(1f-ratio));
            mat.m01 *= ratio;
            mat.m02 *= ratio;
            mat.m10 *= ratio;
            mat.m11 = mat.m11+((1f-mat.m11)*(1f-ratio));
            mat.m12 *= ratio;
            mat.m20 *= ratio;
            mat.m21 *= ratio;
            mat.m22 = mat.m22+((1f-mat.m22)*(1f-ratio));

            mat.m30 = mat.m30 + (-mat.m30 * (1f - ratio));
            mat.m31 = mat.m30 + (-mat.m30 * (1f - ratio));
            mat.m32 = mat.m30 + (-mat.m30 * (1f - ratio));

            return mat;
        }

        public static class SwapType {
            public static int[] RGB = {0,1,2};
            public static int[] RBG = {0,2,1};
            public static int[] BGR = {2,1,0};
            public static int[] BRG = {2,0,1};
            public static int[] GRB = {1,0,2};
            public static int[] GBR = {1,2,0};

            public static int[] Make(string value) {
                int[] response = { 0, 0, 0 };

                for (int i = 0; i < 3; ++i)
                {
                    response[i] = value[i] == 'R' ? 0 : value[i] == 'G' ? 1 : 2;
                }

                return response;
            }
        }
        #endregion

        #region Private
        Matrix4x4 _matrix = Matrix4x4.identity;
        Texture2D _subTexture = null;
        int _mainMatrixPosition = 0;

        static Matrix4x4 Xrotatemat(Matrix4x4 mat, float rs, float rc) {
            Matrix4x4 mmat = Matrix4x4.identity;

            mmat.m11 = rc;
            mmat.m12 = rs;
            mmat.m21 = -rs;
            mmat.m22 = rc;

            return Matrixmult4x4(mmat, mat);
        }

        static Matrix4x4 Yrotatemat(Matrix4x4 mat, float rs, float rc) {
            Matrix4x4 mmat = Matrix4x4.identity;

            mmat.m00 = rc;
            mmat.m02 = -rs;
            mmat.m20 = rs;
            mmat.m22 = rc;

            return Matrixmult4x4(mmat, mat);
        }

        static Matrix4x4 Zrotatemat(Matrix4x4 mat, float rs, float rc) {
            Matrix4x4 mmat = Matrix4x4.identity;

            mmat.m00 = rc;
            mmat.m01 = rs;
            mmat.m10 = -rs;
            mmat.m11 = rc;

            return Matrixmult4x4(mmat, mat);
        }

        static Matrix4x4 Zshearmat(Matrix4x4 mat, float dx, float dy) {
            Matrix4x4 mmat = Matrix4x4.identity;

            mmat.m02 = dx;
            mmat.m12 = dy;

            return Matrixmult4x4(mmat, mat);
        }

        static Vector3 Xformpnt(Matrix4x4 mat, Vector3 point) {
            return new Vector3(
                point.x*mat.m00 + point.y*mat.m10 + point.z*mat.m20 + mat.m30,
                point.x*mat.m01 + point.y*mat.m11 + point.z*mat.m21 + mat.m31,
                point.x*mat.m02 + point.y*mat.m12 + point.z*mat.m22 + mat.m32
            );
        }
        #endregion

        #region Public
        public Matrix4x4 Matrix{
            get{ return _matrix; }
            private set{ }
        }

        public Texture2D SubTexture{
            get{ return _subTexture; }
            private set{ }
        }

        public int MainMatrixPosition{
            get{ return _mainMatrixPosition; }
            private set{ }
        }

        public void UpdateMatrix() {
            _matrix = Matrix4x4.identity;

            if (Saturation != 1f)
                Saturate(Saturation);

            if (Lightness != 0f)
                Lighten(Lightness);

            if (HueRotation != 0f)
                HueRotate(HueRotation);

            if (Swap.Length != 0)
                SwapRGBTo(Swap, SwapRatio);

            if (Inversion)
                Invert();

            for (int i = 0; i < Matrices.Count; ++i)
            {
                if (MatrixRatios[i] > 0f)
                    Matrixmult(RatioMatrix4x4(Matrices[i], MatrixRatios[i]));
            }

            if (Ranges != Vector4.zero)
            {
                _matrix[0,3] = Ranges.x;
                _matrix[1,3] = Ranges.y;
                _matrix[2,3] = Ranges.z;
                _matrix[3,3] = Ranges.w;
            }


            // subtexture
            if (ColorMatrices.Count > 0)
            {
                _subTexture = new Texture2D(4, ColorMatrices.Count);

                ColorMatrix cm;
                _mainMatrixPosition = -1;
                for (int y = 0; y < ColorMatrices.Count; ++y)
                {
                    if (_mainMatrixPosition < 0 && ColorMatrices[y].Priority)
                    {
                        _mainMatrixPosition = y;
                    }

                    cm = SubMatrixGen(y);
                    cm.UpdateMatrix();

                    for (int x = 0; x < 4; ++x)
                    {
                        // store column = pixel
                        _subTexture.SetPixel(x, y, new Color((cm.Matrix[0,x]*0.25f)+0.5f, (cm.Matrix[1,x]*0.25f)+0.5f, (cm.Matrix[2,x]*0.25f)+0.5f, (cm.Matrix[3,x]*0.25f)+0.5f));
                    }
                }

                if (_mainMatrixPosition < 0)
                {
                    _mainMatrixPosition = ColorMatrices.Count;
                }

                _subTexture.Apply();
            }
        }

        public ColorMatrix Clone() {
            ColorMatrix colorMatrix = CreateInstance<ColorMatrix>();
            colorMatrix.Saturation = Saturation;
            colorMatrix.Lightness = Lightness;
            colorMatrix.HueRotation = HueRotation;
            colorMatrix.SwapRatio = SwapRatio;
            colorMatrix.Swap = (int[])Swap.Clone();
            colorMatrix.Inversion = Inversion;

            for (int i = 0; i < Matrices.Count; ++i)
            {
                colorMatrix.Matrices.Add(Matrices[i]);
            }
            for (int i = 0; i < MatrixRatios.Count; ++i)
            {
                colorMatrix.MatrixRatios.Add(MatrixRatios[i]);
            }

            for (int i = 0; i < ColorMatrices.Count; ++i)
            {
                colorMatrix.ColorMatrices.Add(ColorMatrices[i]);
            }

            return colorMatrix;
        }

        public void Matrixmult(Matrix4x4 mat) {
            int x, y;
            Matrix4x4 result = Matrix4x4.zero;

            for(y=0; y<4 ; y++)
                for(x=0 ; x<4 ; x++) {
                    result[y,x] = mat[y,0] * _matrix[0,x]
                        + mat[y,1] * _matrix[1,x]
                        + mat[y,2] * _matrix[2,x]
                        + mat[y,3] * _matrix[3,x];
                }

            _matrix = result;
        }

        public void Scale(float sca) {
            Matrix4x4 mmat = Matrix4x4.identity;

            mmat.m00 = sca; // red
            mmat.m11 = sca; // green
            mmat.m22 = sca; // blue

            Matrixmult(mmat);
        }

        public void Lighten(float light) {
            Matrix4x4 mmat = Matrix4x4.identity;

            mmat.m30 = light;
            mmat.m31 = light;
            mmat.m32 = light;

            Matrixmult(mmat);
        }

        public void Saturate(float sat) {
            Matrix4x4 mmat = Matrix4x4.identity;

            float sl0 = (1f - sat) * _luminance[0];
            float sl1 = (1f - sat) * _luminance[1];
            float sl2 = (1f - sat) * _luminance[2];

            mmat.m00 = (sl0 + sat);
            mmat.m01 = sl0;
            mmat.m02 = sl0;
            mmat.m10 = sl1;
            mmat.m11 = (sl1 + sat);
            mmat.m12 = sl1;
            mmat.m20 = sl2;
            mmat.m21 = sl2;
            mmat.m22 = (sl2 + sat);

            Matrixmult(mmat);
        }

        public void HueRotate(float rot, bool simple=true) {
            Matrix4x4 mmat = Matrix4x4.identity;

            float mag;
            float xrs, xrc;
            float yrs, yrc;
            float zrs, zrc;
            float zsx=0f, zsy=0f;

            /* rotate the grey vector into positive Z */
            mag = Mathf.Sqrt(2f);
            xrs = 1f/mag;
            xrc = 1f/mag;
            mmat = Xrotatemat(mmat,xrs,xrc);

            mag = Mathf.Sqrt(3f);
            yrs = -1f/mag;
            yrc = Mathf.Sqrt(2f)/mag;
            mmat = Yrotatemat(mmat,yrs,yrc);

            /* shear the space to make the luminance plane horizontal */
            if (!simple)
            {
                Vector3 l = Xformpnt(mmat, new Vector3(_luminance[0], _luminance[1], _luminance[2]));
                zsx = l.x / l.z;
                zsy = l.y / l.z;
                mmat = Zshearmat(mmat, zsx, zsy);
            }

            /* rotate the hue */
            zrs = Mathf.Sin(rot*Mathf.Deg2Rad);
            zrc = Mathf.Cos(rot*Mathf.Deg2Rad);
            mmat = Zrotatemat(mmat,zrs,zrc);

            /* unshear the space to put the luminance plane back */
            if (!simple)
            {
                mmat = Zshearmat(mmat, -zsx, -zsy);
            }

            /* rotate the grey vector back into place */
            mmat = Yrotatemat(mmat,-yrs,yrc);
            mmat = Xrotatemat(mmat,-xrs,xrc);

            Matrixmult(mmat);
        }

        public void SwapRGBTo(int[] swapType, float ratio=1f) {
            if (ratio == 0f)
                return;

            Matrix4x4 mmat = Matrix4x4.identity;
            float temp;

            for (int i = 0; i < 3; ++i)
            {
                temp = mmat[i, swapType[i]]*(1f-ratio) + mmat[i,i]*ratio;
                mmat[i, i] = mmat[i, i]*(1f-ratio) + mmat[i, swapType[i]]*ratio;
                mmat[i, swapType[i]] = temp;
            }

            Matrixmult(mmat);
        }

        public void Invert(float ratio=1f) {
            if (ratio == 0f)
                return;

            Matrix4x4 mmat = Matrix4x4.identity;

            mmat.m00 = 1f-ratio*2f;
            mmat.m11 = 1f-ratio*2f;
            mmat.m22 = 1f-ratio*2f;
            mmat.m30 = ratio;
            mmat.m31 = ratio;
            mmat.m32 = ratio;

            Matrixmult(mmat);
        }

        public void Sepia(float ratio=1f) {
            if (ratio == 0f)
                return;

            Matrix4x4 mmat = MakeMatrix4x4(new float[]{
                0.393f, 0.349f, 0.272f, 0f,
                0.769f, 0.686f, 0.534f, 0f,
                0.189f, 0.168f, 0.131f, 0f,
                0f, 0f, 0f, 1f
            });

            Matrixmult(RatioMatrix4x4(mmat, ratio));
        }

        public void BlackAndWhite(float ratio=1f) {
            if (ratio == 0f)
                return;

            Matrix4x4 mmat = MakeMatrix4x4(new float[]{
                1.5f, 1.5f, 1.5f, 0f,
                1.5f, 1.5f, 1.5f, 0f,
                1.5f, 1.5f, 1.5f, 0f,
                -1f, -1f, -1f, 1f
            });

            Matrixmult(RatioMatrix4x4(mmat, ratio));
        }
        #endregion

        #region Getters
        public float GetSaturation() { return Saturation; }
        public float GetLightness() { return Lightness; }
        public float GetRotation() { return HueRotation; }
        public float GetSwapRatio() { return SwapRatio; }
        public int[] GetSwap() { return Swap; }
        public bool GetInversion() { return Inversion; }

        public List<Matrix4x4> GetMatrices() { return Matrices; }
        public Matrix4x4 GetMatrix(int index) { return index < Matrices.Count ? Matrices[index] : Matrix4x4.identity; }
        public List<float> GetMatrixRatios() { return MatrixRatios; }
        public float GetMatrixRatio(int index) { return index < Matrices.Count ? MatrixRatios[index] : 0f; }
        // tmp, return +index*1000
        public float GetMatrixRatioHack(int index) { return GetMatrixRatio(index)+(index*1000f); }

        public Vector4 GetRanges() { return Ranges; }
        #endregion

        #region Setters
        public void SetSaturation(float value) { Saturation = value; }
        public void SetLightness(float value) { Lightness = value; }
        public void SetRotation(float value) { HueRotation = value; }
        public void SetSwapRatio(float value) { SwapRatio = value; }
        public void SetSwap(int[] value) { Swap = value; }
        public void SetInversion(bool value) { Inversion = value; }

        public void SetMatrices(List<Matrix4x4> value) { Matrices = value; }
        public void SetMatrix(int index, Matrix4x4 value) { if (index < Matrices.Count) Matrices[index] = value; }
        public void SetMatrixRatios(List<float> value) { MatrixRatios = value; }
        public void SetMatrixRatio(int index, float value) { if (index < Matrices.Count) MatrixRatios[index] = value; }
        // tmp, index in value
        public void SetMatrixRatioHack(float value) {
            int index = Mathf.FloorToInt(value / 1000f);
            value = value - index*1000f;
            SetMatrixRatio(index, value);
        }

        public void SetRanges(Vector4 value) { Ranges = value; }
        #endregion

        #region Setters tweening
        public void DOSaturation(float value, float duration) { DOTween.To(GetSaturation, SetSaturation, value, duration); }
        public void DOLightness(float value, float duration) { DOTween.To(GetLightness, SetLightness, value, duration); }
        public void DORotation(float value, float duration) { DOTween.To(GetRotation, SetRotation, value, duration); }
        public void DOSwapRatio(float value, float duration) { DOTween.To(GetSwapRatio, SetSwapRatio, value, duration); }

        public void DOMatrixRatio(int index, float value, float duration) {
            DOTween.To(SetMatrixRatioHack, GetMatrixRatioHack(index), value+((float)index*1000f), duration);
        }

        public void DORanges(Vector4 value, float duration) { DOTween.To(GetRanges, SetRanges, value, duration); }
        #endregion
    	
    }
}
