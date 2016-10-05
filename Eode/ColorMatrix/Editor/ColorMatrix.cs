using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(EODE.ColorMatrix))]
public class ColorMatrixEditor : Editor {
    enum swapColorPopup{
        Red,
        Green,
        Blue
    }

    int _foldoutMatrix = -1;
    Texture2D _default = null;
    Texture2D _preview = null;
    Texture2D _previewSelected = null;

    List<Texture2D> _previews = new List<Texture2D>();
    List<EODE.ColorMatrix> _submatrices = new List<EODE.ColorMatrix>();
    int _matrixSelected = -1;

    EODE.ColorMatrix _old = null;

    #region Utils
    float _debug = 0f;

    void DebugT(string log) {
        if (_debug < Time.realtimeSinceStartup)
        {
            _debug = Time.realtimeSinceStartup + 0.1f;

            Debug.Log(log);
        }
    }

    string CutFloat(float f) {
        if (f < 0.01f)
        {
            if (f < 0f)
                return f.ToString();
            else
                return "0";
        }

        string resp = f.ToString();

        if (resp.Length > 4)
            resp = resp.Substring(0, 4);

        return resp;
    }

    // value1 = 1/3 width
    Color GetPreviewColorBase(int i, float mult) {
        float value1;

            /*value1 = 33.3333f * mult;
            float value2 = value1 * 2;
            return new Color(
                (i <= value1 ? ((value1 - i) * 1f) / value1 : (i > value2) ? ((i - value2) * 1f) / value1 : 0f),
                (i <= value1 ? i / value1 : i <= value2 ? 1f - ((i - value1) / value1) : 0f),
                (i >= value2 ? 1f - ((i - value2) / value1) : i > value1 ? (i - value1) / value1 : 0f)
            );*/

        value1 = 8.3333f * mult;
        float tmp = 0f;

        if (i <= value1)
        {
            return new Color(
                i / value1,
                0f,
                0f
            );
        }
        if (i <= value1*2)
        {
            tmp = (value1 * 2 - i) / value1;
            return new Color(
                1f,
                1f-tmp,
                1f-tmp
            );
        }
        if (i <= value1*3)
        {
            tmp = (i - value1 * 2) / value1;
            return new Color(
                1f,
                1f,
                1f-tmp
            );
        }
        if (i <= value1*4)
        {
            return new Color(
                (value1*4 - i) / value1,
                (value1*4 - i) / value1,
                0f
            );
        }
        if (i <= value1*5)
        {
            return new Color(
                0f,
                (i - value1*4) / value1,
                0f
            );
        }
        if (i <= value1*6)
        {
            tmp = (value1 * 6 - i) / value1;
            return new Color(
                1f-tmp,
                1f,
                1f-tmp
            );
        }
        if (i <= value1*7)
        {
            tmp = (i - value1 * 6) / value1;
            return new Color(
                1f-tmp,
                1f,
                1f
            );
        }
        if (i <= value1*8)
        {
            return new Color(
                0f,
                (value1*8 - i) / value1,
                (value1*8 - i) / value1
            );
        }
        if (i <= value1*9)
        {
            return new Color(
                0f,
                0f,
                (i - value1*8) / value1
            );
        }
        if (i <= value1*10)
        {
            tmp = (value1 * 10 - i) / value1;
            return new Color(
                1f-tmp,
                1f-tmp,
                1f
            );
        }
        if (i <= value1*11)
        {
            tmp = (i - value1 * 10) / value1;
            return new Color(
                1f,
                1f-tmp,
                1f
            );
        }
        if (i <= value1*12)
        {
            return new Color(
                (value1*12 - i) / value1,
                0f,
                (value1*12 - i) / value1
            );
        }


        return new Color(1f, 1f, 1f);
    }
    #endregion

