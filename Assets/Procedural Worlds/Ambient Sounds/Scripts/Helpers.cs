// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;

/*
 * Set of Helper functions used in AmbientSounds system (some may no longer be used)
 */

namespace AmbientSounds {
    /// <summary>
    /// Contains helper functions used in Audio system
    /// </summary>
    public class Helpers {
        /// <summary>Interpolates starting at "begin" and ending at "begin + change" over "duration" seconds with current time "time" with optional EaseIn and/or EaseOut</summary>
        /// <param name="time">Current Time</param>
        /// <param name="begin">Starting Value</param>
        /// <param name="change">Change in Value</param>
        /// <param name="duration">Total Time for transition</param>
        /// <param name="easeIn">Do Quadratic Ease-In?</param>
        /// <param name="easeOut">Do Quadratic Ease-Out?</param>
        /// <returns>value along path between begin and begin+change following optional Quadratic Ease-In and/or Ease-Out</returns>
        public static double Interpolate(double time, double begin, double change, double duration, bool EaseIn = false, bool EaseOut = false) {
            if (duration <= 0f)
                duration = 0.0001f; //prevent Division By Zero
            time /= duration;
            if (EaseIn) {
                if (EaseOut) {
                    time *= 2.0;
                    if (time < 1f)
                        return change / 2.0 * time * time + begin;
                    time--;
                    return -change / 2.0 * (time * (time - 2.0) - 1.0) + begin;
                } else {
                    return change * time * time + begin;
                }
            } else if (EaseOut) {
                return -change * time * (time - 2) + begin;
            } else {
                return begin + change * time;
            }
        }
        /// <summary>
        /// Interpolates along data curve using cubic interpolation (makes for smoother waveforms)
        /// </summary>
        /// <param name="data">Array of datapoints to interpolate between</param>
        /// <param name="pos">Position along data path with 1.5 being halfway between data[1] and data[2]</param>
        /// <returns>floating point value at point along curve</returns>
        public static float CubicInterp(float[] data, double pos) {
            int posFloor = (int)pos;
            if (posFloor < 0) {
                posFloor = 0;
                pos = 0f;
            } else if (posFloor >= data.Length - 1) {
                pos = posFloor = data.Length - 1;
            }
            float posRem = (float)(pos - posFloor);
            if (posRem == 0f) { //position falls directly on a data point. no modification needed
                return data[posFloor];
            } else { //interpolate between two data points
                float pStart = data[posFloor]; //beginning point
                float pEnd = data[posFloor + 1]; //ending point
                float pBefore = (posFloor - 1 >= 0 ? data[posFloor - 1] : pStart + (pEnd - pStart)); //point before beginning (or a continuation of the slope between 1 and 2
                float pAfter = (posFloor + 2 < data.Length ? data[posFloor + 2] : pEnd + (pStart - pEnd)); //point after ending

                float d0 = pAfter - pEnd - pBefore + pStart;
                return d0 * posRem * posRem * posRem + (pBefore - pStart - d0) * posRem * posRem + (pEnd - pBefore) * posRem + pStart;
            }
        }
        //Random number Generation.
        //Unity.Random has to be called from main thread but System.Random doesn't (but doesn't have the easy get functions Unity does)
        static System.Random rng = null;
        /// <summary>
        /// Sets seed for Random number generation
        /// </summary>
        /// <param name="seed">Seed value to set</param>
        public static void RandomSeed(int seed) {
            rng = new System.Random(seed);
        }
        /// <summary>
        /// Generates a random number between min and max (exclusive)
        /// </summary>
        /// <param name="min">Minimum value allowed</param>
        /// <param name="max">Maximum value allowed</param>
        /// <returns>Floating point number between min and max</returns>
        public static float GetRandom(float min, float max) {
            if (rng == null)
                rng = new System.Random();
            if (max <= min)
                return min;
            return min + (max - min) * (float)rng.NextDouble();
        }
        /// <summary>
        /// Generates a random number between min and max (exclusive)
        /// </summary>
        /// <param name="minMax">Minimum and Maximum value allowed in Vector2</param>
        /// <returns>Floating point number between min and max</returns>
        public static float GetRandom(Vector2 minMax) {
            if (rng == null)
                rng = new System.Random();
            if (minMax.y <= minMax.x)
                return minMax.x;
            return minMax.x + (minMax.y - minMax.x) * (float)rng.NextDouble();
        }
        /// <summary>
        /// Generates a random number between min and max (exclusive)
        /// </summary>
        /// <param name="min">Minimum value allowed</param>
        /// <param name="max">Maximum value allowed</param>
        /// <returns>Double-length floating point number between min and max</returns>
        public static double GetRandom(double min, double max) {
            if (rng == null)
                rng = new System.Random();
            if (max <= min)
                return min;
            return min + (max - min) * rng.NextDouble();
        }
        /// <summary>
        /// Generates a random number between min and max (inclusive)
        /// </summary>
        /// <param name="min">Minimum value allowed</param>
        /// <param name="max">Maximum value allowed</param>
        /// <returns>32-Bit integer number between min and max</returns>
        public static int GetRandom(int min, int max) {
            if (rng == null)
                rng = new System.Random();
            if (max <= min)
                return min;
            return min + Mathf.FloorToInt((max - min + 1) * (float)rng.NextDouble());
        }
    }
}
