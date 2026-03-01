using System;
using System.Collections;
using System.Collections.Generic;
using Base.PoolingSystem;
using Playable.Gameplay.Loot;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ParticleSystem))]
public class LootHeap : MonoBehaviour, IMonoPoolObject
{
    [Header("Bounds (XZ)")] 
    [SerializeField] private bool _useBounds;
    [SerializeField] private float _maxX = Mathf.Infinity;
    [SerializeField] private float _minX = -Mathf.Infinity;
    [SerializeField] private float _maxZ = Mathf.Infinity;
    [SerializeField] private float _minZ = -Mathf.Infinity;

    private Settings _settings;
    private ParticleSystem _ps;
    private ParticleSystem.Particle[] _particles;
    private readonly List<int> _exploding = new List<int>();
    private readonly List<int> _flying = new List<int>();
    private Transform _flyTarget;
    private PState[] _state;
    private Transform[] _initialConfigs;
    private ILootCollector _collector;
    
    public void OnSpawnFromPool()
    {
        
    }

    public void OnReturnToPool()
    {
        
    }
    
    public void Init(Settings settings)
    {
        _settings = settings;
    }

    public void CollectLoot(Transform[] itemPoints, ILootCollector collector, Action<LootHeap, ILootCollector> onComplete)
    {
        _collector = collector;
        _initialConfigs = itemPoints;
        
        var lootCollectPoint = collector.GetLootCollectPoint(0);

        if (collector.LootCollectPointsCount > 0)
        {
            var startPos = transform.position;
            var min = Vector3.SqrMagnitude(lootCollectPoint.position - startPos);
            
            float f;
            for (int i = 1; i < collector.LootCollectPointsCount; i++)
            {
                f = Vector3.SqrMagnitude(collector.GetLootCollectPoint(i).position - startPos);
                if (f >= min)
                    continue;
                
                min = f;
                lootCollectPoint = collector.GetLootCollectPoint(i);
            }
        }
        
        OnHarvestByHero(lootCollectPoint, onComplete);
    }

    private void OnHarvestByHero(Transform targetPoint, Action<LootHeap, ILootCollector> onComplete)
    {
        _flyTarget = targetPoint;
        InitParticles();
        StartCoroutine(HarvestRoutine(onComplete));
    }

    private void InitParticles()
    {
        if(_ps == null)
            _ps = GetComponent<ParticleSystem>();
        
        var main = _ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;
        main.startRotation3D = true;
        main.startSize3D = true;

        var n = Mathf.Max(1, _initialConfigs?.Length ?? 0);
        _particles = new ParticleSystem.Particle[n];
        _state = new PState[n];

        _ps.Clear();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        _ps.Emit(n);
        _ps.GetParticles(_particles);
        
        for (var i = 0; i < n; i++)
        {
            var p = _particles[i];
            p.position = _initialConfigs[i].position;
            p.velocity = Vector3.zero;
            p.startLifetime = Mathf.Infinity;
            p.remainingLifetime = Mathf.Infinity;
            
            p.startSize3D = _initialConfigs[i].localScale == Vector3.zero 
                ? Vector3.one : _initialConfigs[i].localScale;
            
            p.rotation3D = _initialConfigs[i].eulerAngles;
            p.startColor = Color.white;
            
            _particles[i] = p;

            _state[i] = new PState
            {
                mode = Mode.Idle,
                startPosition = _initialConfigs[i].position,
                startRotEuler = _initialConfigs[i].eulerAngles,
                startScale = p.startSize3D,
                vel = Vector3.zero,
                angVelDeg = Vector3.zero,
                progress = 0f,
                flyStartWorld = p.position
            };
        }
        
        _ps.SetParticles(_particles, n);
        _ps.Play();
    }
    
    private void Update()
    {
        if(_initialConfigs == null) 
            return;
        
        _ps.SetParticles(_particles, _initialConfigs.Length);
    }

