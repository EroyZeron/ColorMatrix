using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace EODE {
    public class ColorMatrixBase : MonoBehaviour {
        public ColorMatrix ColorMatrix = null;

        public virtual void UpdateMatrix(){}
        public virtual void UpdateMatrix(Matrix4x4 parentm){}

        #region Setters tweening
        public void DOSaturation(float value, float duration) { ColorMatrix.DOSaturation(value, duration); }
        public void DOLightness(float value, float duration) { ColorMatrix.DOLightness(value, duration); }
        public void DORotation(float value, float duration) { ColorMatrix.DORotation(value, duration); }
        public void DOSwapRatio(float value, float duration) { ColorMatrix.DOSwapRatio(value, duration); }

        public void DOMatrixRatio(int index, float value, float duration) {
            ColorMatrix.DOMatrixRatio(index, value, duration);
        }

        public void DORanges(Vector4 value, float duration) { ColorMatrix.DORanges(value, duration); }
        #endregion

        #region Utils
        protected ColorMatrixBase GetParentMatrix() {
            if (!transform.parent)
                return null;

            ColorMatrixBase[] cmbs = GetComponentsInParent<ColorMatrixBase>();

            for (int i = 0; i < cmbs.Length; ++i)
            {
                if (cmbs[i] != this && cmbs[i].ColorMatrix != null)
                    return cmbs[i];
            }

            return null;
        }
        #endregion
    }
}