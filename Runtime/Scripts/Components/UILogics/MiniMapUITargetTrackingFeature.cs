using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	public sealed class MiniMapUITargetTrackingFeature : Feature<IMiniMapUI>, IR3PreLateUpdatable
	{
		// - Public Methods -
		public override void ValidateDependencies() {
			base.ValidateDependencies();
			if (_trackingTarget == null)
				throw new InvalidOperationException($"{nameof(TrackingTarget)} is not assigned.");
		}

		// - Public Properties -
		public Transform TrackingTarget
		{
			get { return _trackingTarget; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_trackingTarget = value;
				Owner.SetCenter(_trackingTarget.position);
			}
		}

		// - Handler -
		protected override void Start() {
			base.Start();
			Owner.SetCenter(_trackingTarget.position);
		}
		void IR3PreLateUpdatable.R3PreLateUpdate() {
			if (_trackingTarget.gameObject.isStatic)
				return;

			Owner.SetCenter(_trackingTarget.position);
		}

		// - Internals -
		[Title(Headers.Injectable)]
		[SerializeField, FormerlySerializedAs("_target")] Transform _trackingTarget;
	}
}