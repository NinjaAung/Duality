// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using UnityEditor;
using AmbientSounds.Internal;
using PWCommon2;

/*
 * Custom PropertyDrawer for SliderRange class
 */

namespace AmbientSounds {
    [CustomPropertyDrawer(typeof(SliderRange))]
    public class SliderRangeEditor : PropertyDrawer {
        /// <summary> Are we currently dragging this slider? </summary>
        bool isDraggingThis = false;
        /// <summary> Starting mouse X Position for current edit operation </summary>
        float curSliderEditingStartPos = 0f;
        /// <summary> Starting "min" value for current edit operation </summary>
        float curSliderEditingStartMin = 0f;
        /// <summary> Starting "max" value for current edit operation </summary>
        float curSliderEditingStartMax = 0f;
        /// <summary> Type of edit we are making 0=None 1=All 2=Min 3=Max </summary>
        int curSliderEditingType = 0;

        /// <summary> Gets height needed to display property </summary>
        /// <param name="property">Property to display</param>
        /// <param name="label">Display GUIContent of Property</param>
        /// <returns>Height of 2 normal Editor lines</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return 2f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        /// <summary> Main GUI Function </summary>
        /// <param name="position">Rect to draw this property in</param>
        /// <param name="property">Property to edit</param>
        /// <param name="label">Display GUIContent of Property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            bool hasChanged = false;
            int originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginProperty(position, label, property);
            GUISkin editorSkin = null;
            if (Event.current.type == EventType.Repaint)
                editorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

            SerializedProperty nameProp = property.FindPropertyRelative("m_name");
            SerializedProperty startProp = property.FindPropertyRelative("m_min");
            SerializedProperty endProp = property.FindPropertyRelative("m_max");
            SerializedProperty startFalloffProp = property.FindPropertyRelative("m_minFalloff");
            SerializedProperty endFalloffProp = property.FindPropertyRelative("m_maxFalloff");
            SerializedProperty invertProp = property.FindPropertyRelative("m_invert");

