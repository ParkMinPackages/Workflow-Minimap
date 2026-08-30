using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	public sealed class MiniMapUIFitUpdateFeature : Feature<MiniMapUIFitFeature>, IR3PreLateUpdatable
	{
		// - Public Methods -
		public override void ValidateDependencies() {
			base.ValidateDependencies();
			if (_pointA == null)
				throw new InvalidOperationException($"{nameof(PointA)} is not assigned.");
			if (_pointB == null)
				throw new InvalidOperationException($"{nameof(PointB)} is not assigned.");
		}

		public void SetPoints(Transform pointA, Transform pointB) {
			_pointA = pointA;
			_pointB = pointB;
		}

		public void RefreshFit() {
			Owner.Fit(_pointA.position, _pointB.position);
		}

		// - Public Properties -
		public MiniMapUIFitFeature FitFeature
		{
			get { return Owner; }
		}
		public Transform PointA
		{
			get { return _pointA; }
			set { _pointA = value; }
		}
		public Transform PointB
		{
			get { return _pointB; }
			set { _pointB = value; }
		}

		// - Handler -
		protected override void OnReady() {
			base.OnReady();
			RefreshFit();
		}

		void IR3PreLateUpdatable.R3PreLateUpdate() {
			RefreshFit();
		}

		// - Internals -
		[Title(Headers.Injectable)]
		[SerializeField] Transform _pointA;
		[SerializeField] Transform _pointB;
	}
}