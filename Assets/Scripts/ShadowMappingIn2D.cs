using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof (Camera))]
public class ShadowMappingIn2D : MonoBehaviour
{
    [Range(1, 4096)]
    public int shadowMapResolution = 1024;

    public LayerMask shadowCastersLayer = ~0;

    private ComputeShader m_ComputeShader;
    private ComputeShader computeShader
    {
        get
        {
            if (m_ComputeShader == null)
            {
                m_ComputeShader = (ComputeShader)
                    Resources.Load("Shaders/ShadowMappingIn2D");
            }

            return m_ComputeShader;
        }
    }

    private Shader m_Shader;
    private Shader shader
    {
        get
        {
            if (m_Shader == null)
                m_Shader = Shader.Find("Hidden/Shadow Mapping in 2D");

            return m_Shader;
        }
    }

    private Material m_Material;
    private Material material
    {
        get
        {
            if (m_Material == null)
            {
                if (shader == null || shader.isSupported == false)
                    return null;

                m_Material = new Material(shader);
            }

            return m_Material;
        }
    }

    private Camera m_Camera;
    private new Camera camera
    {
        get
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();

            return m_Camera;
        }
    }

    private Camera m_ShadowCastersCamera;
    private Camera shadowCastersCamera
    {
        get
        {
            if (m_ShadowCastersCamera == null)
            {
                GameObject gameObject = new GameObject("Shadow Casters Camera");
                gameObject.hideFlags = HideFlags.HideAndDontSave;

                m_ShadowCastersCamera = gameObject.AddComponent<Camera>();
            }

            return m_ShadowCastersCamera;
        }
    }

    private RenderTexture m_ShadowCasters;
    private RenderTexture shadowCasters
    {
        get
        {
            if (m_ShadowCasters == null)
            {
                m_ShadowCasters = new RenderTexture(1024, 1024, 0,
                    RenderTextureFormat.RHalf, RenderTextureReadWrite.Default);

                m_ShadowCasters.filterMode = FilterMode.Bilinear;
                m_ShadowCasters.useMipMap = false;

                m_ShadowCasters.enableRandomWrite = true;
                m_ShadowCasters.hideFlags = HideFlags.HideAndDontSave;

                m_ShadowCasters.Create();
            }

            return m_ShadowCasters;
        }
    }

    private RenderTexture m_ShadowMap;
    private RenderTexture shadowMap
    {
        get
        {
            if (m_ShadowMap == null || m_ShadowMap.width != shadowMapResolution)
            {
                m_ShadowMap = new RenderTexture(shadowMapResolution, 1, 0,
                    RenderTextureFormat.RInt, RenderTextureReadWrite.Default);

                m_ShadowMap.filterMode = FilterMode.Point;
                m_ShadowMap.useMipMap = false;

                m_ShadowMap.enableRandomWrite = true;
                m_ShadowMap.hideFlags = HideFlags.HideAndDontSave;

                m_ShadowMap.Create();
            }

            return m_ShadowMap;
        }
    }

    public void OnDisable()
    {
        if (m_ShadowCastersCamera != null)
        {
            DestroyImmediate(m_ShadowCastersCamera.gameObject);
            m_ShadowCastersCamera = null;
        }

        if (m_ShadowCasters != null)
        {
            m_ShadowCasters.Release();
            m_ShadowCasters = null;
        }

        if (m_ShadowMap != null)
        {
            m_ShadowMap.Release();
            m_ShadowMap = null;
        }
    }

    public void OnPreCull()
    {
        shadowCastersCamera.CopyFrom(camera);
        shadowCastersCamera.enabled = false;

        shadowCastersCamera.renderingPath = RenderingPath.Forward;

        shadowCastersCamera.clearFlags = CameraClearFlags.SolidColor;
        shadowCastersCamera.backgroundColor = new Color(1f, 1f, 1f, 0f);

        shadowCastersCamera.cullingMask = shadowCastersLayer;
        shadowCastersCamera.targetTexture = shadowCasters;

        shadowCastersCamera.SetReplacementShader(shader, null);
        shadowCastersCamera.Render();
    }

    public void OnPreRender()
    {
        shadowMapResolution &= ~0x07;

        int kernel = computeShader.FindKernel("clearShadowMap");

        computeShader.SetTexture(kernel, "_ShadowMap", shadowMap);

        computeShader.Dispatch(kernel, shadowMap.width >> 3, 1, 1);

        kernel = computeShader.FindKernel("generateShadowMap");

        computeShader.SetTexture(kernel, "_ShadowCasters", shadowCasters);
        computeShader.SetTexture(kernel, "_ShadowMap", shadowMap);

        computeShader.Dispatch(kernel, shadowCasters.width >> 3,
            shadowCasters.height >> 3, 1);
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        material.SetTexture("_ShadowMap", shadowMap);

        Graphics.Blit(source, destination, material, 0);
    }
}
