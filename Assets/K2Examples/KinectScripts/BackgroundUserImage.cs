using UnityEngine;
using System.Collections;


/// <summary>
/// Background user image is component that displays the user image on GUI texture, usually the scene background.
/// </summary>
public class BackgroundUserImage : MonoBehaviour 
{
	[Tooltip("RawImage used to display the depth image.")]
	public UnityEngine.UI.RawImage backgroundImage;

	[Tooltip("Camera used to display the background image.")]
	public Camera backgroundCamera;

    [Tooltip("RenderTexture to render the image.")]
    public RenderTexture backgroundTexture;

    [Tooltip("Whether to use the texture-2d option of the user image (may lower the performance).")]
	public bool useTexture2D = false;

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
		KinectManager manager = KinectManager.Instance;

		if (manager && manager.IsInitialized()) 
		{
			Texture imageTex = !useTexture2D ? manager.GetUsersLblTex() : manager.GetUsersLblTex2D();
            KinectInterop.SensorData sensorData = manager.GetSensorData();

            if (backgroundImage && (backgroundImage.texture == null)) 
			{
				backgroundImage.texture = imageTex;
                backgroundImage.color = new Color(1f, 1f, 1f, opaqueness);  // Color.white;

				if (sensorData != null && sensorData.sensorInterface != null && backgroundCamera != null) 
				{
					// get depth image size
					int depthImageWidth = sensorData.depthImageWidth;
					int depthImageHeight = sensorData.depthImageHeight;

					// calculate insets
					Rect cameraRect = backgroundCamera.pixelRect;
					float rectWidth = cameraRect.width;
					float rectHeight = cameraRect.height;

					if (rectWidth > rectHeight)
						rectWidth = rectHeight * depthImageWidth / depthImageHeight;
					else
						rectHeight = rectWidth * depthImageHeight / depthImageWidth;

					float deltaWidth = cameraRect.width - rectWidth;
					float deltaHeight = cameraRect.height - rectHeight;

//					float leftX = deltaWidth / 2;
//					float rightX = -deltaWidth;
//					float bottomY = -deltaHeight / 2;
//					float topY = deltaHeight;
//
//					backgroundImage.pixelInset = new Rect(leftX, bottomY, rightX, topY);

					RectTransform rectImage = backgroundImage.GetComponent<RectTransform>();
					if (rectImage) 
					{
						rectImage.sizeDelta = new Vector2(-deltaWidth, -deltaHeight);
					}
				}
			}

            if (imageTex != null && backgroundTexture != null)
            {
                Vector2 imageScale = sensorData.depthImageScale;
                KinectInterop.TransformTexture(imageTex, backgroundTexture, 0, imageScale.x < 0f, imageScale.y < 0f, backgroundCamera == null);
            }
        }
    }
}
