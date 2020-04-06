﻿using UnityEngine;
using UnityEngine.UI;
using DCL.Helpers;
using TMPro;

namespace DCL
{
    public class NavmapView : MonoBehaviour
    {
        const int LEFT_BORDER_PARCELS = 25;
        const int BOTTOM_BORDER_PARCELS = 25;
        const int WORLDMAP_WIDTH_IN_PARCELS = 300;

        [Header("Configuration")]
        [SerializeField] float parcelHightlightScale = 1.25f;

        [Header("References")]
        [SerializeField] InputAction_Trigger toggleNavMapAction;
        [SerializeField] Button closeButton;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] Transform scrollRectContentTransform;
        [SerializeField] TextMeshProUGUI currentSceneNameText;
        [SerializeField] TextMeshProUGUI currentSceneCoordsText;
        RawImage parcelHighlightImage;
        TextMeshProUGUI highlightedParcelText;
        InputAction_Trigger.Triggered toggleNavMapDelegate;
        int parcelSizeInMap;
        RectTransform minimapViewport;
        Transform mapRendererMinimapParent;
        Vector3 atlasOriginalPosition;
        MinimapMetadata mapMetadata;
        Vector3 worldmapOffset;
        Vector3 worldCoordsOriginInMap;
        Vector3[] navmapWorldspaceCorners = new Vector3[4];
        bool cursorLockedBeforeOpening = true;
        Vector3 mouseMapCoords;

        public bool isOpen
        {
            private set
            {
                scrollRect.gameObject.SetActive(value);
            }
            get
            {
                return scrollRect.gameObject.activeSelf;
            }
        }

        // TODO: Remove this bool once we finish the feature
        bool enabledInProduction = true;

        void Start()
        {
            mapMetadata = MinimapMetadata.GetMetadata();

            worldmapOffset = new Vector3(LEFT_BORDER_PARCELS + WORLDMAP_WIDTH_IN_PARCELS / 2, BOTTOM_BORDER_PARCELS + WORLDMAP_WIDTH_IN_PARCELS / 2, 0);
            parcelSizeInMap = (MapUtils.PARCEL_SIZE / 2);

            parcelHighlightImage = MapRenderer.i.parcelHighlightImage;
            highlightedParcelText = parcelHighlightImage.GetComponentInChildren<TextMeshProUGUI>(true);
            parcelHighlightImage.rectTransform.localScale = new Vector3(parcelHightlightScale, parcelHightlightScale, 1f);

            closeButton.onClick.AddListener(() => { ToggleNavMap(); });
            scrollRect.onValueChanged.AddListener((x) => { if (isOpen) MapRenderer.i.atlas.UpdateCulling(); });

            toggleNavMapDelegate = (x) => { ToggleNavMap(); };
            toggleNavMapAction.OnTriggered += toggleNavMapDelegate;

            MinimapHUDView.OnUpdateData += UpdateCurrentSceneData;
        }

        void Update()
        {
            if (!isOpen) return;

            // Get the world-space corners of the map
            (MapRenderer.i.atlas.chunksParent.transform as RectTransform).GetWorldCorners(navmapWorldspaceCorners);

            // Offset world coordinates origin position in map with border-parcels and worldmap amount of parcels (horizontally/vertically) / 2
            // (since the "border-parcels" outside the world are not the same amount on the 4 sides of the worldmap we can't just use the center of the rect)
            worldCoordsOriginInMap = navmapWorldspaceCorners[0] + worldmapOffset * parcelSizeInMap;

            UpdateMouseMapCoords();

            UpdateParcelHighlight();
        }

        void UpdateMouseMapCoords()
        {
            mouseMapCoords = Input.mousePosition - worldCoordsOriginInMap;
            mouseMapCoords = mouseMapCoords / parcelSizeInMap;

            mouseMapCoords.x = (int)Mathf.Floor(mouseMapCoords.x);
            mouseMapCoords.y = (int)Mathf.Floor(mouseMapCoords.y);
        }

