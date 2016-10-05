using UnityEngine;
using System.Collections;

namespace EODE {
    public class RotateHueTest : MonoBehaviour {
        ColorMatrixBase _cmb;

    	void Start () {
            _cmb = GetComponent<ColorMatrixBase>();
            _cmb.DORotation(180f, 15f);
    	}

        void Update() {
            _cmb.UpdateMatrix();
        }
    }
}