            Vector2 nameSize = new Vector2(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            Rect nameRect = new Rect(position.position, nameSize);
            nameRect.xMin += originalIndentLevel * 15f;
            float minMaxWidth = Mathf.Min(80f, ((position.width - nameRect.width) * 0.3f + 10f) * 0.5f);
            Rect minRect = new Rect(nameRect.xMax, position.y, minMaxWidth, nameSize.y);
            Rect maxRect = new Rect(position.xMax - minMaxWidth, position.y, minMaxWidth, nameSize.y);

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(nameRect, nameProp.stringValue);
            if (string.IsNullOrEmpty(newName))
                EditorGUI.LabelField(nameRect, "Name", EditorStyles.centeredGreyMiniLabel);
            if (EditorGUI.EndChangeCheck()) {
                hasChanged = true;
                nameProp.stringValue = newName;
            }
            float start = startProp.floatValue, end = endProp.floatValue;
            EditorGUI.BeginChangeCheck();
            start = EditorGUI.FloatField(minRect, start);
            if (EditorGUI.EndChangeCheck()) {
                hasChanged = true;
                if (start > end)
                    start = end;
                startProp.floatValue = Mathf.Clamp01(start);
            }
            EditorGUI.BeginChangeCheck();
            end = EditorGUI.FloatField(maxRect, end);
            if (EditorGUI.EndChangeCheck()) {
                hasChanged = true;
                if (end < start)
                    end = start;
                endProp.floatValue = Mathf.Clamp01(end);
            }
            #region Slider
            Rect barRect = new Rect(minRect.xMax + 5f, position.y, maxRect.xMin - minRect.xMax - 10f, nameSize.y);
            float totalWidth = barRect.width - 11f;
            float totalHeight = barRect.height;
            Rect drawRect = new Rect(barRect.x + totalWidth * startProp.floatValue, barRect.y + totalHeight * 0.165f, totalWidth * (endProp.floatValue - startProp.floatValue) + 10f, totalHeight * 0.67f);
            //Check for Events
            Rect sliderPanRect = new Rect(drawRect.xMin + 5f, drawRect.y, drawRect.width - 10f, totalHeight * 0.67f);
            Rect sliderEndRect = new Rect(drawRect.xMax - 5f, drawRect.y, 5f, totalHeight * 0.67f);
            Rect sliderBeginRect = new Rect(drawRect.xMin, drawRect.y, 5f, totalHeight * 0.67f);
            #region Mouse Events
            EditorGUIUtility.AddCursorRect(sliderEndRect, MouseCursor.SplitResizeLeftRight);
            EditorGUIUtility.AddCursorRect(sliderBeginRect, MouseCursor.SplitResizeLeftRight);
            //check for drag of either ends or slider
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                if (sliderEndRect.Contains(Event.current.mousePosition)) {
                    isDraggingThis = true;
                    curSliderEditingStartPos = Event.current.mousePosition.x;
                    curSliderEditingStartMax = endProp.floatValue;
                    curSliderEditingType = 3;
                    Event.current.Use();
                } else if (sliderBeginRect.Contains(Event.current.mousePosition)) {
                    isDraggingThis = true;
                    curSliderEditingStartPos = Event.current.mousePosition.x;
                    curSliderEditingStartMin = startProp.floatValue;
                    curSliderEditingType = 2;
                    Event.current.Use();
                } else if (sliderPanRect.Contains(Event.current.mousePosition)) {
                    isDraggingThis = true;
                    curSliderEditingStartPos = Event.current.mousePosition.x;
                    curSliderEditingStartMin = startProp.floatValue;
                    curSliderEditingStartMax = endProp.floatValue;
                    curSliderEditingType = 1;
                    Event.current.Use();
                }
            } else if (Event.current.type == EventType.MouseDrag && isDraggingThis) {
                hasChanged = true;
                float moveAmount = (Event.current.mousePosition.x - curSliderEditingStartPos) / totalWidth;
                switch (curSliderEditingType) {
                    case 1:
                        moveAmount = Mathf.Clamp(moveAmount, -curSliderEditingStartMin, 1f - curSliderEditingStartMax);
                        startProp.floatValue = Mathf.Clamp(curSliderEditingStartMin + moveAmount, 0f, 1f);
                        endProp.floatValue = Mathf.Clamp(curSliderEditingStartMax + moveAmount, 0f, 1f);
                        break;
                    case 2:
                        startProp.floatValue = Mathf.Clamp(curSliderEditingStartMin + moveAmount, 0f, endProp.floatValue);
                        break;
                    case 3:
                        endProp.floatValue = Mathf.Clamp(curSliderEditingStartMax + moveAmount, startProp.floatValue, 1f);
                        break;
                    default:
                        break;
                }
                Event.current.Use();
            } else if (Event.current.type == EventType.MouseUp) {
                isDraggingThis = false;
                curSliderEditingType = 0;
                curSliderEditingStartPos = 0f;
                curSliderEditingStartMin = 0f;
                curSliderEditingStartMax = 0f;
            }
            #endregion
            //Draw slider (Repaint event only)
            if (Event.current.type == EventType.Repaint) {
                editorSkin.horizontalSlider.Draw(barRect, false, false, false, false);
                if (!invertProp.boolValue) { //normal display
                    editorSkin.button.Draw(drawRect, false, false, false, false);
                } else { //invert display
                    GUIStyle leftButton = editorSkin.GetStyle("ButtonLeft") ?? editorSkin.button;
                    GUIStyle rightButton = editorSkin.GetStyle("ButtonRight") ?? editorSkin.button;
                    Rect drawRect2 = drawRect;
                    drawRect2.xMin = drawRect.xMax - 5f;
                    drawRect2.xMax = barRect.xMax + 4f;
                    drawRect.xMax = drawRect.xMin + 5f;
                    drawRect.xMin = barRect.xMin - 5f;
                    if(startProp.floatValue > 0f)
                        rightButton.Draw(drawRect, false, false, false, false);
                    if(endProp.floatValue < 1f)
                        leftButton.Draw(drawRect2, false, false, false, false);
                }
            }
            #endregion

            nameRect.y += nameRect.height + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.BeginChangeCheck();
            invertProp.boolValue = EditorGUI.ToggleLeft(nameRect, invertProp.displayName, invertProp.boolValue);
            if (EditorGUI.EndChangeCheck())
                hasChanged = true;
            minRect.y = nameRect.y;
            GUI.Label(minRect, "Falloff");
            Rect falloffRect = new Rect(minRect.xMax, nameRect.y, (maxRect.xMax - minRect.xMax - EditorGUIUtility.standardVerticalSpacing) / 2f, nameSize.y);
            EditorGUI.BeginChangeCheck();
            startFalloffProp.floatValue = Mathf.Clamp01(EditorGUI.FloatField(falloffRect, startFalloffProp.floatValue));
            falloffRect.x += falloffRect.width + EditorGUIUtility.standardVerticalSpacing;
            endFalloffProp.floatValue = Mathf.Clamp01(EditorGUI.FloatField(falloffRect, endFalloffProp.floatValue));
            if(EditorGUI.EndChangeCheck())
                hasChanged = true;
            EditorGUI.EndProperty();
            EditorGUI.indentLevel = originalIndentLevel;
            if (hasChanged && MainWindowEditor.Instance != null)
                MainWindowEditor.Instance.Repaint();
        }
    }
}
