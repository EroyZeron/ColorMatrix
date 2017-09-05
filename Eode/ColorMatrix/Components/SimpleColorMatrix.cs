using UnityEngine;
using System.Collections;

namespace EODE {
    [ExecuteInEditMode]
    public class SimpleColorMatrix : ColorMatrixBase {
        public Material SpriteMaterial;

        protected MaterialPropertyBlock _props;
        protected Renderer _renderer;

        void Awake() {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                if (GetComponent<SpriteRenderer>() != null)
                    _renderer.material = SpriteMaterial;

                _props = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_props);
            }
        }

        #if UNITY_EDITOR
        void Update() {
            if (Application.isPlaying) return;

            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                if (GetComponent<SpriteRenderer>() != null)
                    _renderer.material = SpriteMaterial;
                _props = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_props);
            }
        }

        void OnRenderObject() {
            if (Application.isPlaying || GetParentMatrix() != null) return;

            UpdateMatrix();
        }
        #endif

        void Start() {
            if (GetParentMatrix() != null)
            {
                return;
            }

            UpdateMatrix();
        }

        public override void UpdateMatrix() {
            // update color matrix values
            if (ColorMatrix != null)
            {
                ColorMatrix.UpdateMatrix();

                if (ColorMatrix != null && _props != null)
                {
                    _props.SetMatrix("_ColorMatrix", ColorMatrix.Matrix);
                    if (ColorMatrix.SubTexture != null)
                    {
                        _props.SetTexture("_SubMatrices", ColorMatrix.SubTexture);
                        _props.SetFloat("_SubMatricesCount", ColorMatrix.ColorMatrices.Count);
                        _props.SetFloat("_MainMatrixPosition", ColorMatrix.MainMatrixPosition);
                    }
                    else
                    {
                        _props.SetFloat("_SubMatricesCount", 0);
                    }
                    _renderer.SetPropertyBlock(_props);
                }
            }

            // update children
            ColorMatrixBase[] cms = GetComponentsInChildren<ColorMatrixBase>();
            for (int i = 0; i < cms.Length; ++i)
            {
                if (cms[i].gameObject == gameObject || !cms[i].isActiveAndEnabled)
                    continue;

                if(ColorMatrix != null)
                    cms[i].UpdateMatrix(ColorMatrix.Matrix);
                else
                    cms[i].UpdateMatrix();
            }
        }

        public override void UpdateMatrix(Matrix4x4 parentm) {
            if (ColorMatrix != null)
                ColorMatrix.UpdateMatrix();

            Matrix4x4 cm = (ColorMatrix != null ? EODE.ColorMatrix.Matrixmult4x4(ColorMatrix.Matrix, parentm) : parentm);

            // update color matrix values
            if (ColorMatrix != null && _props != null)
            {
                _props.SetMatrix("_ColorMatrix", cm);
                if (ColorMatrix.SubTexture != null)
                {
                    _props.SetTexture("_SubMatrices", ColorMatrix.SubTexture);
                    _props.SetFloat("_SubMatricesCount", ColorMatrix.ColorMatrices.Count);
                    _props.SetFloat("_MainMatrixPosition", ColorMatrix.MainMatrixPosition);
                }
                else
                {
                    _props.SetFloat("_SubMatricesCount", 0);
                }
                _renderer.SetPropertyBlock(_props);
            }

            // update children
            ColorMatrixBase[] cms = GetComponentsInChildren<ColorMatrixBase>();
            for (int i = 0; i < cms.Length; ++i)
            {
                if (cms[i].gameObject == gameObject || !cms[i].isActiveAndEnabled)
                    continue;

                cms[i].UpdateMatrix(cm);
            }

            // no matrix, last child -> take parent matrix ... TODO
            //if (ColorMatrix == null && cms.Length == 0) {}
        }
    }
}