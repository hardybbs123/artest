using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using System.Collections;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// This component tests getting the latest camera image
    /// and converting it to RGBA format. If successful,
    /// it displays the image on the screen as a RawImage
    /// and also displays information about the image.
    ///
    /// This is useful for computer vision applications where
    /// you need to access the raw pixels from camera image
    /// on the CPU.
    ///
    /// This is different from the ARCameraBackground component, which
    /// efficiently displays the camera image on the screen. If you
    /// just want to blit the camera texture to the screen, use
    /// the ARCameraBackground, or use Graphics.Blit to create
    /// a GPU-friendly RenderTexture.
    ///
    /// In this example, we get the camera image data on the CPU,
    /// convert it to an RGBA format, then display it on the screen
    /// as a RawImage texture to demonstrate it is working.
    /// This is done as an example; do not use this technique simply
    /// to render the camera image on screen.
    /// </summary>
    public class CPUImageCapture : MonoBehaviour
    {
        [SerializeField]        
        ARCameraManager m_CameraManager; 

        [SerializeField]        
        DynamicLibrary m_DynamicLibrary;

        [SerializeField]        
        public GameObject renCube;

        public DynamicLibrary dynamicLibrary
        {
            get => m_DynamicLibrary;
            set => m_DynamicLibrary = value;
        }


        bool isCapture = false;

        /// <summary>
        /// Get or set the <c>ARCameraManager</c>.
        /// </summary>
        public ARCameraManager cameraManager
        {
            get => m_CameraManager;
            set => m_CameraManager = value;
        }

        [SerializeField]
        RawImage m_RawCameraImage;

        /// <summary>
        /// The UI RawImage used to display the image on screen.
        /// </summary>
        public RawImage rawCameraImage
        {
            get => m_RawCameraImage;
            set => m_RawCameraImage = value;
        }
         
        [SerializeField]
        Text m_ImageInfo;

        /// <summary>
        /// The UI Text used to display information about the image on screen.
        /// </summary>
        public Text imageInfo
        {
            get => m_ImageInfo;
            set => m_ImageInfo = value;
        }

        [SerializeField]
        Button m_CaptureButton;

        /// <summary>
        /// The button that controls transformation selection.
        /// </summary>
        public Button transformationButton
        {
            get => m_CaptureButton;
            set => m_CaptureButton = value;
        }

        XRCpuImage.Transformation m_Transformation = XRCpuImage.Transformation.MirrorX ; //|XRCpuImage.Transformation.MirrorY

        /// <summary>
        /// Cycles the image transformation to the next case.
        /// </summary>
        public void Capture()
        {
            isCapture = true;
            //StartCoroutine(GetTexture2());
        }

        void OnEnable()
        {
            if (m_CameraManager != null)
            {
                m_CameraManager.frameReceived += OnCameraFrameReceived;
            }
        }

        void OnDisable()
        {
            if (m_CameraManager != null)
            {
                m_CameraManager.frameReceived -= OnCameraFrameReceived;
            }
        }

        unsafe void UpdateCameraImage()
        {
            // Attempt to get the latest camera image. If this method succeeds,
            // it acquires a native resource that must be disposed (see below).
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return;
            }

            // Display some information about the camera image
            m_ImageInfo.text = string.Format(
                "Image info:\n\twidth: {0}\n\theight: {1}\n\tplaneCount: {2}\n\ttimestamp: {3}\n\tformat: {4}",
                image.width, image.height, image.planeCount, image.timestamp, image.format);

            // Once we have a valid XRCpuImage, we can access the individual image "planes"
            // (the separate channels in the image). XRCpuImage.GetPlane provides
            // low-overhead access to this data. This could then be passed to a
            // computer vision algorithm. Here, we will convert the camera image
            // to an RGBA texture and draw it on the screen.

            // Choose an RGBA format.
            // See XRCpuImage.FormatSupported for a complete list of supported formats.
            var format = TextureFormat.RGBA32;

            if (m_CameraTexture == null || m_CameraTexture.width != image.width || m_CameraTexture.height != image.height)
            {
                m_CameraTexture = new Texture2D(image.width, image.height, format, false);
                //m_CameraTexture.isReadable = true;
            }

            // Convert the image to format, flipping the image across the Y axis.
            // We can also get a sub rectangle, but we'll get the full image here.
            var conversionParams = new XRCpuImage.ConversionParams(image, format, m_Transformation);

            // Texture2D allows us write directly to the raw texture data
            // This allows us to do the conversion in-place without making any copies.
            var rawTextureData = m_CameraTexture.GetRawTextureData<byte>();
            try
            {
                image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            }
            finally
            {
                // We must dispose of the XRCpuImage after we're finished
                // with it to avoid leaking native resources.
                image.Dispose();
            }

            // Apply the updated texture data to our texture
            m_CameraTexture.Apply(false,false);

            // Set the RawImage's texture so we can visualize it.
            m_RawCameraImage.texture = m_CameraTexture;

            if(m_DynamicLibrary)
            {
                m_DynamicLibrary.SendCaptureImage(m_CameraTexture,"hardyTest",m_CameraTexture.width);
                Debug.Log("hardy 1 SendCaptureImage");
            }
        }
  

        void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            if(isCapture)
            {
                UpdateCameraImage();  
                isCapture = false;          
            }
        }

        Texture2D m_CameraTexture;

        IEnumerator GetTexture() 
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture("https://192.168.2.205/ARF/test1.jpg");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.Log(www.error);
                Debug.Log("hardy   GetTexture fail" + www.error);
            }
            else {
                m_CameraTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                Debug.Log("hardy 5 GetTexture ok");
                //renCube.GetComponent<Renderer>().mater
                
                
                renCube.GetComponent<Renderer>().material.mainTexture = m_CameraTexture;

    

                if(m_DynamicLibrary)
                {
                    m_DynamicLibrary.SendCaptureImage(m_CameraTexture,"hardyTest",m_CameraTexture.width);
                    Debug.Log("hardy 1 SendCaptureImage");
                }

            }
        }

        IEnumerator GetTexture2() 
        {                
            m_CameraTexture = Resources.Load("test1") as Texture2D;
            renCube.GetComponent<Renderer>().material.mainTexture = m_CameraTexture;

            if(m_DynamicLibrary)
            {
                m_DynamicLibrary.SendCaptureImage(m_CameraTexture,"hardyTest",m_CameraTexture.width);
                Debug.Log("hardy 1 SendCaptureImage");
            }   
            yield return null;          
        }
    }
}
