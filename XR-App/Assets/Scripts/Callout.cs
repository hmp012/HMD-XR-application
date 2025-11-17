using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Callout used to display information like world and controller tooltips.
/// </summary>
public class Callout : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] [Tooltip("The tooltip Transform associated with this Callout.")]
    Transform mLazyTooltip;

    [SerializeField] [Tooltip("The line curve GameObject associated with this Callout.")]
    private GameObject mCurve;

    [SerializeField] [Tooltip("The required time to dwell on this callout before the tooltip and curve are enabled.")]
    private float mDwellTime = 1f;

    [SerializeField] [Tooltip("Whether the associated tooltip will be unparented on Start.")]
    private bool mUnparent = true;

    [SerializeField] [Tooltip("Whether the associated tooltip and curve will be disabled on Start.")]
    private bool mTurnOffAtStart = true;

    private bool _mGazing = false;

    private Coroutine _mStartCo;
    private Coroutine _mEndCo;

    void Start()
    {
        if (mUnparent)
        {
            if (mLazyTooltip != null)
                mLazyTooltip.SetParent(null);
        }

        if (mTurnOffAtStart)
        {
            if (mLazyTooltip != null)
                mLazyTooltip.gameObject.SetActive(false);
            if (mCurve != null)
                mCurve.SetActive(false);
        }
    }

    public void GazeHoverStart()
    {
        _mGazing = true;
        if (_mStartCo != null)
            StopCoroutine(_mStartCo);
        if (_mEndCo != null)
            StopCoroutine(_mEndCo);
        _mStartCo = StartCoroutine(StartDelay());
    }

    public void GazeHoverEnd()
    {
        _mGazing = false;
        _mEndCo = StartCoroutine(EndDelay());
    }

    IEnumerator StartDelay()
    {
        yield return new WaitForSeconds(mDwellTime);
        if (_mGazing)
            TurnOnStuff();
    }

    IEnumerator EndDelay()
    {
        if (!_mGazing)
            TurnOffStuff();
        yield return null;
    }

    public void TurnOnStuff()
    {
        if (mLazyTooltip != null)
            mLazyTooltip.gameObject.SetActive(true);
        if (mCurve != null)
            mCurve.SetActive(true);
    }

    public void TurnOffStuff()
    {
        if (mLazyTooltip != null)
            mLazyTooltip.gameObject.SetActive(false);
        if (mCurve != null)
            mCurve.SetActive(false);
    }
}