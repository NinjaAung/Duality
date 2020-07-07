// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.

/*
 * Collection of Enums used in multiple areas of AmbientSounds system
 */

namespace AmbientSounds {
    /// <summary>
    /// Used to decide how a collection of triggers or sliders should be evaulated.
    /// All = all must be true.
    /// Any = one must be true.
    /// None = all must be false.
    /// </summary>
    public enum EvaluationType {
        ALL,
        ANY,
        NONE,
    }
    /// <summary>
    /// Flags to decide whether Sliders, Events, Both, or Either should be evaluated.
    /// </summary>
    public enum ValuesOrEvents {
        ValuesAndEvents    = 0x07,
        ValuesOrEvents     = 0x03,
        Values             = 0x02,
        Events              = 0x01,
        None                = 0,
    }
    /// <summary>
    /// Used to deicde how to modify a collection.
    /// Replace - the new collection will be used instead
    /// Add - the new collection will be added to the end of the existing collection
    /// Remove - all instances of items within the new collection will be removed from the existing collection
    /// </summary>
    public enum ClipModType {
        Replace,
        Add,
        Remove,
    }
    /// <summary> How many dimensions should be checked? </summary>
    public enum Dimentions {
        THREE,
        TWO,
        ONE,
    }
    /// <summary> Shape of a PositionalSequence area </summary>
    public enum Shape {
        RECTANGLE,
        SPHERE,
    }
    /// <summary> Directions to ignore in 2D mode or to check in 1D mode </summary>
    public enum Direction {
        X,
        Y,
        Z,
    }
    /// <summary>
    /// How to output Audio data.
    /// Strait = Feeds directly into AudioListener,
    /// LocalPosition = Creates AudioSource local to PositionalSequence,
    /// PlayerPosition = Creates AudioSource local to "Player Object".
    /// </summary>
    public enum OutputType {
        STRAIGHT,
        LOCAL_POSITION,
        PLAYER_POSITION,
    }
    /// <summary>
    /// How to sync sequences.
    /// Once = Play once, no auto stretching
    /// Stretch = Play once, extend length to match group
    /// Squeeze = Play once, reduce length to match group
    /// Fit = Play once, extend or reduce length to match group
    /// Repeat = Play repeatedly until group ends, no auto stretching
    /// Stretch and Repeat = Play repeatedly until group ends, extend total length to match group
    /// Squeeze and Repeat = Play repeatedly until group ends, reduce total length to match group
    /// Fit and Repeat = Play repeatedly until group ends, extend or reduce total length to match group
    /// </summary>
    public enum SyncType {
        ONCE                = 0x00,
        STRETCH             = 0x01,
        SQUEEZE             = 0x02,
        FIT                 = STRETCH | SQUEEZE,
        REPEAT              = 0x10,
        STRETCH_AND_REPEAT  = REPEAT | STRETCH,
        SQUEEZE_AND_REPEAT  = REPEAT | SQUEEZE,
        FIT_AND_REPEAT      = REPEAT | FIT,
    }
}