        void UpdateParcelHighlight()
        {
            if (!CoordinatesAreInsideTheworld((int)mouseMapCoords.x, (int)mouseMapCoords.y))
            {
                if (parcelHighlightImage.gameObject.activeSelf)
                    parcelHighlightImage.gameObject.SetActive(false);

                return;
            }

            if (!parcelHighlightImage.gameObject.activeSelf)
                parcelHighlightImage.gameObject.SetActive(true);

            parcelHighlightImage.transform.position = worldCoordsOriginInMap + mouseMapCoords * parcelSizeInMap + new Vector3(parcelSizeInMap, parcelSizeInMap, 0f) / 2;
            highlightedParcelText.text = $"{mouseMapCoords.x}, {mouseMapCoords.y}";

            // ----------------------------------------------------
            // TODO: Use sceneInfo to highlight whole scene parcels and populate scenes hover info on navmap once we can access all the scenes info
            // var sceneInfo = mapMetadata.GetSceneInfo(mouseMapCoords.x, mouseMapCoords.y);
        }

        bool CoordinatesAreInsideTheworld(int xCoord, int yCoord)
        {
            return (Mathf.Abs(xCoord) <= WORLDMAP_WIDTH_IN_PARCELS / 2) && (Mathf.Abs(yCoord) <= WORLDMAP_WIDTH_IN_PARCELS / 2);
        }

        void ToggleNavMap()
        {
            if (MapRenderer.i == null) return;

#if !UNITY_EDITOR
            if(!enabledInProduction) return;
#endif

            scrollRect.StopMovement();
            isOpen = !isOpen;

            if (isOpen)
            {
                cursorLockedBeforeOpening = Utils.isCursorLocked;
                if (cursorLockedBeforeOpening)
                    Utils.UnlockCursor();

                minimapViewport = MapRenderer.i.atlas.viewport;
                mapRendererMinimapParent = MapRenderer.i.transform.parent;
                atlasOriginalPosition = MapRenderer.i.atlas.chunksParent.transform.localPosition;

                MapRenderer.i.atlas.viewport = scrollRect.viewport;
                MapRenderer.i.transform.SetParent(scrollRectContentTransform);
                MapRenderer.i.atlas.UpdateCulling();

                scrollRect.content = MapRenderer.i.atlas.chunksParent.transform as RectTransform;

                // Reposition de player icon parent to scroll everything together
                MapRenderer.i.atlas.overlayLayerGameobject.transform.SetParent(scrollRect.content);

                // Center map
                MapRenderer.i.atlas.CenterToTile(Utils.WorldToGridPositionUnclamped(CommonScriptableObjects.playerWorldPosition));
            }
            else
            {
                if (cursorLockedBeforeOpening)
                    Utils.LockCursor();

                parcelHighlightImage.gameObject.SetActive(false);

                MapRenderer.i.atlas.viewport = minimapViewport;
                MapRenderer.i.transform.SetParent(mapRendererMinimapParent);
                MapRenderer.i.atlas.chunksParent.transform.localPosition = atlasOriginalPosition;
                MapRenderer.i.atlas.UpdateCulling();

                MapRenderer.i.atlas.overlayLayerGameobject.transform.SetParent(MapRenderer.i.atlas.chunksParent.transform.parent);
                (MapRenderer.i.atlas.overlayLayerGameobject.transform as RectTransform).anchoredPosition = Vector2.zero;

                MapRenderer.i.UpdateRendering(Utils.WorldToGridPositionUnclamped(CommonScriptableObjects.playerWorldPosition.Get()));
            }
        }

        void UpdateCurrentSceneData(MinimapHUDModel model)
        {
            currentSceneNameText.text = string.IsNullOrEmpty(model.sceneName) ? "Unnamed" : model.sceneName;
            currentSceneCoordsText.text = model.playerPosition;
        }
    }
}