    #region textures preview
    Color SubMatrixApply(Color c, EODE.ColorMatrix cm) {
        if (cm.Matrix[3, 3] > 0.001f && cm.Matrix[3, 3] < 0.999f)
        {
            float rat = GetRatioColorRange(new Color(c.r, c.g, c.b), cm.Matrix.GetColumn(3));

            float r2 = c.r * cm.Matrix[0, 0] + c.g * cm.Matrix[1, 0] + c.b * cm.Matrix[2, 0] + cm.Matrix[3, 0];
            float g2 = c.r * cm.Matrix[0, 1] + c.g * cm.Matrix[1, 1] + c.b * cm.Matrix[2, 1] + cm.Matrix[3, 1];
            float b2 = c.r * cm.Matrix[0, 2] + c.g * cm.Matrix[1, 2] + c.b * cm.Matrix[2, 2] + cm.Matrix[3, 2];

            c.r = c.r * (1f - rat) + r2 * rat;
            c.g = c.g * (1f - rat) + g2 * rat;
            c.b = c.b * (1f - rat) + b2 * rat;
        }

        return c;
    }

    Color GetColor(Color c, EODE.ColorMatrix cm, bool noChildren=false) {
        float r = c.r*cm.Matrix[0,0] + c.g*cm.Matrix[1,0] + c.b*cm.Matrix[2,0] + cm.Matrix[3,0];
        float g = c.r*cm.Matrix[0,1] + c.g*cm.Matrix[1,1] + c.b*cm.Matrix[2,1] + cm.Matrix[3,1];
        float b = c.r*cm.Matrix[0,2] + c.g*cm.Matrix[1,2] + c.b*cm.Matrix[2,2] + cm.Matrix[3,2];

        if (cm.ColorMatrices.Count > 0 && !noChildren)
        {
            for (int i = 0; i < _submatrices.Count; ++i)
            {
                Color tmp = SubMatrixApply(new Color(r, g, b), _submatrices[i]);

                r = tmp.r;
                g = tmp.g;
                b = tmp.b;
            }
        }

        return new Color(r, g, b);
    }

    float GetRatioColorRange(Color colorRGB, Vector4 filter) {
        if (filter.w < 0.001f || filter.w > 0.999f) return filter.w;

        float red = Mathf.Abs(colorRGB.r - filter.x);
        float green = Mathf.Abs(colorRGB.g - filter.y);
        float blue = Mathf.Abs(colorRGB.b - filter.z);

        float alpha = (1 / filter.w);

        return Mathf.Clamp(alpha-((red + green + blue) * alpha), 0f, 1f);
    }

    void UpdateTexture(EODE.ColorMatrix cm, float width=500f, int height=15) {
        bool createDefault = _default == null;

        float mult = width/100f;
        _preview = new Texture2D(Mathf.RoundToInt(100f*mult), height);
        _previewSelected = new Texture2D(Mathf.RoundToInt((100f*mult)/1.5f), height);

        if (createDefault)
        {
            _default = new Texture2D(Mathf.RoundToInt(100f*mult), height);
        }

        Color pxl;
        Color pxlm;
        Color pxlmp;
        for (int i = 0; i < 100*mult; ++i)
        {
            pxl = GetPreviewColorBase(i, mult);
            pxlmp = GetColor(pxl, cm, true);
            pxlm = GetColor(pxl, cm);

            for (int y = 0; y < height; ++y)
            {
                if (createDefault) _default.SetPixel(i, y, pxl);
                _preview.SetPixel(i, y, pxlm);
                _previewSelected.SetPixel(Mathf.RoundToInt(i / 1.5f), y, pxlmp);
            }
        }

        if (createDefault) _default.Apply();
        _preview.Apply();
        _previewSelected.Apply();
    }

    void TextureGen(Texture2D texture, EODE.ColorMatrix cm, float width=500f, int height=15) {
        float mult = width/100f;

        Color pxl;
        Color pxlm;
        for (int i = 0; i < 100 * mult; ++i)
        {
            pxl = GetPreviewColorBase(i, mult);
            pxlm = SubMatrixApply(pxl, cm);

            for (int y = 0; y < height; ++y)
            {
                texture.SetPixel(i, y, pxlm);
            }
        }

        texture.Apply();
    }
    #endregion

