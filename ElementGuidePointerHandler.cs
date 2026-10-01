using UnityEngine;
using UnityEngine.EventSystems;

namespace ElementalBattlegroundsMod
{
    internal sealed class ElementGuidePointerHandler : MonoBehaviour, IPointerClickHandler
    {
        internal ElementLoadoutMenuRuntime runtime;
        internal string elementId;
        internal ElementId element;
        internal string displayName;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Right && runtime != null)
                runtime.OpenElementGuide(elementId, element, displayName);
        }
    }
}
