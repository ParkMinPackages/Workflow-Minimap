using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	[DisallowMultipleComponent, RequireComponent(typeof(MiniMapUI))]
	public sealed class MiniMapTargetTrackingController : ExtendedBehaviour, IR3PreLateUpdatable
	{
		// - Public Methods -
		public void Track() {
			if (_target == null)
				return;
			if (_target.gameObject.isStatic && _staticTargetTracked)
				return;

			MiniMapUI.Center = _target.position;
			_staticTargetTracked = _target.gameObject.isStatic;
		}

		// - Public Properties -
		public IMiniMapUI MiniMapUI
		{
			get
			{
				if (_miniMapUI == null)
					throw new ArgumentNullException(nameof(_miniMapUI));

				return _miniMapUI;
			}
			set
			{
				_miniMapUI = value ?? throw new ArgumentNullException(nameof(value));
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
		void IR3PreLateUpdatable.R3PreLateUpdate() {
			Track();
		}

		// - Internals -
		[SerializeField] Transform _target;
		IMiniMapUI _miniMapUI;
		bool _staticTargetTracked;
	}
}
