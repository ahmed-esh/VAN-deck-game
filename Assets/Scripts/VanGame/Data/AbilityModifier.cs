using System;
using UnityEngine;

namespace VanGame.Data
{
    [Serializable]
    public struct AbilityModifier
    {
        public ModifierTarget target;
        public ModifierOperation operation;
        public float value;
    }
}
