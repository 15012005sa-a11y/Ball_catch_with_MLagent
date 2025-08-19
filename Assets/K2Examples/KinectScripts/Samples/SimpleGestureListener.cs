using UnityEngine;
//using Windows.Kinect;
using System.Collections;
using System.Collections.Generic;
using System;


public class SimpleGestureListener : MonoBehaviour, KinectGestures.GestureListenerInterface
{
	[Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
	public int playerIndex = 0;

    [Tooltip("List of the gestures to detect.")]
    public List<KinectGestures.Gestures> detectGestures = new List<KinectGestures.Gestures>();

    [Tooltip("UI-Text to display gesture-listener messages and gesture information.")]
	public UnityEngine.UI.Text gestureInfo;
	
	// private bool to track if progress message has been displayed
	private bool progressDisplayed;
	private float progressGestureTime;


    // invoked when a new user is detected
    public void UserDetected(long userId, int userIndex)
	{
        if (userIndex == playerIndex)
        {
            // as an example - detect these user specific gestures
            KinectManager kinectManager = KinectManager.Instance;

            foreach (KinectGestures.Gestures gesture in detectGestures)
            {
                kinectManager.DetectGesture(userId, gesture);
            }
        }

        if (gestureInfo != null)
		{
			//gestureInfo.text = "Swipe, Jump, Squat or Lean.";
		}
	}

    // invoked when the user is lost
    public void UserLost(long userId, int userIndex)
	{
		if (userIndex != playerIndex)
			return;

		if(gestureInfo != null)
		{
			gestureInfo.text = string.Empty;
		}
	}

    // invoked to report gesture progress. important for the continuous gestures, because they never complete.
    public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              float progress, KinectInterop.JointType joint, Vector3 screenPos)
	{
		if (userIndex != playerIndex)
			return;

        // check for continuous gestures
        switch (gesture)
        {
            case KinectGestures.Gestures.ZoomOut:
            case KinectGestures.Gestures.ZoomIn:
                if (progress > 0.5f && gestureInfo != null)
                {
                    string sGestureText = string.Format("{0} - {1:F0}%", gesture, screenPos.z * 100f);
                    gestureInfo.text = sGestureText;

                    progressDisplayed = true;
                    progressGestureTime = Time.realtimeSinceStartup;
                }
                break;

            case KinectGestures.Gestures.Wheel:
            case KinectGestures.Gestures.LeanLeft:
            case KinectGestures.Gestures.LeanRight:
            case KinectGestures.Gestures.LeanForward:
            case KinectGestures.Gestures.LeanBack:
                if (progress > 0.5f && gestureInfo != null)
                {
                    string sGestureText = string.Format("{0} - {1:F0} degrees", gesture, screenPos.z);
                    gestureInfo.text = sGestureText;

                    progressDisplayed = true;
                    progressGestureTime = Time.realtimeSinceStartup;
                }
                break;

            case KinectGestures.Gestures.Run:
                if (progress > 0.5f && gestureInfo != null)
                {
                    string sGestureText = string.Format("{0} - progress: {1:F0}%", gesture, progress * 100);
                    gestureInfo.text = sGestureText;

                    progressDisplayed = true;
                    progressGestureTime = Time.realtimeSinceStartup;
                }
                break;
        }
	}

    // invoked when a (discrete) gesture is complete.
    public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint, Vector3 screenPos)
	{
		if (userIndex != playerIndex)
			return false;

		if(progressDisplayed)
			return true;

		string sGestureText = gesture + " detected";
        Debug.Log(sGestureText);

        if (gestureInfo != null)
		{
			gestureInfo.text = sGestureText;
		}
		
		return true;
	}

    // invoked when a gesture gets cancelled by the user
    public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint)
	{
		if (userIndex != playerIndex)
			return false;

		if(progressDisplayed)
		{
			progressDisplayed = false;

			if(gestureInfo != null)
			{
				gestureInfo.text = String.Empty;
			}
		}
		
		return true;
	}

	public void Update()
	{
		if(progressDisplayed && ((Time.realtimeSinceStartup - progressGestureTime) > 2f))
		{
			progressDisplayed = false;
			
			if(gestureInfo != null)
			{
				gestureInfo.text = String.Empty;
			}

			Debug.Log("Forced progress to end.");
		}
	}
	
}
