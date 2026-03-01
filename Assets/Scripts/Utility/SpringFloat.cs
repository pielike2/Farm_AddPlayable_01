using System;
using UnityEngine;

namespace Utility
{
	[Serializable]
	public class SpringClampSettings
	{
		[Header("Min")]
		public bool ClampMin;
		public float ClampMinValue;
		public bool ClampMinInitial;
		public bool ClampMinBounce;
		
		[Header("Max")]
		public bool ClampMax;
		public float ClampMaxValue = 10f;
		public bool ClampMaxInitial;
		public bool ClampMaxBounce;

		public bool ClampNeeded => ClampMin || ClampMax || ClampMinBounce || ClampMaxBounce;

		public virtual float GetTargetValue(float value, float initialValue)
		{
			var targetValue = value;
			
			var clampMinValue = ClampMinInitial ? initialValue : ClampMinValue;
			if (ClampMin && value < clampMinValue)
				targetValue = clampMinValue;
			
			var clampMaxValue = ClampMaxInitial ? initialValue : ClampMaxValue;
			if (ClampMax && value > clampMaxValue)
				targetValue = clampMaxValue;
			
			return targetValue;
		}
	}
	
	[Serializable]
	public class SpringFloat
	{
		[Tooltip("the dumping ratio determines how fast the spring will evolve after a disturbance. At a low value, it'll oscillate for a long time, while closer to 1 it'll stop oscillating quickly")]
		[Range(0.01f, 1f)]
		public float Damping = 0.4f;
		[Tooltip("the frequency determines how fast the spring will oscillate when disturbed, low frequency means less oscillations per second, high frequency means more oscillations per second")]
		public float Frequency = 6f;

		public float CurrentValue
		{
			get
			{
				return _returnCurrentValue;
			}
			set
			{
				_actualCurrentValue = value;
				_returnCurrentValue = value;
			}
		}

		public SpringClampSettings ClampSettings = new SpringClampSettings();
		
		public float TargetValue
		{
			get
			{
				return _targetValue;
			}
			set
			{
				_targetValue = ClampSettings.GetTargetValue(value, InitialValue);
			}
		}

		public float Velocity
		{
			get
			{
				return _velocity;
			}
			set
			{
				_velocity = value;
			}
		}
		
		public float InitialValue { get; protected set; }
		
		protected float _actualCurrentValue;
		protected float _returnCurrentValue;
		protected float _targetValue;
		protected float _velocity;

		public void UpdateSpringValue(float deltaTime)
		{
			SpringMath.Spring(ref _actualCurrentValue, TargetValue, ref _velocity, Damping, Frequency, deltaTime);
			_returnCurrentValue = _actualCurrentValue;
			
			if (ClampSettings.ClampNeeded)
				HandleClampMode();
		}

		protected virtual void HandleClampMode()
		{
			float minValue = ClampSettings.ClampMinInitial ? InitialValue : ClampSettings.ClampMinValue;
			float maxValue = ClampSettings.ClampMaxInitial ? InitialValue : ClampSettings.ClampMaxValue;
			
			if (ClampSettings.ClampMin && (_actualCurrentValue < minValue))
			{
				
				if (ClampSettings.ClampMinBounce)
				{
					_returnCurrentValue = Mathf.Abs(_actualCurrentValue - minValue) + minValue;
				}
				else
				{
					_returnCurrentValue = Mathf.Max(_actualCurrentValue, minValue);	
				}
			}
			
			if (ClampSettings.ClampMax && (_actualCurrentValue > maxValue))
			{
				if (ClampSettings.ClampMaxBounce)
				{
					_returnCurrentValue = maxValue - (_actualCurrentValue - maxValue);
				}
				else
				{
					_returnCurrentValue = Mathf.Min(_actualCurrentValue, maxValue);	
				}
			}
		}
		
		public void MoveToInstant(float newValue)
		{
			_actualCurrentValue = newValue;
			_returnCurrentValue = newValue;
			TargetValue = newValue;
			Velocity = 0;
		}

		public void Stop()
		{
			Velocity = 0f;
			TargetValue = _actualCurrentValue;
		}

		public void SetInitialValue(float newInitialValue)
		{
			InitialValue = newInitialValue;
		}

		public void RestoreInitialValue()
		{
			_actualCurrentValue = InitialValue;
			_returnCurrentValue = InitialValue;
			TargetValue = _actualCurrentValue;
			// UpdateSpringDebug();
		}

		public void SetCurrentValueAsInitialValue()
		{
			InitialValue = _actualCurrentValue;
		}
		
		public void MoveTo(float newValue)
		{
			TargetValue = newValue;
		}
		
		public void MoveToAdditive(float newValue)
		{
			TargetValue += newValue;
		}
		
		public void MoveToSubtractive(float newValue)
		{
			TargetValue -= newValue;
		}

		public void MoveToRandom(float min, float max)
		{
			TargetValue = UnityEngine.Random.Range(min, max);
		}

		public void Bump(float bumpAmount)
		{
			Velocity += bumpAmount;
		}

		public void BumpRandom(float min, float max)
		{
			Velocity += UnityEngine.Random.Range(min, max);
		}
		
		public void Finish()
		{
			Velocity = 0f;
			_actualCurrentValue = TargetValue;
			_returnCurrentValue = TargetValue;
		}
	}
}