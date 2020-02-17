using DCL.Controllers;
using DCL.Helpers;
using DCL.Interface;
using DCL.Models;
using UnityEngine;

namespace DCL.Components
{
    public class OnPointerEvent : UUIDComponent<OnPointerEvent.Model>
    {
        public static bool enableInteractionHoverFeedback = true;

        [System.Serializable]
        new public class Model : UUIDComponent.Model
        {
            public string button = WebInterface.ACTION_BUTTON.ANY.ToString();
            public string hoverText = "Interact";
            public float distance = 10f;
            public bool showFeedback = true;
        }

        OnPointerEventColliders pointerEventColliders;
        InteractionHoverCanvasController hoverCanvasController;

        public override void Setup(ParcelScene scene, DecentralandEntity entity, UUIDComponent.Model model)
        {
            this.entity = entity;
            this.scene = scene;

            if (model == null)
                this.model = new OnPointerEvent.Model();
            else
                this.model = (OnPointerEvent.Model)model;

            Initialize();

            entity.OnShapeUpdated -= OnComponentUpdated;
            entity.OnShapeUpdated += OnComponentUpdated;
        }

        public string GetMeshName(Collider collider)
        {
            return pointerEventColliders.GetMeshName(collider);
        }

        public WebInterface.ACTION_BUTTON GetActionButton()
        {
            switch (model.button)
            {
                case "PRIMARY":
                    return WebInterface.ACTION_BUTTON.PRIMARY;
                case "SECONDARY":
                    return WebInterface.ACTION_BUTTON.SECONDARY;
                case "POINTER":
                    return WebInterface.ACTION_BUTTON.POINTER;
                default:
                    return WebInterface.ACTION_BUTTON.ANY;
            }
        }

        public void Initialize()
        {
            if (!entity.meshRootGameObject) return;

            // Create OnPointerEventCollider child
            pointerEventColliders = Utils.GetOrCreateComponent<OnPointerEventColliders>(this.gameObject);
            pointerEventColliders.Initialize(entity);
            pointerEventColliders.refCount++;

            if (hoverCanvasController == null)
            {
                GameObject hoverCanvasGameObject = Object.Instantiate(Resources.Load("InteractionHoverCanvas"), PointerEventsController.i.transform) as GameObject;
                hoverCanvasController = hoverCanvasGameObject.GetComponent<InteractionHoverCanvasController>();
            }

            if (hoverCanvasController == null || model == null)
            {
                Debug.Log("Hover canvas or model is null???");
                return;
            }

            hoverCanvasController.enabled = model.showFeedback;
            hoverCanvasController.Setup(model.button, model.hoverText, entity);
        }

        void OnComponentUpdated(DecentralandEntity e)
        {
            Initialize();
        }

        protected override void RemoveComponent<T>(DecentralandEntity entity)
        {
            if (hoverCanvasController != null)
            {
                Destroy(hoverCanvasController.gameObject);
            }
        }

        public void SetHoverState(bool hoverState)
        {
            if (!enableInteractionHoverFeedback) return;

            hoverCanvasController.SetHoverState(hoverState);
        }

        public bool IsAtHoverDistance(Transform other)
        {
            return Vector3.Distance(other.position, transform.position) <= model.distance;
        }
        public bool IsAtHoverDistance(float distance)
        {
            if (model == null)
                return false;

            return distance <= model.distance;
        }

        void OnDestroy()
        {
            if (entity != null)
                entity.OnShapeUpdated -= OnComponentUpdated;

            if (pointerEventColliders != null)
            {
                pointerEventColliders.refCount--;

                if (pointerEventColliders.refCount <= 0)
                {
                    Destroy(pointerEventColliders);
                }
            }

            if (hoverCanvasController != null)
            {
                Destroy(hoverCanvasController.gameObject);
            }
        }
    }
}
