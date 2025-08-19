using UnityEngine;
using System.Collections;


/// <summary>
/// Background color image is component that displays the color camera feed on GUI texture, usually the scene background.
/// </summary>
public class BackgroundColorImage : MonoBehaviour 
{
	[Tooltip("RawImage used to display the color camera feed.")]
	public UnityEngine.UI.RawImage backgroundImage;

    [Tooltip("RenderTexture to render the image.")]
    public RenderTexture backgroundTexture;

    [Tooltip("Opaqueness factor of the raw-image.")]
    [Range(0f, 1f)]
    public float opaqueness = 1.0f;


    void Start()
	{
		if (backgroundImage == null) 
		{
			backgroundImage = GetComponent<UnityEngine.UI.RawImage>();
		}
	}


	void Update () 
	{
		KinectManager kinectManager = KinectManager.Instance;

		if (kinectManager && kinectManager.IsInitialized()) 
		{
            Texture imageTex = kinectManager.GetUsersClrTex();

            if (backgroundImage && (backgroundImage.texture == null)) 
			{
				backgroundImage.texture = imageTex;
				backgroundImage.rectTransform.localScale = kinectManager.GetColorImageScale();
				backgroundImage.color = new Color(1f, 1f, 1f, opaqueness);  // Color.white;
            }

            if (imageTex != null && backgroundTexture != null)
            {
                Vector2 imageScale = kinectManager.GetColorImageScale();
                KinectInterop.TransformTexture(imageTex, backgroundTexture, 0, imageScale.x < 0f, imageScale.y < 0f, keepST: true);
            }
        }
    }
}
