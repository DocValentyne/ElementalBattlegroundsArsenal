using ElementalBattlegroundsMod.Api;
using UnityEngine;

namespace ElementalBattlegroundsMod
{
    internal enum ElementId
    {
        None = -1,
        Vanilla = 0,
        Fire,
        Water,
        Grass,
        Wind,
        Storm,
        Earth,
        Lava,
        Ice,
        Nature,
        Sand,
        Metal,
        Plasma,
        Crystal,
        Spirit,
        Sound,
        Light,
        Darkness,
        Gravity,
        Explosion,
        Technology,
        Phoenix,
        Dragon,
        Acid,
        Aurora,
        Nightmare,
        Time,
        Void,
        Spectrum,
        Chaos,
        Angel,
        Space,
        Reaper,
        Illusion,
        Slime,
        Creation,
        Solar,
        Physical,
        PhysicalAlt,
        Sans
    }

    internal enum WeaponFamily
    {
        Revolver,
        Shotgun,
        Rapid,
        Ultimate,
        Explosive
    }

    internal sealed class ElementalSlotMarker : MonoBehaviour
    {
        public ElementId element;
        public string elementId;
        public WeaponFamily family;
        public int position;
        public string choiceId;
        public string choiceName;
        public ExplosiveIntegrationMode explosiveMode;
        public bool hasVanillaTemplate;
        public VanillaWeaponTemplate vanillaTemplate;

        public bool IsCustom => !string.IsNullOrEmpty(elementId) || (element != ElementId.Vanilla && element != ElementId.None);
        public bool IsEmpty => string.IsNullOrEmpty(elementId) && element == ElementId.None;
    }

    internal static class MarkerUtility
    {
        internal static ElementalSlotMarker Find(Component component)
        {
            if (component == null)
                return null;
            ElementalSlotMarker marker = component.GetComponent<ElementalSlotMarker>();
            if (marker != null)
                return marker;
            return component.GetComponentInParent<ElementalSlotMarker>();
        }

        internal static ElementalSlotMarker Find(GameObject gameObject)
        {
            if (gameObject == null)
                return null;
            ElementalSlotMarker marker = gameObject.GetComponent<ElementalSlotMarker>();
            if (marker != null)
                return marker;
            return gameObject.GetComponentInParent<ElementalSlotMarker>();
        }
    }
}
