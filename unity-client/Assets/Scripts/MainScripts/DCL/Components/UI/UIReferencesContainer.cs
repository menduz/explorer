using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DCL.Interface;

namespace DCL.Components
{
    public class UIReferencesContainer : MonoBehaviour, IPointerDownHandler
    {
        [System.NonSerialized] public UIShape owner;

        [Header("Basic Fields")]
        [Tooltip("This needs to always have the root RectTransform.")]
        public RectTransform rectTransform;

        public CanvasGroup canvasGroup;

        public HorizontalLayoutGroup layoutGroup;
        public LayoutElement layoutElement;
        public RectTransform layoutElementRT;

        [Tooltip("Children of this UI object will reparent to this rectTransform.")]
        public RectTransform childHookRectTransform;

        public LinkedList<UIReferencesContainer> uiTree;
        public LinkedListNode<UIReferencesContainer> uiTreeNode;

        bool VERBOSE = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            UIShape.Model ownerModel = owner.model;

            if (VERBOSE)
            {
                Debug.Log("pointer current raycast: " + eventData.pointerCurrentRaycast,
                    eventData.pointerCurrentRaycast.gameObject);
                Debug.Log("pointer press raycast: " + eventData.pointerPressRaycast,
                    eventData.pointerPressRaycast.gameObject);
            }

            if (!string.IsNullOrEmpty(ownerModel.onClick) &&
                eventData.pointerPressRaycast.gameObject == childHookRectTransform.gameObject)
            {
                WebInterface.ReportOnClickEvent(owner.scene.sceneData.id, ownerModel.onClick);
            }
        }

#if UNITY_EDITOR
        [Header("Debug")]
        public bool forceRefresh;

        public void LateUpdate()
        {
            if (forceRefresh)
            {
                owner.RefreshDCLLayoutRecursively();
                forceRefresh = false;
            }
        }
#endif
    }
}