using UnityEngine;

public class MuzzleFlashEffect : MonoBehaviour
{
    private float duration = 0.1f;
    private float timer = 0f;
    private Light flashLight;

    // Acepta un padre opcional para que el flash siga al arma
    public static GameObject CreateMuzzleFlash(Vector3 position, Quaternion rotation, float duration = 0.1f, Transform parent = null)
    {
        GameObject flashObject = new GameObject("MuzzleFlash");
        flashObject.transform.position = position;
        flashObject.transform.rotation = rotation;

        if (parent != null)
            flashObject.transform.SetParent(parent, worldPositionStays: true);

        MuzzleFlashEffect effect = flashObject.AddComponent<MuzzleFlashEffect>();
        effect.duration = duration;

        // Partículas
        GameObject particleObj = new GameObject("Particles");
        particleObj.transform.parent = flashObject.transform;
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        ConfigureParticleSystem(ps);
        effect.ConfigureRenderer(ps);

        // Luz de destello
        GameObject lightObj = new GameObject("FlashLight");
        lightObj.transform.parent = flashObject.transform;
        lightObj.transform.localPosition = Vector3.zero;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 3f;
        light.range = 8f;
        light.color = new Color(1f, 0.85f, 0.4f);
        effect.flashLight = light;

        return flashObject;
    }

    private void ConfigureRenderer(ParticleSystem ps)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        Shader shader = Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Legacy Shaders/Particles/Additive")
                     ?? Shader.Find("Mobile/Particles/Additive")
                     ?? Shader.Find("Unlit/Color");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = new Color(1f, 0.8f, 0.2f, 1f);
            renderer.material = mat;
        }
        else
        {
            Debug.LogWarning("[MuzzleFlash] No se encontró shader de partículas.");
        }

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void ConfigureParticleSystem(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.08f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.5f, 0.1f)
        );

        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 15, 20)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.01f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ps.Play();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (flashLight != null)
            flashLight.intensity = Mathf.Lerp(3f, 0f, timer / duration);

        if (timer >= duration + 0.1f)
            Destroy(gameObject);
    }
}