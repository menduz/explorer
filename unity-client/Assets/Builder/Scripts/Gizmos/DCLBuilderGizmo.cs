﻿using UnityEngine;

namespace Builder.Gizmos
{
    public abstract class DCLBuilderGizmo : MonoBehaviour
    {
        [SerializeField] private string gizmoType = string.Empty;
        [SerializeField] protected DCLBuilderGizmoAxis axisX;
        [SerializeField] protected DCLBuilderGizmoAxis axisY;
        [SerializeField] protected DCLBuilderGizmoAxis axisZ;

        public bool initialized { get; private set; }

        protected float snapFactor = 0;

        protected bool worldOrientedGizmos = true;
        private Transform targetTransform = null;

        protected Camera builderCamera;

        private Vector3 relativeScaleRatio;
        protected bool startDragging = false;
        protected float prevAxisValue;

        public DCLBuilderGizmoAxis activeAxis { protected set; get; }

        public abstract void SetSnapFactor(DCLBuilderGizmoManager.SnapInfo snapInfo);
        public abstract void TransformEntity(Transform targetTransform, DCLBuilderGizmoAxis axis, float axisValue);

        public virtual void Initialize(Camera camera)
        {
            initialized = true;
            relativeScaleRatio = transform.localScale / GetCameraPlaneDistance(camera, transform.position);
            builderCamera = camera;
            axisX.SetGizmo(this);
            axisY.SetGizmo(this);
            axisZ.SetGizmo(this);
        }

        public string GetGizmoType()
        {
            return gizmoType;
        }

        public virtual void SetTargetTransform(Transform entityTransform)
        {
            targetTransform = entityTransform;
            SetPositionToTarget();
        }

        public virtual void OnBeginDrag(DCLBuilderGizmoAxis axis, Transform entityTransform)
        {
            startDragging = true;
            targetTransform = entityTransform;
            activeAxis = axis;
            axis.SetColorHighlight();
        }

        public virtual void OnDrag(Vector3 hitPoint, Vector2 mousePosition)
        {
            float axisValue = GetHitPointToAxisValue(activeAxis, hitPoint, mousePosition);

            if (startDragging)
            {
                startDragging = false;
                prevAxisValue = axisValue;
            }

            float transformValue = axisValue - prevAxisValue;
            if (Mathf.Abs(transformValue) >= snapFactor)
            {
                if (snapFactor > 0)
                {
                    float sign = Mathf.Sign(transformValue);
                    transformValue = transformValue + (Mathf.Abs(transformValue) % snapFactor) * -sign;
                }

                SetPreviousAxisValue(axisValue, transformValue);
                TransformEntity(targetTransform, activeAxis, transformValue);
            }
        }

        public virtual void OnEndDrag()
        {
            activeAxis.SetColorDefault();
        }

        public virtual bool RaycastHit(Ray ray, out Vector3 hitPoint)
        {
            Vector3 hitPos = ray.GetPoint(Vector3.Distance(ray.origin, activeAxis.transform.position));
            Vector3 hitOffset = (hitPos - activeAxis.transform.position);
            hitPoint = activeAxis.transform.position + Vector3.Project(hitOffset, activeAxis.transform.forward);
            return true;
        }

        protected virtual float GetHitPointToAxisValue(DCLBuilderGizmoAxis axis, Vector3 hitPoint, Vector2 mousePosition)
        {
            Vector3 dir = (hitPoint - axis.transform.position).normalized;
            float sign = Vector3.Angle(dir, axis.transform.forward) == 180 ? -1 : 1;
            return Vector3.Distance(activeAxis.transform.position, hitPoint) * sign;
        }

        protected virtual void SetPreviousAxisValue(float axisValue, float transformValue)
        {
            prevAxisValue = axisValue - transformValue;
        }

        private void SetPositionToTarget()
        {
            if (targetTransform)
            {
                transform.position = targetTransform.position;
                if (!worldOrientedGizmos)
                {
                    transform.rotation = targetTransform.rotation;
                }
            }
        }

        private void Update()
        {
            SetPositionToTarget();
            if (builderCamera)
            {
                float dist = GetCameraPlaneDistance(builderCamera, transform.position);
                transform.localScale = relativeScaleRatio * dist;
            }
        }

        private static float GetCameraPlaneDistance(Camera camera, Vector3 objectPosition)
        {
            Plane plane = new Plane(camera.transform.forward, camera.transform.position);
            return plane.GetDistanceToPoint(objectPosition);
        }
    }
}