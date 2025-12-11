using UnityEngine;
using UnityEditor;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Mobile/Optimization/Pixel Quality Scale")]
    public class PixelQualityScale : MonoBehaviour
    {

        private Resolution start_current_resolution;
        [Space]
        [Header("Is useful for increasing mobile performance")]
        [Header("This will reduce the resolution up to 2 times")]

        [Range(3, 1)]
        public float ResolutionQuality;
        void Start()
        {
            SetRenderResolutionQuality(Display.main.systemWidth, Display.main.systemHeight, ResolutionQuality);
        }
        private void SetRenderResolutionQuality(int width, int height, float downScale)
        {
            //Load current resolution
            start_current_resolution.width = width;
            start_current_resolution.height = height;

            //divide the resolution
            int w = (int)((float)start_current_resolution.width / downScale);
            int h = (int)((float)start_current_resolution.height / downScale);

            //Set New Resolution
            Screen.SetResolution(w, h, true);
            print("Resolution: Width: " + w + "Height: " + h);
        }
    }

}