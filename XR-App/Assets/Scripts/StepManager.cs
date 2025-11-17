using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Controls the steps in the in coaching card.
/// </summary>
public class StepManager : MonoBehaviour
{
    [Serializable]
    class Step
    {
        [SerializeField] public GameObject stepObject;

        [SerializeField] public string buttonText;
    }

    [SerializeField] public TextMeshProUGUI mStepButtonTextField;

    [SerializeField] private List<Step> mStepList = new List<Step>();
    [SerializeField] private GameManager mGameManager;

    private int _mCurrentStepIndex = 0;

    public void Next()
    {
        if (_mCurrentStepIndex < mStepList.Count - 1)
        {
            mStepList[_mCurrentStepIndex].stepObject.SetActive(false);
            _mCurrentStepIndex = (_mCurrentStepIndex + 1) % mStepList.Count;
            mStepList[_mCurrentStepIndex].stepObject.SetActive(true);
            mStepButtonTextField.text = mStepList[_mCurrentStepIndex].buttonText;
            if (_mCurrentStepIndex == mStepList.Count - 2)
            {
                mGameManager.mContinueButtonTextField.text = "Start Game!";
            }
        }
        else
        {
            gameObject.SetActive(false);
            mGameManager.OnGameStart();
        }
    }
    
    public void SkipToLast()
    {
        mStepList[_mCurrentStepIndex].stepObject.SetActive(false);
        _mCurrentStepIndex = mStepList.Count - 1;
        mStepList[_mCurrentStepIndex].stepObject.SetActive(true);
        mStepButtonTextField.text = mStepList[_mCurrentStepIndex].buttonText;
    }   
}