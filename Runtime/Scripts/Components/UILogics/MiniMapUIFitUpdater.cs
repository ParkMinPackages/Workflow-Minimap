using System;
using ParkMinPackages.Foundation.Components;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UILogics
{
	public sealed class MiniMapUIFitUpdater : DependencyBehaviour, IR3PreLateUpdatable
	{
		// - Public Methods -
		public void Initialize(MiniMapUIFitter fitter, Transform pointA, Transform pointB) {
			_fitter = fitter;
			_pointA = pointA;
			_pointB = pointB;
		}

		public override void ValidateDependencies() {
			if (_fitter == null)
				throw new InvalidOperationException($"{nameof(Fitter)} is not assigned.");
			if (_pointA == null)
				throw new InvalidOperationException($"{nameof(PointA)} is not assigned.");
			if (_pointB == null)
				throw new InvalidOperationException($"{nameof(PointB)} is not assigned.");
		}

		public void RefreshFit() => _fitter.Fit(_pointA.position, _pointB.position);

		// - Public Properties -
		public MiniMapUIFitter Fitter
		{
			get { return _fitter; }
			set { _fitter = value; }
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
		[SerializeField] MiniMapUIFitter _fitter;
		[SerializeField] Transform _pointA;
		[SerializeField] Transform _pointB;

	}
}
