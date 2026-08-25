using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	public sealed class MiniMapUITargetTrackingUpdater : DependencyBehaviour, IR3PreLateUpdatable
	{
		// - Public Methods -
		public void Initialize(IMiniMapUI miniMapUI, Transform target) {
			_miniMapUI = miniMapUI;
			_target = target;
			_staticTargetTracked = false;
		}

		public override void ValidateDependencies() {
			if (_miniMapUI == null)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not assigned.");
			if (_target == null)
				throw new InvalidOperationException($"{nameof(Target)} is not assigned.");
		}

		// - Public Properties -
		public IMiniMapUI MiniMapUI
		{
			get { return _miniMapUI; }
			set
			{
				if (ReferenceEquals(_miniMapUI, value))
					return;

				_miniMapUI = value;
				_staticTargetTracked = false;
			}
		}
		public Transform Target
		{
			get { return _target; }
			set
			{
				if (_target == value)
					return;

				_target = value;
				_staticTargetTracked = false;
			}
		}

		// - Handler -
		void Awake() {
			if (_miniMapUI == null && _miniMapUIObject != null) {
				MiniMapUI = _miniMapUIObject as IMiniMapUI;
			}
		}

		void IR3PreLateUpdatable.R3PreLateUpdate() {
			if (_target.gameObject.isStatic && _staticTargetTracked)
				return;

			_miniMapUI.SetCenter(_target.position);
			_staticTargetTracked = _target.gameObject.isStatic;
		}

		// - Internals -
		[Title(Headers.Injectable)]
		[SerializeField] UnityEngine.Object _miniMapUIObject;
		[SerializeField] Transform _target;

		IMiniMapUI _miniMapUI;
		bool _staticTargetTracked;
	}
}