    private IEnumerator HarvestRoutine(Action<LootHeap, ILootCollector> onComplete)
    {
        _exploding.Clear();
        _flying.Clear();

        Vector3 velocity;

        for (var i = 0; i < _state.Length; i++)
        {
            _state[i].mode = Mode.Explode;

            velocity = (_state[i].startPosition - transform.position).normalized 
                       * (_settings.StartVelocity + Random.value);
            
            velocity.y += Random.value * 3f;
            velocity.x *= Random.value * 2f;
            velocity.z *= Random.value * 2f;
            _state[i].vel = velocity;
            _state[i].angVelDeg = Random.onUnitSphere * _settings.StartAngularVelocity;
            _particles[i].startSize3D = _state[i].startScale;
            _exploding.Add(i);
        }

        yield return FlyRoutine();
        onComplete?.Invoke(this, _collector);
    }

    private IEnumerator FlyRoutine()
    {
        var waitBeforeFly = _settings.WaitBeforeFly;
        var tWait = 0f;
        
        while (tWait < waitBeforeFly)
        {
            tWait += Time.deltaTime;
            SimulateExplode(Time.deltaTime);
            yield return null;
        }

        var order = new List<int>(_exploding);
        for (var i = 0; i < order.Count; i++)
        {
            var j = Random.Range(i, order.Count);
            (order[i], order[j]) = (order[j], order[i]);
        }

        var batch = 0;
        var idx = 0;
        var tPause = 0f;
        
        while (order.Count > 0)
        {
            idx = order[order.Count - 1];
            order.RemoveAt(order.Count - 1);

            _state[idx].mode = Mode.Fly;
            _state[idx].progress = 0f;
            _state[idx].flyStartWorld = _particles[idx].position;

            _exploding.Remove(idx);
            _flying.Add(idx);

            if (++batch % _settings.FlyBatchSize == 0)
            {
                tPause = 0f;
                while (tPause < _settings.FlyBatchPause)
                {
                    tPause += Time.deltaTime;

                    SimulateExplode(Time.deltaTime);
                    SimulateFly(Time.deltaTime);

                    yield return null;
                }
            }
            else
            {
                SimulateExplode(Time.deltaTime);
                SimulateFly(Time.deltaTime);
                yield return null;
            }
        }

        while (_flying.Count > 0)
        {
            SimulateFly(Time.deltaTime);
            yield return null;
        }
    }

    private void RemoveSwapBack(List<int> list, int index)
    {
        list[index] = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
    }

    private void SimulateExplode(float dt)
    {
        if (_exploding.Count == 0) 
            return;

        Vector3 pos;
        Vector3 euler;
        var expIdx = 0;

        for (var idx = 0; idx < _exploding.Count; idx++)
        {
            expIdx = _exploding[idx];
            
            if (_state[expIdx].mode != Mode.Explode) 
                continue;

            pos = _particles[expIdx].position + _state[expIdx].vel * dt;
            euler = _particles[expIdx].rotation3D + _state[expIdx].angVelDeg * dt;

            _state[expIdx].vel += _settings.Gravity * dt;

            if (pos.y < _settings.GroundY)
            {
                pos.y = _settings.GroundY;
                _state[expIdx].vel.y = -_state[expIdx].vel.y * _settings.Bounciness;
                _state[expIdx].vel.x *= _settings.FrictionXZ;
                _state[expIdx].vel.z *= _settings.FrictionXZ;
                if (Mathf.Abs(_state[expIdx].vel.y) < 0.1f) _state[expIdx].vel.y = 0f;
            }

            if (_useBounds)
            {
                if (pos.x > _maxX)
                {
                    pos.x = _maxX;
                    _state[expIdx].vel.x = -_state[expIdx].vel.x * _settings.Bounciness;
                }

                if (pos.x < _minX)
                {
                    pos.x = _minX;
                    _state[expIdx].vel.x = -_state[expIdx].vel.x * _settings.Bounciness;
                }

                if (pos.z > _maxZ)
                {
                    pos.z = _maxZ;
                    _state[expIdx].vel.z = -_state[expIdx].vel.z * _settings.Bounciness;
                }

                if (pos.z < _minZ)
                {
                    pos.z = _minZ;
                    _state[expIdx].vel.z = -_state[expIdx].vel.z * _settings.Bounciness;
                }
            }

            _particles[expIdx].position = pos;
            _particles[expIdx].rotation3D = euler;
        }
    }