    #region GUI
    void DrawMatrixPreview(int index, EODE.ColorMatrix colorMatrix) {
        EditorGUILayout.BeginHorizontal();

        if (index < 0)
        {
            if (GUILayout.Toggle(_matrixSelected < 0, GUIContent.none))
                _matrixSelected = -1;

            GUILayout.Label(_previewSelected);
            GUI.color = Color.white;
            GUILayout.Label("All");
        }
        else
        {
            if (_submatrices.Count <= index)
            {
                _submatrices.Add(colorMatrix.SubMatrixGen(index));
                _submatrices[_submatrices.Count-1].UpdateMatrix();

                float mult = ((EditorGUIUtility.currentViewWidth*0.85f)/2f)/100f;
                _previews.Add(new Texture2D(Mathf.RoundToInt(100f*mult), 15));
                TextureGen(_previews[index], _submatrices[index], (EditorGUIUtility.currentViewWidth*0.85f)/2f, 15);
            }

            if (GUILayout.Toggle(_matrixSelected == index, GUIContent.none))
                _matrixSelected = index;

            if (_previews.Count > index)
                GUILayout.Label(_previews[index]);
            else
                GUILayout.Label("");


            Color newVal = EditorGUILayout.ColorField(new Color(colorMatrix.ColorMatrices[index].Ranges.x, colorMatrix.ColorMatrices[index].Ranges.y, colorMatrix.ColorMatrices[index].Ranges.z), GUILayout.Width(50));
            float newSup = EditorGUILayout.Slider(colorMatrix.ColorMatrices[index].Ranges.w, 0.002f, 0.98f, GUILayout.Width(60));

            colorMatrix.ColorMatrices[index].Ranges = new Vector4(newVal.r, newVal.g, newVal.b, newSup);
            if (colorMatrix.ColorMatrices[index].Ranges != _submatrices[index].Ranges)
            {
                _submatrices[index].Ranges = new Vector4(newVal.r, newVal.g, newVal.b, newSup);

                _submatrices[index].UpdateMatrix();
                TextureGen(_previews[index], _submatrices[index], (EditorGUIUtility.currentViewWidth*0.85f)/2f, 15);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    public override void OnInspectorGUI() {
        EODE.ColorMatrix colorMatrix = (EODE.ColorMatrix)target;

        #region start
        if (_old == null)
        {
            _old = colorMatrix.Clone();
        }

        while (colorMatrix.MatrixRatios.Count < colorMatrix.Matrices.Count)
        {
            colorMatrix.MatrixRatios.Add(1f);
        }

        for (int i = 0; i < _submatrices.Count; ++i)
        {
            while (_submatrices[i].MatrixRatios.Count < _submatrices[i].Matrices.Count)
            {
                _submatrices[i].MatrixRatios.Add(1f);
            }
        }

        colorMatrix.UpdateMatrix();
        UpdateTexture(colorMatrix, EditorGUIUtility.currentViewWidth*0.85f, 15);
        #endregion

        #region preview
        EditorGUILayout.LabelField("Preview", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label(_default);
        GUILayout.Label(_preview);

        EditorGUILayout.Space();
        #endregion

        #region Selection
        EditorGUILayout.LabelField("Selection", EditorStyles.centeredGreyMiniLabel);

        for (int i = 0; i < colorMatrix.ColorMatrices.Count; ++i)
        {
            if (!colorMatrix.ColorMatrices[i].Priority)
            {
                DrawMatrixPreview(i, colorMatrix);
            }
        }

        DrawMatrixPreview(-1, colorMatrix);

        for (int i = 0; i < colorMatrix.ColorMatrices.Count; ++i)
        {
            if (colorMatrix.ColorMatrices[i].Priority)
            {
                DrawMatrixPreview(i, colorMatrix);
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUI.color = Color.cyan;
        if (GUILayout.Button("+", GUILayout.Width(50)))
        {
            colorMatrix.ColorMatrices.Add(new EODE.ColorMatrix.SubMatrixData());
        }

        if (_matrixSelected >= 0)
        {
            GUI.color = Color.yellow;
            if (GUILayout.Button("-", GUILayout.Width(50)))
            {
                colorMatrix.ColorMatrices.RemoveAt(_matrixSelected);
                _submatrices.RemoveAt(_matrixSelected);
                _previews.RemoveAt(_matrixSelected);
                _matrixSelected = -1;
            }
            else
            {
                GUI.color = Color.white;

                if (colorMatrix.ColorMatrices[_matrixSelected].Priority || _matrixSelected > 0)
                {
                    if (GUILayout.Button("Up", GUILayout.Width(50)))
                    {
                        if (colorMatrix.ColorMatrices[_matrixSelected].Priority && (_matrixSelected == 0 || !colorMatrix.ColorMatrices[_matrixSelected-1].Priority))
                        {
                            colorMatrix.ColorMatrices[_matrixSelected].Priority = false;
                        }
                        else if (_matrixSelected > 0)
                        {
                            colorMatrix.ColorMatrices.Insert(_matrixSelected-1, colorMatrix.ColorMatrices[_matrixSelected]);
                            _submatrices.Insert(_matrixSelected-1, _submatrices[_matrixSelected]);
                            _previews.Insert(_matrixSelected-1, _previews[_matrixSelected]);

                            colorMatrix.ColorMatrices.RemoveAt(_matrixSelected+1);
                            _submatrices.RemoveAt(_matrixSelected+1);
                            _previews.RemoveAt(_matrixSelected+1);

                            _matrixSelected -= 1;
                        }
                    }
                }

                if (!colorMatrix.ColorMatrices[_matrixSelected].Priority || _matrixSelected < colorMatrix.ColorMatrices.Count-1)
                {
                    if (GUILayout.Button("Down", GUILayout.Width(50)))
                    {
                        if (!colorMatrix.ColorMatrices[_matrixSelected].Priority && (_matrixSelected == colorMatrix.ColorMatrices.Count-1 || colorMatrix.ColorMatrices[_matrixSelected+1].Priority))
                        {
                            colorMatrix.ColorMatrices[_matrixSelected].Priority = true;
                        }
                        else if (_matrixSelected < colorMatrix.ColorMatrices.Count-1)
                        {
                            colorMatrix.ColorMatrices.Insert(_matrixSelected+2, colorMatrix.ColorMatrices[_matrixSelected]);
                            _submatrices.Insert(_matrixSelected+2, _submatrices[_matrixSelected]);
                            _previews.Insert(_matrixSelected+2, _previews[_matrixSelected]);

                            colorMatrix.ColorMatrices.RemoveAt(_matrixSelected);
                            _submatrices.RemoveAt(_matrixSelected);
                            _previews.RemoveAt(_matrixSelected);

                            _matrixSelected += 1;
                        }
                    }
                }
            }
        }

        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        #endregion

        #region standard
        EODE.ColorMatrix selectedMatrix = colorMatrix;
        if (_matrixSelected >= 0 && _matrixSelected < _submatrices.Count)
        {
            selectedMatrix = _submatrices[_matrixSelected];
            selectedMatrix.UpdateMatrix();
        }

        EditorGUILayout.LabelField("Standard", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.LabelField("Hue");
        selectedMatrix.HueRotation = (float)EditorGUILayout.IntSlider(Mathf.RoundToInt(selectedMatrix.HueRotation), -180, 180);

        EditorGUILayout.LabelField("Lightness");
        selectedMatrix.Lightness = (EditorGUILayout.IntSlider(Mathf.RoundToInt((selectedMatrix.Lightness)*100), -100, 100)/100f);

        EditorGUILayout.LabelField("Saturation");
        selectedMatrix.Saturation = (EditorGUILayout.IntSlider(Mathf.RoundToInt((selectedMatrix.Saturation-1f)*100), -100, 100)/100f)+1f;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Effects", EditorStyles.centeredGreyMiniLabel);
        #endregion

        #region Swap
        bool changeSwap = EditorGUILayout.Toggle("Swap RGB", selectedMatrix.Swap.Length > 0);
        if (changeSwap)
        {
            if (selectedMatrix.Swap.Length == 0)
                selectedMatrix.Swap = EODE.ColorMatrix.SwapType.RGB;

            selectedMatrix.SwapRatio = EditorGUILayout.Slider("Ratio", selectedMatrix.SwapRatio, 0f, 1f);

            swapColorPopup r = (swapColorPopup)EditorGUILayout.EnumPopup("Red to", 
                selectedMatrix.Swap[0] == 0 ? swapColorPopup.Red : selectedMatrix.Swap[0] == 1 ? swapColorPopup.Green : swapColorPopup.Blue
            );
            swapColorPopup g = (swapColorPopup)EditorGUILayout.EnumPopup("Green to", 
                selectedMatrix.Swap[1] == 0 ? swapColorPopup.Red : selectedMatrix.Swap[1] == 1 ? swapColorPopup.Green : swapColorPopup.Blue
            );
            swapColorPopup b = (swapColorPopup)EditorGUILayout.EnumPopup("Blue to", 
                selectedMatrix.Swap[2] == 0 ? swapColorPopup.Red : selectedMatrix.Swap[2] == 1 ? swapColorPopup.Green : swapColorPopup.Blue
            );

            selectedMatrix.Swap = EODE.ColorMatrix.SwapType.Make(r.ToString()[0].ToString() + g.ToString()[0] + b.ToString()[0]);

            EditorGUILayout.Space();
        }
        else if( selectedMatrix.Swap.Length > 0 )
        {
            selectedMatrix.Swap = new int[0];
        }
        #endregion

        #region Others
        selectedMatrix.Inversion = EditorGUILayout.Toggle("Inversion", selectedMatrix.Inversion);
        #endregion

        #region Matrices
        Matrix4x4 msepia = EODE.ColorMatrix.MakeMatrix4x4(new float[]{
            0.393f, 0.349f, 0.272f, 0f,
            0.769f, 0.686f, 0.534f, 0f,
            0.189f, 0.168f, 0.131f, 0f,
            0f, 0f, 0f, 1f
        });

        Matrix4x4 mbaw = EODE.ColorMatrix.MakeMatrix4x4(new float[]{
            1.5f, 1.5f, 1.5f, 0f,
            1.5f, 1.5f, 1.5f, 0f,
            1.5f, 1.5f, 1.5f, 0f,
            -1f, -1f, -1f, 1f
        });

        bool sepia = selectedMatrix.Matrices.Contains(msepia);
        bool baw = selectedMatrix.Matrices.Contains(mbaw);


        bool sendSepia = EditorGUILayout.Toggle("Sepia", sepia);
        bool sendBaw = EditorGUILayout.Toggle("Black and White", baw);

        if (sendSepia != sepia)
        {
            if (selectedMatrix.Matrices.Contains(msepia))
                selectedMatrix.Matrices.Remove(msepia);
            else
                selectedMatrix.Matrices.Add(msepia);
        }

        if (sendBaw != baw)
        {
            if (selectedMatrix.Matrices.Contains(mbaw))
                selectedMatrix.Matrices.Remove(mbaw);
            else
                selectedMatrix.Matrices.Add(mbaw);
        }

        EditorGUILayout.Space();
        #endregion

        #region Show matrices
        EditorGUILayout.LabelField("Matrices", EditorStyles.centeredGreyMiniLabel);

        bool opened = false;
        int deleteElement = -1;
        Color matcolor = Color.white;
        for (int i = 0; i < selectedMatrix.Matrices.Count; ++i)
        {
            if (_foldoutMatrix == i)
                opened = true;
            else
                opened = false;

            if (selectedMatrix.Matrices[i] == Matrix4x4.identity)
                matcolor = Color.grey;
            else if (selectedMatrix.Matrices[i] == Matrix4x4.zero)
                matcolor = Color.cyan;
            else if (selectedMatrix.Matrices[i] == msepia)
                matcolor = Color.yellow;
            else if (selectedMatrix.Matrices[i] == mbaw)
                matcolor = new Color(1f, 0.4f, 0);

            GUILayout.BeginHorizontal();
            opened = EditorGUILayout.Foldout(
                opened,
                "Matrix "+
                (
                    selectedMatrix.Matrices[i] == msepia ? "Sepia" :
                    selectedMatrix.Matrices[i] == mbaw ? "Black and White" :
                    i.ToString()
                )
            );

            GUI.color = matcolor;
            GUILayout.Box(GUIContent.none, GUILayout.Width(5), GUILayout.Height(15));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();


            if (opened)
            {
                _foldoutMatrix = i;

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                selectedMatrix.MatrixRatios[i] = (EditorGUILayout.IntSlider("Ratio", Mathf.RoundToInt(selectedMatrix.MatrixRatios[i] * 100), 0, 100)/100f);

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                Vector4 v0 = selectedMatrix.Matrices[i].GetRow(0);
                GUI.color = new Color(1f, 0.7f, 0.7f);
                v0.x = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(0).x, GUILayout.Width(65));
                GUI.color = new Color(1f, 1f, 0.7f);
                v0.y = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(0).y, GUILayout.Width(65));
                GUI.color = new Color(1f, 0.7f, 1f);
                v0.z = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(0).z, GUILayout.Width(65));
                GUI.color = Color.grey;
                v0.w = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(0).w, GUILayout.Width(65));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                Vector4 v1 = selectedMatrix.Matrices[i].GetRow(1);
                GUI.color = new Color(1f, 1f, 0.7f);
                v1.x = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(1).x, GUILayout.Width(65));
                GUI.color = new Color(0.7f, 1f, 0.7f);
                v1.y = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(1).y, GUILayout.Width(65));
                GUI.color = new Color(0.7f, 1f, 1f);
                v1.z = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(1).z, GUILayout.Width(65));
                GUI.color = Color.grey;
                v1.w = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(1).w, GUILayout.Width(65));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                Vector4 v2 = selectedMatrix.Matrices[i].GetRow(2);
                GUI.color = new Color(1f, 0.7f, 1f);
                v2.x = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(2).x, GUILayout.Width(65));
                GUI.color = new Color(0.7f, 1f, 1f);
                v2.y = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(2).y, GUILayout.Width(65));
                GUI.color = new Color(0.7f, 0.7f, 1f);
                v2.z = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(2).z, GUILayout.Width(65));
                GUI.color = Color.grey;
                v2.w = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(2).w, GUILayout.Width(65));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                Vector4 v3 = selectedMatrix.Matrices[i].GetRow(3);
                GUI.color = Color.white;
                GUI.color = new Color(1f, 1f, 1f);
                v3.x = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(3).x, GUILayout.Width(65));
                v3.y = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(3).y, GUILayout.Width(65));
                v3.z = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(3).z, GUILayout.Width(65));
                GUI.color = Color.grey;
                v3.w = EditorGUILayout.FloatField(GUIContent.none, selectedMatrix.Matrices[i].GetRow(3).w, GUILayout.Width(65));
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                selectedMatrix.Matrices[i] = EODE.ColorMatrix.MakeMatrix4x4(new float[]{
                    v0.x, v0.y, v0.z, v0.w,
                    v1.x, v1.y, v1.z, v1.w,
                    v2.x, v2.y, v2.z, v2.w,
                    v3.x, v3.y, v3.z, v3.w
                });

                EditorGUILayout.Space();

                GUI.color = Color.red;
                if (GUILayout.Button("Remove Matrix", GUILayout.Width(100)))
                {
                    deleteElement = i;
                    _foldoutMatrix = -1;
                }
                GUI.color = Color.white;

                GUILayout.EndVertical();

                GUI.color = matcolor;
                GUILayout.Box(GUIContent.none, GUILayout.Width(5), GUILayout.ExpandHeight(true));
                GUI.color = Color.white;

                GUILayout.EndHorizontal();
            }
            else if (_foldoutMatrix == i)
            {
                _foldoutMatrix = -1;
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
            GUI.color = Color.green;
            if (GUILayout.Button("+", GUILayout.Width(50)))
            {
                selectedMatrix.Matrices.Add(Matrix4x4.identity);
                selectedMatrix.MatrixRatios.Add(1f);
            }

            GUI.color = Color.white;
            if (_matrixSelected < 0 && GUILayout.Button("Reset", GUILayout.Width(100)))
            {
                selectedMatrix.Saturation = _old.Saturation;
                selectedMatrix.Lightness = _old.Lightness;
                selectedMatrix.HueRotation = _old.HueRotation;
                selectedMatrix.SwapRatio = _old.SwapRatio;
                selectedMatrix.Swap = _old.Swap;
                selectedMatrix.Inversion = _old.Inversion;
                selectedMatrix.Matrices = _old.Matrices;
                selectedMatrix.MatrixRatios = _old.MatrixRatios;
            }

            GUI.color = Color.red;
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                selectedMatrix.Saturation = 1f;
                selectedMatrix.Lightness = 0f;
                selectedMatrix.HueRotation = 0;
                selectedMatrix.SwapRatio = 1f;
                selectedMatrix.Swap = new int[0];
                selectedMatrix.Inversion = false;
                selectedMatrix.Matrices.Clear();
                selectedMatrix.MatrixRatios.Clear();
            }
            GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        if (deleteElement >= 0)
        {
            selectedMatrix.Matrices.RemoveAt(deleteElement);
            selectedMatrix.MatrixRatios.RemoveAt(deleteElement);
        }
        #endregion

        #region Result
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Result", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
                GUI.color = Color.grey;
                for (int i = 0; i < 4; ++i)
                {
                    EditorGUILayout.LabelField(
                        CutFloat(selectedMatrix.Matrix[i,0])+", "+
                        CutFloat(selectedMatrix.Matrix[i,1])+", "+
                        CutFloat(selectedMatrix.Matrix[i,2])+", "+
                        CutFloat(selectedMatrix.Matrix[i,3]),
                        EditorStyles.whiteLabel
                    );
                }
                GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
                if (GUILayout.Button("Copy Matrix"))
                {
                    GUIUtility.systemCopyBuffer = "{"+
                        selectedMatrix.Matrix[0,0]+", "+selectedMatrix.Matrix[0,1]+", "+selectedMatrix.Matrix[0,2]+", "+selectedMatrix.Matrix[0,3]+", \n"+
                        selectedMatrix.Matrix[1,0]+", "+selectedMatrix.Matrix[1,1]+", "+selectedMatrix.Matrix[1,2]+", "+selectedMatrix.Matrix[1,3]+", \n"+
                        selectedMatrix.Matrix[2,0]+", "+selectedMatrix.Matrix[2,1]+", "+selectedMatrix.Matrix[2,2]+", "+selectedMatrix.Matrix[2,3]+", \n"+
                        selectedMatrix.Matrix[3,0]+", "+selectedMatrix.Matrix[3,1]+", "+selectedMatrix.Matrix[3,2]+", "+selectedMatrix.Matrix[3,3]+
                        "}";
                }
                
                GUI.color = Color.green;
                if (GUILayout.Button("Add & Reset"))
                {
                    Matrix4x4 matr = selectedMatrix.Matrix;
                            
                    selectedMatrix.Saturation = 1f;
                    selectedMatrix.Lightness = 0f;
                    selectedMatrix.HueRotation = 0;
                    selectedMatrix.SwapRatio = 1f;
                    selectedMatrix.Swap = new int[0];
                    selectedMatrix.Inversion = false;
                    selectedMatrix.Matrices.Clear();
                    selectedMatrix.MatrixRatios.Clear();

                    selectedMatrix.Matrices.Add(matr);
                    selectedMatrix.MatrixRatios.Add(1f);
                }
                GUI.color = Color.white;
            EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        #endregion

        if (_matrixSelected >= 0)
        {
            colorMatrix.SubMatrixUpdate(_matrixSelected, selectedMatrix);

            while (selectedMatrix.MatrixRatios.Count < selectedMatrix.Matrices.Count)
            {
                selectedMatrix.MatrixRatios.Add(1f);
            }

            selectedMatrix.UpdateMatrix();
            TextureGen(_previews[_matrixSelected], selectedMatrix, (EditorGUIUtility.currentViewWidth*0.85f)/2f, 15);
        }

        EditorUtility.SetDirty(colorMatrix);
    }
    #endregion
}