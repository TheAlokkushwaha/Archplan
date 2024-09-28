/*============================================================================== 
Copyright (c) 2021 PTC Inc. All Rights Reserved.

Vuforia is a trademark of PTC Inc., registered in the United States and other 
countries.   
==============================================================================*/

using UnityEngine;
using Vuforia;
using Vuforia.UnityRuntimeCompiled;

public class ProductPlacement : MonoBehaviour
{
    public bool GroundPlaneHitReceived { get; private set; }
    Vector3 ProductScale
    {
        get
        {
            var augmentationScale = VuforiaRuntimeUtilities.IsPlayMode() ? 0.1f : ProductSize;
            return new Vector3(augmentationScale, augmentationScale, augmentationScale);
        }
    }

    [Header("Augmentation Objects")]
    [SerializeField] GameObject[] AugmentationObjects = null;

    [Header("Control Indicators")]
    [SerializeField] GameObject TranslationIndicator = null;
    [SerializeField] GameObject RotationIndicator = null;

    [Header("Augmentation Size")]
    [Range(0.1f, 2.0f)]
    [SerializeField] float ProductSize = 0.65f;

    const string RESOURCES_CHAIR_BODY = "ChairBody";
    const string RESOURCES_CHAIR_FRAME = "ChairFrame";
    const string RESOURCES_CHAIR_BODY_TRANSPARENT = "ChairBodyTransparent";
    const string RESOURCES_CHAIR_FRAME_TRANSPARENT = "ChairFrameTransparent";
    const string GROUND_PLANE_NAME = "Emulator Ground Plane";
    const string FLOOR_NAME = "Floor";

    MeshRenderer[] mAugmentationRenderers;
    Material[][] mAugmentationMaterials, mAugmentationMaterialsTransparent;
    Camera mMainCamera;
    string mFloorName;
    Vector3[] mOriginalAugmentationScales;
    bool mIsPlaced;
    int mAutomaticHitTestFrameCount;

    void Start()
    {
        mMainCamera = VuforiaBehaviour.Instance.GetComponent<Camera>();

        mAugmentationRenderers = new MeshRenderer[AugmentationObjects.Length];
        mAugmentationMaterials = new Material[AugmentationObjects.Length][];
        mAugmentationMaterialsTransparent = new Material[AugmentationObjects.Length][];
        mOriginalAugmentationScales = new Vector3[AugmentationObjects.Length];

        SetupMaterials();
        SetupFloor();

        for (int i = 0; i < AugmentationObjects.Length; i++)
        {
            mAugmentationRenderers[i] = AugmentationObjects[i].GetComponent<MeshRenderer>();
            mOriginalAugmentationScales[i] = AugmentationObjects[i].transform.localScale;
        }
        Reset();
    }

    void Update()
    {
        EnablePreviewModeTransparency(!mIsPlaced);
        if (!mIsPlaced)
        {
            foreach (var augmentation in AugmentationObjects)
            {
                RotateTowardsCamera(augmentation);
            }
        }

        if (mIsPlaced)
        {
            RotationIndicator.SetActive(Input.touchCount == 2);

            TranslationIndicator.SetActive((TouchHandler.sIsSingleFingerDragging || TouchHandler.sIsSingleFingerStationary)
                                            && !UnityRuntimeCompiledFacade.Instance.IsUnityUICurrentlySelected());

            SnapProductToMousePosition();
        }
        else
        {
            RotationIndicator.SetActive(false);
            TranslationIndicator.SetActive(false);
        }
    }

    void LateUpdate()
    {
        GroundPlaneHitReceived = mAutomaticHitTestFrameCount == Time.frameCount;

        if (!mIsPlaced)
        {
            var isVisible = VuforiaBehaviour.Instance.DevicePoseBehaviour.TargetStatus.IsTrackedOrLimited() && GroundPlaneHitReceived;

            foreach (var renderer in mAugmentationRenderers)
            {
                renderer.enabled = isVisible;
            }
        }
    }

    void SnapProductToMousePosition()
    {
        if (TouchHandler.sIsSingleFingerDragging || VuforiaRuntimeUtilities.IsPlayMode() && Input.GetMouseButton(0))
        {
            if (!UnityRuntimeCompiledFacade.Instance.IsUnityUICurrentlySelected())
            {
                var cameraToPlaneRay = mMainCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(cameraToPlaneRay, out var cameraToPlaneHit) &&
                    cameraToPlaneHit.collider.gameObject.name == mFloorName)
                {
                    foreach (var augmentation in AugmentationObjects)
                    {
                        augmentation.transform.position = cameraToPlaneHit.point;
                    }
                }
            }
        }
    }

    public void Reset()
    {
        for (int i = 0; i < AugmentationObjects.Length; i++)
        {
            AugmentationObjects[i].transform.localPosition = Vector3.zero;
            AugmentationObjects[i].transform.localEulerAngles = Vector3.zero;
            AugmentationObjects[i].transform.localScale = Vector3.Scale(mOriginalAugmentationScales[i], ProductScale);
        }

        mIsPlaced = false;
    }

    public void OnContentPlaced()
    {
        Debug.Log("OnContentPlaced() called.");

        foreach (var augmentation in AugmentationObjects)
        {
            augmentation.transform.localPosition = Vector3.zero;
            RotateTowardsCamera(augmentation);
        }

        mIsPlaced = true;
    }

    public void OnAutomaticHitTest(HitTestResult result)
    {
        mAutomaticHitTestFrameCount = Time.frameCount;

        if (!mIsPlaced)
        {
            foreach (var augmentation in AugmentationObjects)
            {
                augmentation.transform.position = result.Position;
            }
        }
    }

    void SetupMaterials()
    {
        for (int i = 0; i < AugmentationObjects.Length; i++)
        {
            mAugmentationMaterials[i] = new[]
            {
                Resources.Load<Material>(RESOURCES_CHAIR_BODY),
                Resources.Load<Material>(RESOURCES_CHAIR_FRAME)
            };

            mAugmentationMaterialsTransparent[i] = new[]
            {
                Resources.Load<Material>(RESOURCES_CHAIR_BODY_TRANSPARENT),
                Resources.Load<Material>(RESOURCES_CHAIR_FRAME_TRANSPARENT)
            };
        }
    }

    void SetupFloor()
    {
        if (VuforiaRuntimeUtilities.IsPlayMode())
            mFloorName = GROUND_PLANE_NAME;
        else
        {
            mFloorName = FLOOR_NAME;
            var floor = new GameObject(mFloorName, typeof(BoxCollider));
            floor.transform.SetParent(AugmentationObjects[0].transform.parent);
            floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            floor.transform.localScale = Vector3.one;
            floor.GetComponent<BoxCollider>().size = new Vector3(100f, 0, 100f);
        }
    }

    void EnablePreviewModeTransparency(bool previewEnabled)
    {
        for (int i = 0; i < mAugmentationRenderers.Length; i++)
        {
            mAugmentationRenderers[i].materials = previewEnabled ? mAugmentationMaterialsTransparent[i] : mAugmentationMaterials[i];
        }
    }

    void RotateTowardsCamera(GameObject augmentation)
    {
        var lookAtPosition = mMainCamera.transform.position - augmentation.transform.position;
        lookAtPosition.y = 0;
        var rotation = Quaternion.LookRotation(lookAtPosition);
        augmentation.transform.rotation = rotation;
    }
}
