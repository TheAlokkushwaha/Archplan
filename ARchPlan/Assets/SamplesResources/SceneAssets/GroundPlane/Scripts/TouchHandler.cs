/*==============================================================================
Copyright (c) 2021 PTC Inc. All Rights Reserved.

Vuforia is a trademark of PTC Inc., registered in the United States and other
countries.
==============================================================================*/

using UnityEngine;

public class TouchHandler : MonoBehaviour
{
    public Transform[] AugmentationObjects;
    public bool EnablePinchScaling;

    public static bool sIsSingleFingerStationary => IsSingleFingerDown() && Input.GetTouch(0).phase == TouchPhase.Stationary;
    public static bool sIsSingleFingerDragging => IsSingleFingerDown() && Input.GetTouch(0).phase == TouchPhase.Moved;
    static int sLastTouchCount;

    const float SCALE_RANGE_MIN = 0.1f;
    const float SCALE_RANGE_MAX = 2.0f;

    Touch[] mTouches;
    bool mEnableRotation;
    bool mIsFirstFrameWithTwoTouches;
    float mCachedTouchAngle;
    float mCachedTouchDistance;
    float[] mCachedAugmentationScales;
    Vector3[] mCachedAugmentationRotations;

    /// <summary>
    /// Enables rotation input.
    /// It is registered to ContentPositioningBehaviour.OnContentPlaced.
    /// </summary>
    public void EnableRotation()
    {
        mEnableRotation = true;
    }

    /// <summary>
    /// Disables rotation input.
    /// It is registered to UI Reset Button and also DevicePoseBehaviourManager.DevicePoseReset event.
    /// </summary>
    public void DisableRotation()
    {
        mEnableRotation = false;
    }

    void Start()
    {
        mCachedAugmentationScales = new float[AugmentationObjects.Length];
        mCachedAugmentationRotations = new Vector3[AugmentationObjects.Length];

        for (int i = 0; i < AugmentationObjects.Length; i++)
        {
            mCachedAugmentationScales[i] = AugmentationObjects[i].localScale.x;
            mCachedAugmentationRotations[i] = AugmentationObjects[i].localEulerAngles;
        }
    }

    void Update()
    {
        if (Input.touchCount == 2)
        {
            GetTouchAngleAndDistance(Input.GetTouch(0), Input.GetTouch(1),
                out var currentTouchAngle, out var currentTouchDistance);

            if (mIsFirstFrameWithTwoTouches)
            {
                mCachedTouchDistance = currentTouchDistance;
                mCachedTouchAngle = currentTouchAngle;
                mIsFirstFrameWithTwoTouches = false;
            }

            var angleDelta = currentTouchAngle - mCachedTouchAngle;
            var scaleMultiplier = currentTouchDistance / mCachedTouchDistance;

            for (int i = 0; i < AugmentationObjects.Length; i++)
            {
                var scaleAmount = mCachedAugmentationScales[i] * scaleMultiplier;
                var scaleAmountClamped = Mathf.Clamp(scaleAmount, SCALE_RANGE_MIN, SCALE_RANGE_MAX);

                if (mEnableRotation)
                    AugmentationObjects[i].localEulerAngles = mCachedAugmentationRotations[i] - new Vector3(0, angleDelta * 3f, 0);

                // Optional Pinch Scaling can be enabled via Inspector for this Script Component
                if (mEnableRotation && EnablePinchScaling)
                    AugmentationObjects[i].localScale = new Vector3(scaleAmountClamped, scaleAmountClamped, scaleAmountClamped);
            }
        }
        else if (Input.touchCount < 2)
        {
            for (int i = 0; i < AugmentationObjects.Length; i++)
            {
                mCachedAugmentationScales[i] = AugmentationObjects[i].localScale.x;
                mCachedAugmentationRotations[i] = AugmentationObjects[i].localEulerAngles;
            }
            mIsFirstFrameWithTwoTouches = true;
        }
        // enable runtime testing of pinch scaling
        else if (Input.touchCount == 6)
            EnablePinchScaling = true;
        // disable runtime testing of pinch scaling
        else if (Input.touchCount == 5)
            EnablePinchScaling = false;
    }

    void GetTouchAngleAndDistance(Touch firstTouch, Touch secondTouch, out float touchAngle, out float touchDistance)
    {
        touchDistance = Vector2.Distance(firstTouch.position, secondTouch.position);
        var diffY = firstTouch.position.y - secondTouch.position.y;
        var diffX = firstTouch.position.x - secondTouch.position.x;
        touchAngle = Mathf.Atan2(diffY, diffX) * Mathf.Rad2Deg;
    }

    static bool IsSingleFingerDown()
    {
        if (Input.touchCount == 0 || Input.touchCount >= 2)
            sLastTouchCount = Input.touchCount;

        return Input.touchCount == 1 && Input.GetTouch(0).fingerId == 0 && sLastTouchCount == 0;
    }
}
