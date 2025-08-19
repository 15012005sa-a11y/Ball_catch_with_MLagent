using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadFirstLevel : MonoBehaviour 
{
	private bool levelLoaded = false;
	
	
	void Update() 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(!levelLoaded && manager && KinectManager.IsKinectInitialized())
		{
			levelLoaded = true;

			Debug.Log($"Loading level 1");
			SceneManager.LoadScene(1);
		}
	}
	
}
