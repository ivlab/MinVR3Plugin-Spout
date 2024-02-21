
using UnityEngine;


/// <summary>
/// https://forum.unity.com/threads/rendering-into-part-of-a-render-texture.425371/
/// </summary>
[RequireComponent(typeof(Camera))]
public class UseViewportRectInTargetTexture : MonoBehaviour
{
    private void Start()
    {
        // guaranteed to succeed since the script requires a Camera component
        cameraComponent = GetComponent<Camera>();
    }

    private void OnPreRender()
    {
        if (cameraComponent.targetTexture != null) {
            cameraComponent.SetTargetBuffers(
                cameraComponent.targetTexture.colorBuffer,
                cameraComponent.targetTexture.depthBuffer
            );
        }
    }

    private Camera cameraComponent;
}
