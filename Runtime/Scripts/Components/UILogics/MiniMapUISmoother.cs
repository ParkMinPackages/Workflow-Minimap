using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	[RequireComponent(typeof(MiniMapUI))]
	public sealed class MiniMapUISmoother : DependencyBehaviour, IMiniMapUI, IR3PostLateUpdatable
	{
		// - Public Methods -
		public void Initialize(MiniMapUI miniMapUI, float smoothness = 10) {
			_miniMapUI = miniMapUI;
			_smoothness = smoothness;
		}
		public override void ValidateDependencies() {
			if (_miniMapUI == null)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not assigned.");
			if (_smoothness <= 0f)
				throw new ArgumentOutOfRangeException(nameof(Smoothness));
		}

		public void SetView(
			Vector3 center,
			float rotation,
			float viewWorldHeight
		) {
			_targetCenter = center;
			_targetRotation = rotation;
			_targetViewWorldHeight = Mathf.Max(0.01f, viewWorldHeight);
			_hasTargetView = true;
		}

		public void SetCenter(Vector3 center) {
			SetView(center, Rotation, ViewWorldHeight);
		}
		public void SetRotation(float rotation) {
			SetView(Center, rotation, ViewWorldHeight);
		}
		public void SetViewWorldHeight(float viewWorldHeight) {
			SetView(Center, Rotation, viewWorldHeight);
		}

		public void SnapToTargetView() {
			if (_hasTargetView == false)
				return;

			_hasTargetView = false;
			_miniMapUI.SetView(_targetCenter, _targetRotation, _targetViewWorldHeight);
		}

		// - Public Properties -
		public MiniMapUI MiniMapUI
		{
			get { return _miniMapUI; }
		}
		public Vector3 Center
		{
			get { return _hasTargetView ? _targetCenter : _miniMapUI.Center; }
		}
		public float Rotation
		{
			get { return _hasTargetView ? _targetRotation : _miniMapUI.Rotation; }
		}
		public float ViewWorldHeight
		{
			get { return _hasTargetView ? _targetViewWorldHeight : _miniMapUI.ViewWorldHeight; }
		}
		public float ViewAspectRatio
		{
			get { return _miniMapUI.ViewAspectRatio; }
		}
		public float Smoothness
		{
			get { return _smoothness; }
			set
			{
				if (value <= 0f)
					throw new ArgumentOutOfRangeException(nameof(value));

				_smoothness = value;
			}
		}

		// - Handler -
		void IR3PostLateUpdatable.R3PostLateUpdate() {
			UpdateSmoothedView();
		}

		// - Internals -
		[Title(Headers.Injectable)]
		[SerializeField] MiniMapUI _miniMapUI;

		[Title(Headers.Settings)]
		[SerializeField, Min(0.01f)] float _smoothness = 10f;

		Vector3 _targetCenter;
		float _targetRotation;
		float _targetViewWorldHeight;
		bool _hasTargetView;

		void UpdateSmoothedView() {
			if (_hasTargetView == false)
				return;
			if (_miniMapUI.Center == _targetCenter &&
			    Mathf.Approximately(_miniMapUI.Rotation, _targetRotation) &&
			    Mathf.Approximately(_miniMapUI.ViewWorldHeight, _targetViewWorldHeight))
				return;

			float interpolation = 1f - Mathf.Exp(-_smoothness * Time.deltaTime);
			Vector3 center = Vector3.Lerp(_miniMapUI.Center, _targetCenter, interpolation);
			float rotation = Mathf.LerpAngle(_miniMapUI.Rotation, _targetRotation, interpolation);
			float viewWorldHeight = Mathf.Lerp(_miniMapUI.ViewWorldHeight, _targetViewWorldHeight, interpolation);
			if ((center - _targetCenter).sqrMagnitude <= 0.000001f)
				center = _targetCenter;
			if (Mathf.Abs(Mathf.DeltaAngle(rotation, _targetRotation)) <= 0.001f)
				rotation = _targetRotation;
			if (Mathf.Abs(viewWorldHeight - _targetViewWorldHeight) <= 0.001f)
				viewWorldHeight = _targetViewWorldHeight;
			_miniMapUI.SetView(center, rotation, viewWorldHeight);
		}
	}
}
