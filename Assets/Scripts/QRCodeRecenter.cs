using System.Collections.Generic;
using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;

public class QRCodeRecenter : MonoBehaviour
{
    [SerializeField]
    private ARSession session;
    [SerializeField]
    private XROrigin origin;
    [SerializeField]
    private ARCameraManager cameraManager;
    [SerializeField]
    private List<Target> navigationTargetObjects = new List<Target>();

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader = new BarcodeReader(); // to create barcode reader instance

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetQrCodeRecenterTarget("Xerox Machine");
        }
    }

    private void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        var conversionParams = new XRCpuImage.ConversionParams
        {
            // to get entire image
            inputRect = new RectInt(0, 0, image.width, image.height),

            // to downsample it by 2
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

            // choosing rgba format
            outputFormat = TextureFormat.RGBA32,

            // to flip across vertical axis / mirror image
            transformation = XRCpuImage.Transformation.MirrorY
        };

        // to see how many bytes we need to store in final image
        int size = image.GetConvertedDataSize(conversionParams);

        // to allocate buffer to store store image
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        // to extract image data
        image.Convert(conversionParams, buffer);

        // since image was converted to RGBA32 format and written into provided buffer
        // so we can dispose of XRCpuImage, such that it does not leak resources
        image.Dispose();

        // now we can process image, pass it to computer vision algo, etc.
        // we will apply texture to visualize it
        cameraImageTexture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false);

        cameraImageTexture.LoadRawTextureData(buffer);
        cameraImageTexture.Apply();

        // done with temp data so we can dispose it
        buffer.Dispose();

        // to detect and decode barcode inside bitmap
        var result = reader.Decode(
            cameraImageTexture.GetPixels32(),
            cameraImageTexture.width,
            cameraImageTexture.height);

        // result
        if (result != null)
        {
            SetQrCodeRecenterTarget(result.Text);
        }
    }

    private void SetQrCodeRecenterTarget(string targetText)
    {
        Target currentTarget = navigationTargetObjects.Find(x => x.Name.ToLower().Equals(targetText.ToLower()));

        if (currentTarget != null)
        {
            // to reset position and rotation of ARSession
            session.Reset();

            // to add offset for recentering
            origin.transform.position = currentTarget.PositionObject.transform.position;
            origin.transform.rotation = currentTarget.PositionObject.transform.rotation;
        }
    }
}
