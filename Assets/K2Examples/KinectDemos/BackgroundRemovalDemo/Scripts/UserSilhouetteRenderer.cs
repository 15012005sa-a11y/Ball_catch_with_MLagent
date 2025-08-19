using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// UserSilhouetteRenderer renders the given user's silhouette in color-camera resolution on a RawImage texture (defaults to the RawImage-component).
/// Please don't forget to set 'Compute user map' to 'User texture' or 'Body texture'.
/// </summary>
public class UserSilhouetteRenderer : MonoBehaviour
{
    [Tooltip("Index of the player, tracked by this component. 0 - the 1st player, 1 - the 2nd player, etc.")]
    public int playerIndex = 0;

    [Tooltip("UI-Raw Image to display the user's silhouette")]
    public RawImage rawImage;


    // local variables
    private KinectManager _kinectManager = null;
    private KinectInterop.SensorData _sensorData = null;

    private long _lastBodyFrameTime = 0;
    private float[] _bodyIndexBufferData = null;
    private RenderTexture _bodyIndexTexture = null;
    private RenderTexture _alphaBodyTexture = null;


    void Start()
    {
        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
        }

        _kinectManager = KinectManager.Instance;
        if (_sensorData == null)
        {
            _sensorData = _kinectManager.GetSensorData();
        }

        // create alphaBodyMaterial if needed
        if (_sensorData != null && _sensorData.alphaBodyMaterial == null)
        {
            _sensorData.alphaBodyMaterial = new Material(Shader.Find("Kinect/Color2BodyShader"));

            _sensorData.alphaBodyMaterial.SetFloat("_ColorResX", (float)_sensorData.colorImageWidth);
            _sensorData.alphaBodyMaterial.SetFloat("_ColorResY", (float)_sensorData.colorImageHeight);
            _sensorData.alphaBodyMaterial.SetFloat("_DepthResX", (float)_sensorData.depthImageWidth);
            _sensorData.alphaBodyMaterial.SetFloat("_DepthResY", (float)_sensorData.depthImageHeight);

            _sensorData.color2DepthBuffer = new ComputeBuffer(_sensorData.colorImageWidth * _sensorData.colorImageHeight, sizeof(float) * 2);
            _sensorData.alphaBodyMaterial.SetBuffer("_DepthCoords", _sensorData.color2DepthBuffer);
        }
    }

    private void OnDestroy()
    {
        // release textures
        _bodyIndexTexture?.Release();
        _bodyIndexTexture = null;

        _alphaBodyTexture?.Release();
        _alphaBodyTexture = null;
    }

    void Update()
    {
        if (_kinectManager == null || !_kinectManager.IsInitialized())
            return;

        // check the color2DepthCoords array
        if (_sensorData.color2DepthCoords == null || _sensorData.color2DepthCoords.Length != (_sensorData.colorImageWidth * _sensorData.colorImageHeight))
        {
            _sensorData.color2DepthCoords = new Vector2[_sensorData.colorImageWidth * _sensorData.colorImageHeight];

            _sensorData.color2DepthBuffer?.Dispose();
            _sensorData.color2DepthBuffer = new ComputeBuffer(_sensorData.colorImageWidth * _sensorData.colorImageHeight, sizeof(float) * 2);
            _sensorData.alphaBodyMaterial.SetBuffer("_DepthCoords", _sensorData.color2DepthBuffer);
        }

        // check for body frame update
        long bodyFrameTime = _kinectManager.GetBodyFrameTimestamp();
        if (_lastBodyFrameTime == bodyFrameTime)
            return;

        _lastBodyFrameTime = bodyFrameTime;

        long userId = _kinectManager.GetUserIdByIndex(playerIndex);
        int bodyIndex = _kinectManager.GetBodyIndexByUserId(userId);
        
        UpdateBodySilhouette((byte)bodyIndex);
    }


    // updates user body silhouette and displays it on the texture
    private void UpdateBodySilhouette(byte userBI)
    {
        int iBodyIndexLength = _sensorData.bodyIndexImage.Length;

        if (_bodyIndexBufferData == null)
        {
            _bodyIndexBufferData = new float[iBodyIndexLength];
        }

        for (int i = 0; i < iBodyIndexLength; i++)
        {
            byte bufBI = _sensorData.bodyIndexImage[i];
            _bodyIndexBufferData[i] = bufBI == userBI ? bufBI : 255f;
        }

        if(_bodyIndexTexture == null || _bodyIndexTexture.width != _sensorData.depthImageWidth || _bodyIndexTexture.height != _sensorData.depthImageHeight)
        {
            _bodyIndexTexture?.Release();
            _bodyIndexTexture = new RenderTexture(_sensorData.depthImageWidth, _sensorData.depthImageHeight, 0);
        }

        _sensorData.bodyIndexBuffer.SetData(_bodyIndexBufferData);
        Graphics.Blit(null, _bodyIndexTexture, _sensorData.bodyIndexMaterial);

        if (_alphaBodyTexture == null || _alphaBodyTexture.width != _sensorData.colorImageWidth || _alphaBodyTexture.height != _sensorData.colorImageHeight)
        {
            _alphaBodyTexture?.Release();
            _alphaBodyTexture = new RenderTexture(_sensorData.colorImageWidth, _sensorData.colorImageHeight, 0);

            if(rawImage != null)
            {
                rawImage.texture = _alphaBodyTexture;
                rawImage.rectTransform.localScale = _kinectManager.GetColorImageScale();
                rawImage.color = Color.white;
            }
        }

        if (_sensorData.alphaBodyMaterial != null && _sensorData.color2DepthCoords != null)
        {
            _sensorData.color2DepthBuffer.SetData(_sensorData.color2DepthCoords);

            _sensorData.alphaBodyMaterial.SetTexture("_BodyTex", _bodyIndexTexture);
            Graphics.Blit(null, _alphaBodyTexture, _sensorData.alphaBodyMaterial);
        }
    }

}
