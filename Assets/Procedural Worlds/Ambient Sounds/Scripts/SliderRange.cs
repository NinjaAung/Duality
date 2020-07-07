// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;

/*
 * Contains information about a "Slider"'s min/max, falloff, and inversion settings for Sequence or Modifier objects
 */

namespace AmbientSounds {
    /// <summary>
    /// Custom type drawn in inspector as a Range with editable label
    /// </summary>
    [System.Serializable]
    public class SliderRange {
        /// <summary> Name of Slider </summary>
        public string m_name = "";
        /// <summary> Minimum value to apply at </summary>
        public float m_min = 0f;
        /// <summary> Maximum value to apply at </summary>
        public float m_max = 1f;
        /// <summary> Falloff leading from 0 up to Min </summary>
        public float m_minFalloff = 0f;
        /// <summary> Falloff leading from Max up to 1 </summary>
        public float m_maxFalloff = 0f;
        /// <summary> Should this slider's value be Inverted? </summary>
        public bool m_invert = false;

        /// <summary>
        /// Gets the value between 0 and 1 where 0 is val is outside of slider range and 1 is within taking falloff into account.
        /// </summary>
        /// <param name="val">Position along slider to check</param>
        /// <returns></returns>
        public float Eval(float val) {
            float ret = 0f;

            if (val >= m_min && val <= m_max)
                ret = 1f;
            else if (val < m_min)
                ret = Mathf.Clamp01((val - (m_min - m_minFalloff)) / m_minFalloff);
            else
                ret = 1f - Mathf.Clamp01((val - m_max) / m_maxFalloff);
            
            return m_invert ? 1f - ret : ret;
        }
    }
}