using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pitablock.Controllers
{
    public class ParticleController : MonoBehaviour
    {
        public static ParticleController Instance { get; private set; }

        [SerializeField] private int poolSize = 4;

        private readonly Queue<ParticleSystem> lineClearPool = new();
        private ParticleSystem magicTemplate;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildPools();
        }

        private void BuildPools()
        {
            for (var i = 0; i < poolSize; i++)
            {
                var ps = CreateStarBurstParticle($"LineClear_{i}");
                ps.gameObject.SetActive(false);
                lineClearPool.Enqueue(ps);
            }

            magicTemplate = CreateMagicParticle("MagicTemplate");
            magicTemplate.gameObject.SetActive(false);
        }

        public void PlayLineClearEffect(int rowIndex, Vector3 worldPosition)
        {
            if (lineClearPool.Count == 0)
            {
                return;
            }

            var ps = lineClearPool.Dequeue();
            ps.transform.position = worldPosition;
            ps.gameObject.SetActive(true);
            ps.Play();
            StartCoroutine(ReturnToPool(ps, ps.main.duration + ps.main.startLifetime.constantMax));
        }

        public void PlayMagicEffect(Vector3 position)
        {
            if (magicTemplate == null)
            {
                return;
            }

            var instance = Instantiate(magicTemplate, position, Quaternion.identity);
            instance.gameObject.SetActive(true);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration + instance.main.startLifetime.constantMax);
        }

        private IEnumerator ReturnToPool(ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            lineClearPool.Enqueue(ps);
        }

        private static ParticleSystem CreateStarBurstParticle(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Instance != null ? Instance.transform : null);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 2.5f;
            main.startSize = 0.15f;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6f, 0.2f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            return ps;
        }

        private static ParticleSystem CreateMagicParticle(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Instance != null ? Instance.transform : null);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 1.2f;
            main.startSize = 0.2f;
            main.maxParticles = 20;
            main.startColor = new Color(1f, 0.85f, 0.3f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            return ps;
        }
    }
}