    private void SimulateFly(float dt)
    {
        var t = 0f;
        Vector3 target;
        Vector3 startW;
        Vector3 p;
        var fadeT = 0f;
        var s = 0f;
        
        for (var k = _flying.Count - 1; k >= 0; k--)
        {
            if (_state[_flying[k]].mode != Mode.Fly)
            {
                RemoveSwapBack(_flying, k);
                continue;
            }

            _state[_flying[k]].progress = Mathf.Min(_state[_flying[k]].progress + dt * _settings.FlySpeed, 1f);
            t = OutSine(_state[_flying[k]].progress);

            target = _flyTarget != null ? _flyTarget.position : transform.position;
            startW = _state[_flying[k]].flyStartWorld;

            p = Vector3.Lerp(startW, target, t);
            p.y += Mathf.Sin(t * Mathf.PI) * _settings.FlyArcHeight;
            
            fadeT = _state[_flying[k]].progress > _settings.ScaleFadeStart
                ? Mathf.InverseLerp(_settings.ScaleFadeStart, 1f, _state[_flying[k]].progress) : 0f;
            
            s = Mathf.Lerp(1f, 0f, fadeT);

            _particles[_flying[k]].position = p;
            _particles[_flying[k]].startSize3D = _state[_flying[k]].startScale * s;

            if (!(_state[_flying[k]].progress >= 1f)) 
                continue;
            
            _state[_flying[k]].mode = Mode.Idle;
            RemoveSwapBack(_flying, k);
        }
    }
    
    private static float OutSine(float t) => Mathf.Sin(t * Mathf.PI * 0.5f);

    private static float OutBack(float t, float s = 1.70158f)
    {
        t -= 1f;
        return (t * t * ((s + 1f) * t + s) + 1f);
    }
    
    private enum Mode : byte
    {
        Idle,
        Explode,
        Fly
    }

    private struct PState
    {
        public Mode mode;
        public Vector3 startPosition;
        public Vector3 startRotEuler;
        public Vector3 startScale;
        public Vector3 vel;
        public Vector3 angVelDeg;
        public float progress;
        public Vector3 flyStartWorld;
    }
    
    [Serializable]
    public class Settings
    {
        [Header("Physics (explode)")] 
        public Vector3 Gravity = new Vector3(0, -15.81f, 0);
        public float GroundY = 0.1f;
        public float Bounciness = 0.6f;
        public float FrictionXZ = 0.95f;
        public float StartVelocity = 3f;
        public float StartAngularVelocity = 180f;
        
        [Header("Fly phase")] 
        public float FlySpeed = 3f;
        public float FlyArcHeight = 0.5f;
        [SerializeField, Range(0f, 0.99f)] public float ScaleFadeStart = 0.6f;
        
        [Header("Timings (seconds)")] 
        public float WaitBeforeFly = 0.5f;

        [Header("Batching")] 
        public int FlyBatchSize = 3;
        public float FlyBatchPause = 0.03f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_useBounds) return;

        if (float.IsInfinity(_minX) || float.IsInfinity(_maxX) ||
            float.IsInfinity(_minZ) || float.IsInfinity(_maxZ)) return;

        var width = Mathf.Abs(_maxX - _minX);
        var depth = Mathf.Abs(_maxZ - _minZ);
        if (width <= 0f || depth <= 0f) return;
        
        var _groundY = _settings?.GroundY ?? 0.1f;

        var p00 = new Vector3(_minX, _groundY, _minZ);
        var p01 = new Vector3(_minX, _groundY, _maxZ);
        var p11 = new Vector3(_maxX, _groundY, _maxZ);
        var p10 = new Vector3(_maxX, _groundY, _minZ);

        var prev = Gizmos.color;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);

        Gizmos.DrawLine(p00, p01);
        Gizmos.DrawLine(p01, p11);
        Gizmos.DrawLine(p11, p10);
        Gizmos.DrawLine(p10, p00);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawLine(p00, p11);
        Gizmos.DrawLine(p01, p10);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        var postH = 0.3f;
        Gizmos.DrawLine(p00, p00 + Vector3.up * postH);
        Gizmos.DrawLine(p01, p01 + Vector3.up * postH);
        Gizmos.DrawLine(p11, p11 + Vector3.up * postH);
        Gizmos.DrawLine(p10, p10 + Vector3.up * postH);

        var center = new Vector3((_minX + _maxX) * 0.5f, _groundY, (_minZ + _maxZ) * 0.5f);
        Gizmos.DrawWireSphere(center, 0.05f);
        Gizmos.color = prev;
    }
#endif
}